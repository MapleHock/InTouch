using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Windows;

namespace InTouch.NetWork
{
    public class P2PPeer {
       
    }

    public class P2PModule {
        private const int bufferSize = 16 * 1024 * 1024;
        private Thread threadRecv = null;
        private byte[] recvBuffer = null;

        private TcpListener listener = null;
        private TcpClient peer = null;
        public IPAddress theIP = null;

        private static P2PModule instance = null;

        public static P2PModule GetInstance() {
            if (instance == null)
                instance = new P2PModule();

            return instance;
        }

        private P2PModule() {
            recvBuffer = new byte[bufferSize];

            theIP = GetIPv4();
            try {
                listener = new TcpListener(theIP, 8000);
            } catch(Exception e) {
                MessageBox.Show(e.Message);
            }
        }

        public void SendData(byte[] data, string targetIP, int targetPort) {
            using (peer = new TcpClient()) {
                peer.SendTimeout = 2000;
                peer.ReceiveTimeout = 2000;
                peer.SendBufferSize = bufferSize;
               
                try {
                    peer.Connect(targetIP, targetPort);
                    NetworkStream stream = peer.GetStream();
                    stream.Write(data, 0, data.Length);
                    stream.Close();
                    peer.Close();
                } catch (Exception e) {
                    MessageBox.Show(e.Message);                    
                }
            }
            return;
        }

        public void beginListen() {
            threadRecv = new Thread(AcceptRech) {
                Name = "MyNetMessage"
            };
            try {
                listener.Start();
            } catch(Exception e) {
                MessageBox.Show(e.Message);
            }
            threadRecv.Start();
        }

        public void EndListen() {
            threadRecv.Join();
            try {
                listener.Stop();
            } catch (Exception e) {
                MessageBox.Show(e.Message);
            }
        }

        private void AcceptRech() {
            bool ongoing = true;
            while (ongoing) {
                if (listener.Pending()) {
                    TcpClient client = null;
                    try {
                        client = listener.AcceptTcpClient();
                        client.ReceiveBufferSize = bufferSize;
                        NetworkStream stream = client.GetStream();
                        int small = 1024 * 1024, len = 0;
                        byte[] buff = new byte[small];
                        if (stream.CanRead) {
                            do {
                                int actual = 0;
                                actual = stream.Read(buff, 0, small);
                                Buffer.BlockCopy(buff, 0, recvBuffer, len, actual);
                                len += actual;
                                Thread.Sleep(50);
                            } while (stream.DataAvailable);
                        }
                        stream.Close();
                        byte[] msg = new byte[len];
                        Buffer.BlockCopy(recvBuffer, 0, msg, 0, len);
                    } catch (Exception e) {
                        MessageBox.Show(e.Message);
                        return;
                    }
                    client.Close();
                }
            }
        }

        private IPAddress GetIPv4() {
            try {
                string HostName = Dns.GetHostName();
                IPHostEntry IpEntry = Dns.GetHostEntry(HostName);

                foreach (var host in IpEntry.AddressList) {
                    if (host.AddressFamily == AddressFamily.InterNetwork) {
                        return host;
                    }
                }
                return null;
            } catch (Exception e) {
                MessageBox.Show(e.Message);
                return null;
            }
        }

    }
}
