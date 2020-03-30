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
    
    // 监听类
    // 起名为P2PListener，实际为基于TCP的数据监听
    public class P2PListener {
        public const int MAXPORTSPAN = 10;

        // 两个端口
        // 通用端口，用于文字消息、图片消息、控制消息传输
        public const int GENERALLISTENPORT = 8000;
        // 文件端口，用于文件传输，防止文件传输时其他功能无法使用
        public const int FILELISTENPORT = 8010;

        // 网络编程，传输层相关
        private TcpListener tcpListener;
        private IPAddress localIP = null;
        int listenPort;
       
        public const int byteBufferSize = 4 * 1024 * 1024; // 4M buffer
        byte[] recvBytes = new byte[byteBufferSize];

        // 程序具体实行相关，线程间通信
        private Thread listenThread;
        bool listening;

        // 与应用层交互
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
            //    case P2PLISTENPORT:
            //        DataToMsg = DataToWord;
            //        break;
            //    case P2PLISTENPORT:
            //        DataToMsg = DataToFile;
            //        break;
            //    default:
            //        break;
            //} 
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

        // 打开侦听线程
        public void BeginListen() {
            listenThread = new Thread(ListenData) { Name = $"listening: {listenPort.ToString()}"};
            try {
                listenThread.Start();
            } catch(Exception e) {
                MessageBox.Show(e.Message, "打开消息监听端口失败");
                return;
            }            
        }

        // 侦听线程运行函数，若接到TCP连接请求，接受请求并且接受数据，然后引发事件，让应用层分发消息
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
                            Thread.Sleep(100); // 等待对方创建流
                        }
                        int offset = 0;
                        while (nwStream.DataAvailable) {
                            offset = nwStream.Read(recvBytes, offset, byteBufferSize - offset);                            
                        }                        
                        nwStream.Close();
                    } catch (Exception e) {
                        MessageBox.Show(e.Message);
                        return;
                    }

                    // 递交应用层
                    if (RecvCallBack != null) {
                        Application.Current.Dispatcher.BeginInvoke(RecvCallBack, recvBytes);
                    }                    
                }

                Thread.Sleep(500); 
            }
            return;
        }

        // 安全结束侦听线程
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
