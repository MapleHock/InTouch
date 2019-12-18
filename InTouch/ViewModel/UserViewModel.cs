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

        void Login(object currWindow) {
            
            String reply = CSClient.getInstance().SendAMsg($"{userName}_{passWord}");
            if (reply != "lol") {                
                return;
            }

            
            var mainWindow = new MainWindow();
            mainWindow.Show();
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
