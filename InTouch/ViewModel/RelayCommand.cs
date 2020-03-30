using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace InTouch.ViewModel {
    // ViewModel的指令式基类
    // 主要用于账户登陆窗口的交互，后主要还是使用事件响应机制，不再使用这个类
    public class RelayCommand : ICommand {
        private readonly Action _handler;
        private bool _isEnabled;

        public RelayCommand(Action handler) {
            _handler = handler;
        }

        public bool IsEnabled {
            get { return _isEnabled; }
            set {
                if (value != _isEnabled) {
                    _isEnabled = value;
                    if (CanExecuteChanged != null) {
                        CanExecuteChanged(this, EventArgs.Empty);
                    }
                }
            }
        }

        public bool CanExecute(object parameter) {
            return IsEnabled;
        }

        public event EventHandler CanExecuteChanged;

        public void Execute(object parameter) {
            _handler();
        }

    }

    public class RelayCommandPara<T> : ICommand {
        private readonly Action<T> _handler;
        private bool _isEnabled;

        public RelayCommandPara(Action<T> handler) {
            _handler = handler;
        }

        public bool IsEnabled {
            get { return _isEnabled; }
            set {
                if (value != _isEnabled) {
                    _isEnabled = value;
                    if (CanExecuteChanged != null) {
                        CanExecuteChanged(this, EventArgs.Empty);
                    }
                }
            }
        }

        public bool CanExecute(object parameter) {
            return IsEnabled;
        }

        public event EventHandler CanExecuteChanged;

        public void Execute(object parameter) {
            _handler((T)parameter);
        }

    }
}
