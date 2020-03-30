using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;



namespace InTouch.Model {
    // 通讯录模型
    public class AddressBook {
        public ObservableCollection<Item> items { get; set; }

        public AddressBook() {
            items = new ObservableCollection<Item>();
        }
        // 文件中的存储格式
        // userName(groupId);Alias;[GroupUserName...]
        public class Item {
            public string UserName { get; set; }
            public string Alias { get; set; }
            public bool isOnline { get; set; }
            public string IPAddress { get; set; }
            public bool isGroup;
            public string[] GroupUserName = null;
        }
    }

   
}
