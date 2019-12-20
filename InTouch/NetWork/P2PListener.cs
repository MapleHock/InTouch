using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Windows;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.Threading;
using System.IO;

using InTouch.Model;

namespace InTouch.NetWork {
    
    
    public class P2PListener {
        public const int MAXPORTSPAN = 10;
        public const int WORDMSGLISTENPORT = 8000;
        public const int FILEMSGLISTENPORT = 8010;

        private TcpListener tcpListener;
        private Thread listenThread;


        private IPAddress localIP = null;
        int listenPort;
        bool listening;
        private const int byteBufferSize = 16 * 1024 * 1024; // 15 + 1M 最大1M报头
        byte[] recvBytes = new byte[byteBufferSize];




        public delegate void RecvNewDataHandler(byte[] newData);
        public event RecvNewDataHandler RecvCallBack;

        public P2PListener(int _listenPort) {
            try {
                string hostName = Dns.GetHostName();
                IPHostEntry iPHostEntry = Dns.GetHostEntry(hostName);
                foreach (var iPHost in iPHostEntry.AddressList) {
                    if (iPHost.AddressFamily == AddressFamily.InterNetwork)
                        localIP = iPHost;
                }
            } catch(Exception e) {
                MessageBox.Show(e.Message,"没有可用的监听端口");
                return;
            }
            if (localIP == null) {
                MessageBox.Show("Cannot get local IP");
                return;
            }

            this.listenPort = _listenPort;
            //switch (_listenPort) {
            //    case WORDMSGLISTENPORT:
            //        DataToMsg = DataToWord;
            //        break;
            //    case FILEMSGLISTENPORT:
            //        DataToMsg = DataToFile;
            //        break;
            //    default:
            //        break;
            //} // TODO
            // 尝试不同的端口号，从起点向上搜索 MAXPORTSPAN 个端口，有空则使用
            IPGlobalProperties ipProperties = IPGlobalProperties.GetIPGlobalProperties();
            IPEndPoint[] ipEndPoints = ipProperties.GetActiveTcpListeners();
            int maxPort = this.listenPort + MAXPORTSPAN;
            for (; this.listenPort < maxPort; this.listenPort++) {
                bool isUsed = false;
                foreach (IPEndPoint endPoint in ipEndPoints) {
                    if (endPoint.Port == this.listenPort) {
                        isUsed = true;
                        break;
                    }
                }
                if (!isUsed)
                    break;
            }            

            tcpListener = new TcpListener(localIP, listenPort); // 目标为本机该端口号的报文才被监听
        }

        public void BeginListen() {
            listenThread = new Thread(ListenData) { Name = $"listening: {listenPort.ToString()}"};
            try {
                listenThread.Start();
            } catch(Exception e) {
                MessageBox.Show(e.Message, "打开消息监听端口失败");
                return;
            }            
        }

        public void ListenData() {
            tcpListener.Start();
            listening = true;
            while (listening) {
                // 监听到挂起的连接请求，连接并收消息
                if (tcpListener.Pending()) {
                    TcpClient recvClient = null;
                    try {
                        recvClient = tcpListener.AcceptTcpClient();
                        recvClient.ReceiveBufferSize = byteBufferSize;
                        NetworkStream nwStream = recvClient.GetStream();
                        while (!nwStream.DataAvailable) {
                            Thread.Sleep(10); // 等待对方流书写完成   
                        }                                
                        while (nwStream.DataAvailable) {
                            recvBytes = new byte[byteBufferSize];
                            nwStream.Read(recvBytes, 0, byteBufferSize);
                        }
                    } catch (Exception e) {
                        MessageBox.Show(e.Message);
                        return;
                    }

                    if (RecvCallBack != null) {
                        Application.Current.Dispatcher.BeginInvoke(RecvCallBack, recvBytes);
                    }                    
                }
            }
            return;
        }

        public void EndListen() {
            listening = false;
            try {
                listenThread.Join();
            } catch (Exception e) {
                MessageBox.Show(e.Message, "消息监听进程无法停止");
                return;
            }            
        }


    }


}
