using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;

using InTouch.Model;

namespace InTouch.ViewModel {
    // AddressBook的ViewModel，处理本地数据和UI的交互
    public class AddressBookViewModel : ViewModelBase {
        private AddressBook _addressBook; // 对Model的指针
        public AddressBookViewModel(AddressBook addressBook) {
            this._addressBook = addressBook;
        }

        public AddressBookViewModel() {
            if (App.addressBook != null) {
                this._addressBook = addressBook;
            }                
        }

        public AddressBook addressBook {
            get { return _addressBook; }
            set { SetProperty(ref _addressBook, value); }
        }


        
    }
}
