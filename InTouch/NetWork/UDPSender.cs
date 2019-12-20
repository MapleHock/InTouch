using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Windows;

namespace InTouch.NetWork {
    class UDPSender {
        private static UDPSender instance = null;

        private const int byteBufferSize = 1 * 1024 * 1024; // 16MB
        public const int UDPSENDPORT = 11001;
        public static UDPSender getInstance() {
            if (instance == null)
                instance = new UDPSender();

            return instance;
        }

        private UDPSender() {

        }

        public void sendData(byte[] data, string targetIP) {
            try {
                IPAddress address = IPAddress.Parse(targetIP);

                var udpClient = new UdpClient(UDPSENDPORT);

                var ipe = new IPEndPoint(address, UDPListener.UDPLISTENPORT);
                udpClient.Send(data, data.Length, ipe);
            } catch (Exception e) {
                MessageBox.Show(e.Message,"UDP发送失败");
            }            
        }

        // private static int GetCheckSum
    }
}
