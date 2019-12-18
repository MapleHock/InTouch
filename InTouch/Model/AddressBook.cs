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

        public class Item {
            public string UserName { get; set; }
            public string Alias { get; set; }
            public bool isOnline { get; set; } // TODO string?
            public string IPAddress { get; set; } // TODO cancel isOnline 0.0.0.0?
        }
    }

   
}
