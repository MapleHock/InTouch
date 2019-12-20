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
    class Video {
        VideoCaptureDevice videoSource = new VideoCaptureDevice();

        public delegate void RecvNewFrameHandler(System.Drawing.Bitmap newFrame);
        public event RecvNewFrameHandler RecvNewFrame;
        TcpClient tcpSender;
        TcpListener tcpListener;
        IPEndPoint ipe;
        const int VIDEOSTREAMLISTENPORT = 10000;
        const int byteBufferSize = 16 * 1024 * 1024;
        bool isChatting = false;
        bool isProcessing = false;
        System.Drawing.Bitmap bitmapToWrite = null;
        public Video() {
            videoSource = new VideoCaptureDevice();
        }

        
        public bool SelectedDevice() {
            VideoCaptureDeviceForm captureDevice = new VideoCaptureDeviceForm(); // 设备选择窗口
            videoSource = new VideoCaptureDevice();
            FilterInfoCollection videoDevice = new FilterInfoCollection(FilterCategory.VideoInputDevice);

            if (captureDevice.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
                videoSource = captureDevice.VideoDevice;
                videoSource.NewFrame += new NewFrameEventHandler(videoSource_NewFrame);
                return true;
            }
            return false;
        }

        public void BeginVideoChatting(IPAddress address) {
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
            Thread tcpListenThread = new Thread(listenVideo) { Name = $"RecvVideo" };
            tcpListenThread.Start();
            Thread.Sleep(500); // 保险等待对方建立listnenter

            // sender
            ipe = new IPEndPoint(address, VIDEOSTREAMLISTENPORT);
            tcpSender = new TcpClient() { ReceiveTimeout = 2000, SendTimeout = 2000 };
            

            videoSource.Start();
            
            Thread SampleThread = new Thread(SampleVideo) { Name = $"SampleVideo" };           
            SampleThread.Start();
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
                        while (!nwStream.DataAvailable) {
                            Thread.Sleep(10); // 等待对方流建立  
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
                        
                    } catch (Exception) {
                        isProcessing = false;
                        continue;
                    }
                    tcpSender = new TcpClient() { ReceiveTimeout = 2000, SendTimeout = 2000 };
                    tcpSender.Connect(ipe);
                    NetworkStream nwStream = tcpSender.GetStream();
                    bitmapToWrite.Save(nwStream, System.Drawing.Imaging.ImageFormat.Jpeg);
                    nwStream.Close();
                    tcpSender.Close();
                    isProcessing = false;

                }
            }
        }

    }
}
