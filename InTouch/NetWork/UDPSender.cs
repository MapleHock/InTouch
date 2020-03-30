using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Windows;
using System.Threading;

namespace InTouch.NetWork {
    class UDPSender {
        // 单例模式相关
        private static UDPSender instance = null;
        public static UDPSender getInstance() {
            if (instance == null)
                instance = new UDPSender();

            return instance;
        }

        // 网络相关
        private const int byteBufferSize = 1 * 1024 * 1024; // 1MB
        public const int UDPSENDPORT = 11001;
        private IPEndPoint ipe = null;
        UdpClient udpClient;
        
        // 状态机 END状态用于结束时关断线程
        public enum State {
            WAITCALL0,
            WAITACK0,
            WAITCALL1,
            WAITACK1,
            END // 程序退出时用
        }
        private State state;
        
        // 待发送消息队列，由应用层填充
        Queue<byte[]> UpperDataQueue;

        // 同步状态机运行线程
        private Thread SenderStateMachineThread;


        private UDPSender() {
            state = State.WAITCALL0;            
            UpperDataQueue = new Queue<byte[]>();
            udpClient = new UdpClient(UDPSENDPORT);
            SenderStateMachineThread = new Thread(SenderStateRun) { Name = "UDP sender state" };
            SenderStateMachineThread.Start();
        }

        // 同步状态机运行线程，同步周期500ms
        // 同时也是状态机状态方程
        private void SenderStateRun() {
            while (state != State.END) {
                switch (state) {
                    case State.WAITCALL0:
                        state = WaitCallProcess(0);
                        break;
                    case State.WAITACK0:
                        state = WaitACKProcess(0);
                        break;
                    case State.WAITCALL1:
                        state = WaitCallProcess(1);
                        break;
                    case State.WAITACK1:
                        state = WaitACKProcess(1);
                        break;
                    default:
                        break;
                }
                Thread.Sleep(500); // 防止CPU占用率过高， 也决定了同步周期为500ms
            }
            return;
        }

        // 辅助函数，结合状态和序列号，返回状态
        private State CombSeqState(Byte seq, bool isWaitCall) {
            if (isWaitCall) {
                if (seq == 0)
                    return State.WAITCALL0;
                return State.WAITCALL1;
            } else {
                if (seq == 0)
                    return State.WAITACK0;
                return State.WAITACK1;
            }
        }

        // WAIT状态的边，输出方程和次态决定
        private State WaitCallProcess(byte seq) {
            // 上层无调用，计时器加一
            if (UpperDataQueue.Count == 0) {
                return CombSeqState(seq, true);
            }
            byte[] dataToSend = UpperDataQueue.First();
            byte[] dataPkt = makePkt(dataToSend, seq);
            sendData(dataPkt);
            counter = 0;
            return CombSeqState(seq, false);
        }

        int counter = 0;
        private State WaitACKProcess(byte seq) {
            if (udpClient.Available == 0) {                
                counter++;
                if (counter >= 20) { // 超时重传， 时间阈值 20 * 500ms = 10s
                    byte[] dataToSend = UpperDataQueue.First();
                    byte[] dataPkt = makePkt(dataToSend, seq);
                    sendData(dataPkt);
                    counter = 0;
                }
                return CombSeqState(seq, false);
            }
                
            byte[] ackPack = udpClient.Receive(ref ipe);
            bool isPassCheck = checkPkt(ackPack);
            bool isSeqCorrect = ackPack[1] == seq;
            if (isPassCheck && isSeqCorrect) {
                UpperDataQueue.Dequeue(); // 成功传输，此消息退出队列传输下一项
                return CombSeqState((byte)(-seq + 1), true); // 跳到下一个序号的wait
            } else { // 接收到错误ack或者破损ack， 不动作,博爱吃原状态
                return CombSeqState(seq, false);
            }
           
        }

        // 给应用层提供的可靠数据传输接口，实际上只把数据加入到待发送队列中
        public void ReliableSendData(byte[] data, string targetIP) {
            
            IPAddress iPAddress = IPAddress.Parse(targetIP);
            if (ipe == null) {
                ipe = new IPEndPoint(iPAddress, UDPListener.UDPLISTENPORT);
            } else if(ipe.Address.AddressFamily != iPAddress.AddressFamily) {
                if (UpperDataQueue.Count != 0) {
                    MessageBox.Show("与上一用户的UDP聊天未结束, 请等待后重发消息");
                    return;
                } else
                state = State.WAITACK0; // 切换了IP，重置状态机
            }
            
            UpperDataQueue.Enqueue(data);
        }

        private void sendData(byte[] data) { 
            try {        
                udpClient.Send(data, data.Length, ipe);
            } catch (Exception e) {
                MessageBox.Show(e.Message,"UDP发送失败");
            }            
        }

        // 封包函数，在头部加上两个字节，一字节的校验和，一字节的序列号
        private byte[] makePkt(byte[] data, byte seq) {
            byte checksum = 0;
            for (int i = 0; i < data.Length; i++) {
                checksum += data[i];
            }
            checksum += seq;
            byte[] pkt = new byte[data.Length + 1 + 1];            
            pkt[0] = checksum;
            pkt[1] = seq;
            data.CopyTo(pkt, 2);
            return pkt;
        }

        // 检验接收方回传的包，是否为正确无损的ACK
        private bool checkPkt(byte[] data) {
            byte sum = 0;
            for (int i = 1; i < data.Length;i++) {
                sum += data[i];
            }
            if (sum == data[0])
                return true;
            else
                return false;
        }

        public void EndUDPSender() {
            state = State.END;
            if (SenderStateMachineThread.IsAlive) {
                SenderStateMachineThread.Join();
            }
        }
    }
}
