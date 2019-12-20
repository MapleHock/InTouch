using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Windows;
using System.Net;
using System.Net.Sockets;
using System.IO;

namespace InTouch.NetWork
{
    public class P2PSender {
        private static P2PSender instance = null;

        private const int byteBufferSize = 16 * 1024 * 1024; // 16MB

        public static P2PSender getInstance() {
            if (instance == null)
                instance = new P2PSender();

            return instance;
        }

        private P2PSender() {

        }

        public void SendData(byte[] data, string targetIP, int targetPort) {
            var tcpClient = new TcpClient() { ReceiveTimeout = 2000, SendTimeout = 2000, SendBufferSize = byteBufferSize };
            int maxPort = targetPort + P2PListener.MAXPORTSPAN;
            for (; targetPort < maxPort; targetPort++) {
                try {
                    tcpClient.Connect(targetIP, targetPort);
                    break;
                } catch (Exception e) {
                    if (targetPort == maxPort -1) {
                        MessageBox.Show(e.Message, "目标计算机未打开监听端口");
                        return;
                    }
                }
            }            

            try {
                NetworkStream nwStream = tcpClient.GetStream();
                nwStream.Write(data, 0, data.Length);
                nwStream.Close();
                tcpClient.Close();
            } catch (Exception e) {
                MessageBox.Show(e.Message);
                return;
            }
        }


    }
}
