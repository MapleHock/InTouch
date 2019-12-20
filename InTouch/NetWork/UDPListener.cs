using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Windows;
using System.Threading;

namespace InTouch.NetWork {
    public class UDPListener {
        public const int UDPLISTENPORT = 11000;
        private IPAddress localIP = null;
        UdpClient udpListener = null;
        IPAddress currentRemoteIPE = null;

        Thread UDPListenThread;
        bool isListening = false;
        public UDPListener() {
            try {
                string hostName = Dns.GetHostName();
                IPHostEntry iPHostEntry = Dns.GetHostEntry(hostName);
                foreach (var iPHost in iPHostEntry.AddressList) {
                    if (iPHost.AddressFamily == AddressFamily.InterNetwork)
                        localIP = iPHost;
                }
            } catch (Exception e) {
                MessageBox.Show(e.Message);
                return;
            }
            if (localIP == null) {
                MessageBox.Show("Cannot get local IP");
                return;
            }

            try {                
                udpListener = new UdpClient(UDPLISTENPORT);
            } catch (Exception e) {
                MessageBox.Show(e.Message, "UDP监听端口被占用");
                throw;
            }
        }

        public void BeginListen() {
            UDPListenThread = new Thread(ListenData) { Name = $"udp listening:{UDPLISTENPORT}" };
            try {
                UDPListenThread.Start();
            } catch(Exception e) {
                MessageBox.Show(e.Message, "打开UDP监听端口失败");
                return;
            }
        }

        public void ListenData() {
            isListening = true;
            while(isListening) {
                IPEndPoint ipe = new IPEndPoint(IPAddress.Any, UDPLISTENPORT);
                // 无新消息
                if (udpListener.Available == 0)
                    continue;

                byte[] recvBytes = udpListener.Receive(ref ipe);

                // debug
                string word = null;
                string srcId = null;
                string destId = null;
                AppProtocol.UnPackWord(recvBytes, ref word, ref srcId,ref destId);
                MessageBox.Show(word);
            }
        }

        public void EndListen() {
            isListening = false;
            try {
                UDPListenThread.Join();
            } catch (Exception e) {
                MessageBox.Show(e.Message, "UDP监听进程无法停止");
                throw;
            }
        }
    }
}
