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

        public const int byteBufferSize = 4 * 1024 * 1024; // 16MB

        public static P2PSender getInstance() {
            if (instance == null)
                instance = new P2PSender();

            return instance;
        }

        private P2PSender() {

        }

        public void SendData(byte[] data, string targetIP, int targetPort) {
            var tcpClient = new TcpClient() { ReceiveTimeout = 2000, SendTimeout = 2000, SendBufferSize = byteBufferSize };
            try {
                tcpClient.Connect(targetIP, targetPort);
            } catch (Exception e) {                
                MessageBox.Show(e.Message, "目标计算机未打开监听端口");
                return;                
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

        public void SendDataGroup(byte[][] dataGroup, string targetIP, int targetPort) {
            for (int i = 0; i < dataGroup.Length; i++) {
                // 非持续性传输，每次报文断开再连接
                SendData(dataGroup[i], targetIP, targetPort);
            }
        }

    }
}
