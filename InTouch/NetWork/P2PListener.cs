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


        List<byte[]> dataGroup = new List<byte[]>();
        private IPAddress localIP = null;
        int listenPort;
        bool listening;
        private const int byteBufferSize = 1024 * 1024; // 1MB TODO
        byte[] recvBytes = new byte[byteBufferSize];
        private DataToMsg DataToMsg;

        public List<Message> rawMessageList = new List<Message>();

        public delegate void RecvNewDataHandler();
        public event RecvNewDataHandler RecvNewData;

        public P2PListener(int _listenPort) {
            try {
                string hostName = Dns.GetHostName();
                IPHostEntry iPHostEntry = Dns.GetHostEntry(hostName);
                foreach (var iPHost in iPHostEntry.AddressList) {
                    if (iPHost.AddressFamily == AddressFamily.InterNetwork)
                        localIP = iPHost;
                }
            } catch(Exception e) {
                MessageBox.Show(e.Message);
                return;
            }
            if (localIP == null) {
                MessageBox.Show("Cannot get local IP");
                return;
            }

            this.listenPort = _listenPort;
            switch (_listenPort) {
                case WORDMSGLISTENPORT:
                    DataToMsg = DataToWord;
                    break;
                case FILEMSGLISTENPORT:
                    DataToMsg = DataToFile;
                    break;
                default:
                    break;
            }
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
                MessageBox.Show(e.Message);
                return;
            }            
        }

        public void ListenData() {
            tcpListener.Start();
            listening = true;
            int groupStart = 0;
            while (listening) {
                // 监听到挂起的连接请求，连接并收消息
                if (tcpListener.Pending()) {
                    TcpClient recvClient = null;
                    try {
                        recvClient = tcpListener.AcceptTcpClient();
                        recvClient.ReceiveBufferSize = byteBufferSize;
                        NetworkStream nwStream = recvClient.GetStream();
                                             
                        if (nwStream.CanRead) {
                            while (nwStream.DataAvailable) {
                                recvBytes = new byte[byteBufferSize];
                                nwStream.Read(recvBytes, 0, byteBufferSize);
                                dataGroup.Add(recvBytes);
                                Thread.Sleep(2000);
                            }
                        }
                    } catch (Exception e) {
                        MessageBox.Show(e.Message);
                        return;
                    }
                    rawMessageList.Add(DataToMsg(groupStart, dataGroup.Count));
                    groupStart = dataGroup.Count;
                    if (RecvNewData != null) {
                        Application.Current.Dispatcher.BeginInvoke(RecvNewData);
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
                MessageBox.Show(e.Message);
                return;
            }            
        }

        private Model.Message DataToWord(int groupStart, int groupEnd) {
            
            // TODO 分发
            StringBuilder stringBuilder = new StringBuilder();
            for (var i = groupStart; i < groupEnd; i++) {
                stringBuilder.Append(Encoding.UTF8.GetString(dataGroup[i]).Replace("\0",""));
                
            }
            string words = stringBuilder + "";

            var newMsg = new Model.Message() {
                type = Model.Message.Type.Words,
                msg = words
            };
            newMsg.UpdateDesciptioin();
            return newMsg;
        }

        private Model.Message DataToFile(int groupStart, int groupEnd) {
            string fileName = $"test";
            FileStream fstream = new FileStream(fileName, FileMode.Create, FileAccess.Write);
            string suffix = null;
            byte[] firstByte = null;
            long fstreamLength = 0;
            try {
                string firstBuffer = Encoding.UTF8.GetString(dataGroup[groupStart]);

                string[] details = firstBuffer.Substring(0, firstBuffer.IndexOf('\0') - 1).Split('|');
                suffix = details[0];
                fstreamLength = Convert.ToUInt32(details[1]);
                firstByte = Encoding.UTF8.GetBytes(firstBuffer.Substring(firstBuffer.IndexOf('\0')));
            } catch (Exception e) {
                MessageBox.Show(e.Message, "文件概况接收失败");
                return null;
            }

            try {
                fstream.Write(firstByte, 0, firstByte.Length);
                for (int i = groupStart + 1; i < groupEnd; i++) {
                    fstream.Write(dataGroup[i], 0, (int)Math.Min(byteBufferSize, fstreamLength)); // TODO check
                }
            } catch (Exception e) {
                MessageBox.Show(e.Message, "文件接收失败");
                return null;
            }
            fstream.Close();
            var newMsg = new Model.Message() {
                type = Model.Message.Type.File,
                msg = fileName
            };
            return newMsg;
        }
    }

    public delegate Model.Message DataToMsg(int groupStart, int groupEnd);
}
