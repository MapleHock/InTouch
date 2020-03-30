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
using System.Windows.Navigation;
using System.Windows.Shapes;

using MaterialDesignThemes.Wpf;
using InTouch.NetWork;

namespace InTouch {
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window {
        public AddressListPage addressListPage;
        public chattingPage chattingPage;
        public MainWindow() {
            InitializeComponent();
            rightPageCtrl.Content = new Frame() { Content = new WelComePage()}; // 欢迎页面
        }


        // 切换到通讯录页面
        private void ContactIcon_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
            if (addressListPage == null)
                addressListPage = new AddressListPage();

            rightPageCtrl.Content = new Frame() { Content = addressListPage };
        }

        // 切换到聊天界面
        private void MessageIcon_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
            if (chattingPage == null)
                chattingPage = new chattingPage();


            rightPageCtrl.Content = new Frame() { Content = chattingPage };
            for (int i = chattingPage.chatRoomList.Items.Count; i < App.addressBook.items.Count; i++) {
                chattingPage.chatViewModel.updateChatList(App.addressBook.items[i]);
            }
            
        }

        // 主窗口关闭时，关断各类监听端口，写本地通讯录
        private void Window_Closed(object sender, EventArgs e) {
            for (int i = 0; i < 100; i++) {
                string recv = NetWork.CSClient.getInstance().SendAMsg($"logout{App.user.userName}");
                if (recv == "loo")
                    break;
                if (i == 100 - 1)
                    MessageBox.Show("多次尝试，未能和服务器成功发送下线请求");
            }
            // TODO commonet logout, debug temp

            App.generalListener.EndListen();
            App.fileListener.EndListen();
            UDPListener.getInstance().EndListen();
            UDPSender.getInstance().EndUDPSender();
            App.UpdateAddressBook();
            Environment.Exit(0);
        }
    }
}
