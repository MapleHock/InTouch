using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;

using InTouch.Model;

namespace InTouch.ViewModel {
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
