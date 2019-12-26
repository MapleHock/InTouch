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
        private static UDPSender instance = null;

        private const int byteBufferSize = 1 * 1024 * 1024; // 16MB
        public const int UDPSENDPORT = 11001;
        private IPEndPoint ipe = null;
        UdpClient udpClient;
        // 
        public enum State {
            WAITCALL0,
            WAITACK0,
            WAITCALL1,
            WAITACK1,
            END // 程序退出时用
        }
        private State state;
        Queue<byte[]> UpperDataQueue;

        private Thread SenderStateMachineThread;
        public static UDPSender getInstance() {
            if (instance == null)
                instance = new UDPSender();

            return instance;
        }

        private UDPSender() {
            state = State.WAITCALL0;            
            UpperDataQueue = new Queue<byte[]>();
            udpClient = new UdpClient(UDPSENDPORT);
            SenderStateMachineThread = new Thread(SenderStateRun) { Name = "UDP sender state" };
            SenderStateMachineThread.Start();
        }

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
                Thread.Sleep(500); // 防止CPU占用率过高
            }
            return;
        }

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

        private State WaitCallProcess(byte seq) {
            // 上层无调用
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
                return CombSeqState((byte)(-seq + 1), true);
            } else { // 接收到错误ack或者破损ack， 不动作
                return CombSeqState(seq, false);
            }
           
        }

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
