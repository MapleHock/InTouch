using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Windows;

namespace InTouch.NetWork {
    // CS Client 类，单例
    // 用于描述和提供CS模式下 客户机的行为
    // 包含上下线和发送查询状态等。
    public class CSClient {
        // 单例相关
        private static CSClient instance = new CSClient();

        public static CSClient getInstance() {
            return instance;
        }

        private CSClient() {
            serverIPE = new IPEndPoint(IPAddress.Parse(serverIP), serverPort);
        }


        private IPEndPoint serverIPE;
        private Socket socket = null;
        private string serverIP = "166.111.140.57"; // TODO const
        private int serverPort = 8000;
        private int timeOut = 5000;
        private int byteBufferSize = 32; // TODO large?


        public string SendAMsg(string msg) {
            string recvMsg = "error";
            socket = new Socket(AddressFamily.InterNetwork,
                                SocketType.Stream,
                                ProtocolType.Tcp) {
                                SendTimeout = timeOut,
                                ReceiveTimeout = timeOut
            };
            try {
                socket.Connect(serverIPE);
                socket.Send(Encoding.UTF8.GetBytes(msg));
                byte[] recvBytes = new byte[byteBufferSize];
                int byteLength = socket.Receive(recvBytes);
                recvMsg = Encoding.UTF8.GetString(recvBytes, 0, byteLength);
                socket.Disconnect(true); // TODO resueabe ?
                socket.Close();
                return recvMsg;
            } catch (Exception e) {
                MessageBox.Show(e.Message);
                return recvMsg;
            }
        }
    }
}
