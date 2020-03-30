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
    // P2P报文的TCP发送端
    public class P2PSender {
        // 单例相关
        private static P2PSender instance = null;

        public const int byteBufferSize = 4 * 1024 * 1024; // 16MB

        public static P2PSender getInstance() {
            if (instance == null)
                instance = new P2PSender();

            return instance;
        }

        private P2PSender() {

        }

        // 发送单组数据时，按照连接，写入流，关断的方式执行
        public void SendData(byte[] data, string targetIP, int targetPort) {
            var tcpClient = new TcpClient() { ReceiveTimeout = 2000, SendTimeout = 2000, SendBufferSize = byteBufferSize };
            try {
                tcpClient.Connect(targetIP, targetPort); // 连接
            } catch (Exception e) {                
                MessageBox.Show(e.Message, "目标计算机未打开监听端口");
                return;                
            }            

            try {
                NetworkStream nwStream = tcpClient.GetStream();
                nwStream.Write(data, 0, data.Length); // 写入流
                nwStream.Close(); // 关断
                tcpClient.Close();
            } catch (Exception e) {
                MessageBox.Show(e.Message);
                return;
            }
        }

        // 发送数据组
        public void SendDataGroup(byte[][] dataGroup, string targetIP, int targetPort) {
            for (int i = 0; i < dataGroup.Length; i++) {
                // 非持续性传输，每次报文断开再连接
                SendData(dataGroup[i], targetIP, targetPort);
            }
        }

    }
}
