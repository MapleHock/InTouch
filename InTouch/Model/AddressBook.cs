using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;



namespace InTouch.Model {
    public class AddressBook {
        public List<Item> items { get; set; }

        public AddressBook() {
            items = new List<Item>();
        }
        // 文件中
        // userName(groupId)/Alias/[GroupUserName...]
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
