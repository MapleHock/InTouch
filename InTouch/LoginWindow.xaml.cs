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
using System.Threading;

using InTouch.NetWork;
namespace InTouch {
    /// <summary>
    /// LoginWindow.xaml 的交互逻辑
    /// </summary>
    public partial class LoginWindow : Window {
        public LoginWindow() {
            InitializeComponent();
        }
        // 账户登陆，主题由ViewModel完成，这个函数只处理App初始化和到MainWindow的衔接
        private void StackPanel_IsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e) {
            App.user = new Model.User() {
                userName = UserNameTbx.Text
            };
            App.LoadAddressBook(UserNameTbx.Text);
            App.generalListener = new P2PListener(P2PListener.GENERALLISTENPORT);
            App.generalListener.BeginListen();
            App.fileListener = new P2PListener(P2PListener.FILELISTENPORT);
            App.fileListener.BeginListen();
            UDPListener.getInstance();
            UDPListener.getInstance().BeginListen();
            this.Close();
        }
    }
}
