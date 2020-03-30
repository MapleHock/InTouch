using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows;

using InTouch.NetWork;

namespace InTouch.ViewModel
{
    // User的ViewModel，处理本地数据和UI的交互
    public class UserViewModel : ViewModelBase {
        private Model.User user = new Model.User();
        public string userName {
            get { return user.userName; }
            set { SetProperty(ref user.userName, value); }
        }
        
        public string passWord {
            get { return user.passWord; }
            set { SetProperty(ref user.passWord, value); }
        }

        private bool _isLogging;
        public bool isLogging {
            get { return _isLogging; }
            private set { SetProperty(ref _isLogging, value); }
        }

        public RelayCommandPara<object> loginCommand { private set; get; }
        public RelayCommand clearCommand { private set; get; }


        // 用户登陆尝试，调用CS模块
        void Login(object currWindow) {
            
            String reply = CSClient.getInstance().SendAMsg($"{userName}_{passWord}");
            if (reply != "lol") {                
                return;
            }

            
            App.mainWindow = new MainWindow();
            App.mainWindow.Show();
            isLogging = false;
        }

        void clear() {
            userName = "";
            passWord = "";
        }

        public UserViewModel() {
            loginCommand = new RelayCommandPara<object>(Login) { IsEnabled = true};
            clearCommand = new RelayCommand(clear) { IsEnabled = true};
            _isLogging = true;
        }
    }
}
