using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using Accord.Video;
using Accord.Video.DirectShow;
using System.Windows;
using System.IO;
using System.Threading;

namespace InTouch.NetWork {
    public class Video {
        VideoCaptureDevice videoSource = new VideoCaptureDevice();

        public delegate void RecvNewFrameHandler(System.Drawing.Bitmap newFrame);
        public event RecvNewFrameHandler RecvNewFrame;
        TcpClient tcpSender;
        TcpListener tcpListener;
        IPEndPoint ipe;
        Thread tcpListenThread = null;
        const int VIDEOSTREAMLISTENPORT = 10000;
        const int byteBufferSize = 16 * 1024 * 1024;
        bool isChatting = false;
        bool isProcessing = false;
        System.Drawing.Bitmap bitmapToWrite = null;

        // 与应用层交互
        public delegate void TryConnectHandler(int second);
        public event  TryConnectHandler TryConnectCallBack;
        public bool isReject = false;

        public Video(string targetIP) {
            ipe = new IPEndPoint(IPAddress.Parse(targetIP), VIDEOSTREAMLISTENPORT);
        }

        
        public bool SelectedDevice() {
            try { // 防止设备还未选择对方就选择挂断
                VideoCaptureDeviceForm captureDevice = new VideoCaptureDeviceForm(); // 设备选择窗口
                videoSource = new VideoCaptureDevice();
                FilterInfoCollection videoDevice = new FilterInfoCollection(FilterCategory.VideoInputDevice);

                if (captureDevice.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
                    videoSource = captureDevice.VideoDevice;
                    videoSource.NewFrame += new NewFrameEventHandler(videoSource_NewFrame);
                    return true;
                }
            } catch (Exception) {
                return false;
            }
            
            return false;
        }

        public void BeginVideoChatting() {
            isChatting = true;

            // listener
            IPAddress localIP = null;
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
            tcpListener = new TcpListener(localIP, VIDEOSTREAMLISTENPORT);
            tcpListenThread = new Thread(listenVideo) { Name = $"RecvVideo" };
            tcpListenThread.Start();

            tcpSender = new TcpClient() { ReceiveTimeout = 2000, SendTimeout = 2000 };
            for (int i = 0; i < 60; i++) {
                if (isReject) {
                    EndVideoChatting();
                    return;
                }                   
                try {
                    tcpSender.Connect(ipe);
                } catch (Exception) {
                    TryConnectCallBack?.Invoke(i + 1);
                    Thread.Sleep(1000); // 尝试连接，保险等待对方建立listnenters
                    continue;
                }
                break;
            }
            TryConnectCallBack?.Invoke(-1); // -1 的连接秒代表连接成功
            tcpSender.Close();
            // 保险等待对方建立listnenter

            // sender            
            tcpSender = new TcpClient() { ReceiveTimeout = 2000, SendTimeout = 2000 };
            

            videoSource.Start();
            
            Thread SampleThread = new Thread(SampleVideo) { Name = $"SampleVideo" };           
            SampleThread.Start();
        }

        public void EndVideoChatting() {
            isChatting = false;
            try {               
                tcpListenThread.Join();
                tcpListener.Stop();
                videoSource.Stop();
            } catch (Exception e) {
                MessageBox.Show(e.Message, "视频流监听进程无法停止");
                return;
            }
        }

        private void videoSource_NewFrame(object sender, NewFrameEventArgs eventArgs) {
            if (isProcessing) {
                return;
            } else {
                bitmapToWrite = (System.Drawing.Bitmap)eventArgs.Frame.Clone();
                isProcessing = true;
            }

        }

        private void listenVideo() {
            tcpListener.Start();
            TcpClient recvClient;
            
            while (isChatting) {
                if (tcpListener.Pending()) {
                    recvClient = tcpListener.AcceptTcpClient();
                    try {
                        recvClient.ReceiveBufferSize = byteBufferSize;
                        
                        NetworkStream nwStream = recvClient.GetStream();
                        for (int i = 0; i < 10; i++) {
                            if (nwStream.DataAvailable)
                                break;
                            Thread.Sleep(10); // 等待对方流建立  
                        }
                        if (!nwStream.DataAvailable) {
                            nwStream.Close();
                            recvClient.Close();
                            continue;
                        }

                        var temp = System.Drawing.Image.FromStream(nwStream);
                        Application.Current.Dispatcher.BeginInvoke(RecvNewFrame, temp);
                        nwStream.Close();
                        recvClient.Close();                        
                    } catch (Exception e) {
                        MessageBox.Show(e.Message);
                    }
                }
            }

            return;
        }

        private void SampleVideo() {
            while (true) {
                if (isProcessing) {
                    try {
                        tcpSender = new TcpClient() { ReceiveTimeout = 2000, SendTimeout = 2000 };
                        tcpSender.Connect(ipe);
                        NetworkStream nwStream = tcpSender.GetStream();
                        bitmapToWrite.Save(nwStream, System.Drawing.Imaging.ImageFormat.Jpeg);
                        nwStream.Close();
                        tcpSender.Close();
                        isProcessing = false;
                    } catch (Exception) {
                        isProcessing = false;
                        continue;
                    }
                }
            }
        }

    }
}
