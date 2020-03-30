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
    // 视频通话向相关类
    public class Video {

        // 本地设备相关
        VideoCaptureDevice videoSource = new VideoCaptureDevice();
        public delegate void RecvNewFrameHandler(System.Drawing.Bitmap newFrame);
        public event RecvNewFrameHandler RecvNewFrame;

        // 网络相关
        TcpClient tcpSender;
        TcpListener tcpListener;
        IPEndPoint ipe;
        Thread tcpListenThread = null;
        const int VIDEOSTREAMLISTENPORT = 10000;
        const int byteBufferSize = 16 * 1024 * 1024;


        // 变码率传输相关        
        bool isProcessing = false;
        System.Drawing.Bitmap bitmapToWrite = null;

        // 与应用层交互
        bool isChatting = false;
        public delegate void TryConnectHandler(int second);
        public event  TryConnectHandler TryConnectCallBack;
        public bool isReject = false;

        public Video(string targetIP) {
            ipe = new IPEndPoint(IPAddress.Parse(targetIP), VIDEOSTREAMLISTENPORT);
        }

        // 选择摄像头
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

        // 开启视频聊天，建立listener，并且尝试连接到目标的listener
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

            // 尝试连接到对方的listtener
            tcpSender = new TcpClient() { ReceiveTimeout = 2000, SendTimeout = 2000 };
            // 60秒尝试连接
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

        // 本地摄像头拍摄到新的帧
        // 由isProcessing进行动态码率调节
        // 若是isProcessing为真，代表空域降采样或者网络繁忙，帧丢弃
        // 否则，克隆该帧，交由传输接口处理
        private void videoSource_NewFrame(object sender, NewFrameEventArgs eventArgs) {
            if (isProcessing) {
                return;
            } else {
                bitmapToWrite = (System.Drawing.Bitmap)eventArgs.Frame.Clone();
                isProcessing = true;
            }

        }

        // 视频流监听端口，传输层部分
        // 将监听到的数据解码为bitmap，交由应用层处理（一般处理函数中把这个新的帧放置到一个Image中刷新显示）
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

        // 自适应动态码率调节核心
        // 进行了空域压缩，把帧转为jpeg
        // 也进行了时域降采样，加工过程包含了转码和流写入
        // 所以isProcessing间接反应了网络状态，可以本地动态丢弃一些帧
        private void SampleVideo() {
            while (true) {
                if (isProcessing) {
                    try {
                        tcpSender = new TcpClient() { ReceiveTimeout = 2000, SendTimeout = 2000 };
                        tcpSender.Connect(ipe);
                        NetworkStream nwStream = tcpSender.GetStream();
                        bitmapToWrite.Save(nwStream, System.Drawing.Imaging.ImageFormat.Jpeg); // 空域压缩，阻塞式传输，变码率传输的保证
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
