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
using System.Windows.Threading;

using InTouch.NetWork;


namespace InTouch {
    /// <summary>
    /// audioWindow.xaml 的交互逻辑
    /// </summary>
    public partial class audioWindow : Window {
        public Audio audio;
        DispatcherTimer timer;
        DateTime total;
        public audioWindow(Audio audio) {            
            InitializeComponent();
            this.audio = audio;

            audio.AudioChatBegin();

            
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(1000);
            timer.Tick += updateCount;
            total = DateTime.MinValue;
            timer.Start();
        }

        private void updateCount(object sender, EventArgs e) {
            total = total.AddSeconds(1);
            timerLbl.Content = $"{total.Hour.ToString("D2")}:{total.Minute.ToString("D2")}:{total.Second.ToString("D2")}";
        }

        private void Window_Closed(object sender, EventArgs e) {
            audio.AudioChatEnd();
        }
    }
}
