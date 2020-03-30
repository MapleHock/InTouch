using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

using InTouch.NetWork;

namespace InTouch.NetWork {
    /// <summary>
    /// videoWindow.xaml 的交互逻辑
    /// </summary>
    public partial class videoWindow : Window {
        // 视频聊天窗
        // 主要调用video中提供的本地初始化接口
        // 设置video中收到新消息的上层显示调用
        public Video video;
        
        public videoWindow(Video video) {
            InitializeComponent();
            this.video = video;
            if (!video.SelectedDevice()) {
                MessageBox.Show("没有可识别的摄像设备");
                return; // 
            }
            video.RecvNewFrame += RecvNewFrame;
            video.TryConnectCallBack += TryConnect;

            video.BeginVideoChatting();

        }

        private void RecvNewFrame(System.Drawing.Bitmap newFrame) {
            FrameImg.Source = BitmaptoSrc(newFrame);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
            video.EndVideoChatting();
            
        }

        private void TryConnect(int second) {
            if (second == -1) {
                hintLBl.Content = ""; // 连接成功
                return;
            }
            hintLBl.Content = $"尝试连接...{second}/60";
        }

        // ------------------------ 辅助函数格式转换

        // 转换函数，把winform 的bitmap类，变为wpf中的Image能使用的source
        // 引用自： https://blog.csdn.net/jiuzaizuotian2014/article/details/81279423
        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr hObject);
        private BitmapSource BitmaptoSrc(System.Drawing.Bitmap bitmap) {
            IntPtr myImagePtr = bitmap.GetHbitmap();     //创建GDI对象，返回指针 
            BitmapSource imgsource = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(myImagePtr, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());  //创建imgSource
            DeleteObject(myImagePtr);
            return imgsource;
        }       
    }
}
