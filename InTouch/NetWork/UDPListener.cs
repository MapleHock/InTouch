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
    public class UDPListener {
        private static UDPListener instance = null;

        // 网络编程，传输层相关
        public const int UDPLISTENPORT = 11000;
        private IPAddress localIP = null;
        UdpClient udpClient = null;
        IPEndPoint currentRemoteIPE = null;

        // 程序具体实行相关，线程间通信
        Thread UDPListenerThread = null;

        // 状态机
        enum State {
            WAITCALL0,
            WAITCALL1,
            NOMORE,
            END // 程序结束时用于关断线程
        }
        State state;
        // 与应用层交互
        public delegate void RecvNewDataHandler(byte[] newData);
        public event RecvNewDataHandler RecvCallBack;
        public static UDPListener getInstance() {
            if (instance == null)
                instance = new UDPListener();
            return instance;
        }

        private UDPListener() {
            try {
                string hostName = Dns.GetHostName();
                IPHostEntry iPHostEntry = Dns.GetHostEntry(hostName);
                foreach (var iPHost in iPHostEntry.AddressList) {
                    if (iPHost.AddressFamily == AddressFamily.InterNetwork)
                        localIP = iPHost;
                }
            } catch (Exception e) {
                MessageBox.Show(e.Message);
                return;
            }
            if (localIP == null) {
                MessageBox.Show("Cannot get local IP");
                return;
            }

            try {                
                udpClient = new UdpClient(UDPLISTENPORT);
            } catch (Exception e) {
                MessageBox.Show(e.Message, "UDP监听端口被占用");
                throw;
            }

            state = State.WAITCALL0;
            currentRemoteIPE = new IPEndPoint(IPAddress.Any, UDPSender.UDPSENDPORT);
        }

        public void BeginListen() {
            UDPListenerThread = new Thread(ListenData) { Name = $"udp listening:{UDPLISTENPORT}" };
            try {
                UDPListenerThread.Start();
            } catch(Exception e) {
                MessageBox.Show(e.Message, "打开UDP监听端口失败");
                return;
            }
        }

        public void ListenData() {
            while (state != State.END) {
                if (udpClient.Available != 0) {
                    currentRemoteIPE.Address = IPAddress.Any;
                    byte[] recvBytes = udpClient.Receive(ref currentRemoteIPE);
                    switch (state) {
                        case State.WAITCALL0:
                            state = ProcessData(recvBytes, 0);
                            break;
                        case State.WAITCALL1:
                            state = ProcessData(recvBytes, 1);
                            break;
                        default:
                            break;
                    }
                }
                Thread.Sleep(500);
            }
        }

        private State ProcessData(byte[] data, byte seq) {
            bool isPassCheck = checkPkt(data);
            bool isSeqCorrect = data[1] == seq;
            if (isPassCheck && isSeqCorrect) {
                // 递交应用层
                byte[] extractData = new byte[data.Length - 2];
                for (int i = 2; i < data.Length; i++) {
                    extractData[i - 2] = data[i];
                }
                if (RecvCallBack != null) {
                    Application.Current.Dispatcher.BeginInvoke(RecvCallBack, extractData);
                }
                sendData(makeACK(seq));
                if (state == State.WAITCALL0)
                    return State.WAITCALL1;
                else
                    return State.WAITCALL0;
            } else {
                sendData(makeACK((byte)(1 - seq)));
                return state;
            }
        }

        private void sendData(byte[] data) {
            try {
                udpClient.Send(data, data.Length, currentRemoteIPE);
            } catch (Exception e) {
                MessageBox.Show(e.Message, "UDP发送失败");
            }
        }

        private bool checkPkt(byte[] data) {
            byte sum = 0;
            for (int i = 1; i < data.Length; i++) {
                sum += data[i];
            }
            if (sum == data[0])
                return true;
            else
                return false;
        }

        private byte[] makeACK(byte seq) {
            byte[] ackPkt = new byte[2];
            // seq
            ackPkt[1] = seq;
            // checksum
            ackPkt[0] = seq;
            return ackPkt;
        }

        public void EndListen() {
            state = State.END;
            try {
                UDPListenerThread.Join();
            } catch (Exception e) {
                MessageBox.Show(e.Message, "UDP监听进程无法停止");
                throw;
            }
        }
    }
}
