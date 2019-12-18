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

        private const int byteBufferSize = 1024 * 1024; // 1MB

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

        public void SendMsg(string msg, string targetIP, int targetPort) {
            SendData(Encoding.UTF8.GetBytes(msg), targetIP, targetPort); // TODO add dataheader
        }

        public void SendFile(string filePath, string targetIP, int targetPort) {
            FileStream fStream = null;
            try {
                fStream = new FileStream(filePath, FileMode.Open);
            } catch (Exception e) {
                MessageBox.Show(e.Message, "指定文件不存在");
                return;
            }

            // SendData中非持续连接，每次Send后关断，不适合文件传输，重写文件传输时的流连接

            // 端口尝试
            var tcpClient = new TcpClient() { ReceiveTimeout = 2000, SendTimeout = 2000, SendBufferSize = byteBufferSize };
            int maxPort = targetPort + P2PListener.MAXPORTSPAN;
            for (; targetPort < maxPort; targetPort++) {
                try {
                    tcpClient.Connect(targetIP, targetPort);
                    break;
                } catch (Exception e) {
                    if (targetPort == maxPort - 1) {
                        MessageBox.Show(e.Message, "目标计算机未打开监听端口");
                        return;
                    }
                }
            }

            NetworkStream nwStream = null;
            try {
                nwStream = tcpClient.GetStream();
            } catch (Exception e) {
                MessageBox.Show(e.Message);
                return;
            }

            // TODO dataprotocol temp use
            string overview = $"{ filePath.Substring(filePath.IndexOf('.'))}|{fStream.Length}\0";
            try {
                byte[] fileDataBuffer = new byte[byteBufferSize];
                nwStream.Write(Encoding.UTF8.GetBytes(overview), 0, overview.Length);
            } catch (Exception e) {
                MessageBox.Show(e.Message, "文件概况发送失败");
                return;
            }

            int offset = 0;
            while (offset < fStream.Length) {                
                try {
                    byte[] fileDataBuffer = new byte[byteBufferSize];
                    int readLength = fStream.Read(fileDataBuffer, offset, byteBufferSize);
                    offset += readLength;
                    nwStream.Write(fileDataBuffer, 0, readLength);
                } catch (Exception e) {
                    MessageBox.Show(e.Message, "文件传输失败");
                    return;
                }
            }

            nwStream.Close();
            tcpClient.Close();
        }
    }
}
