using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.IO;

using InTouch.Model;
using InTouch.NetWork;

namespace InTouch {
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application {


        public static AddressBook addressBook;

        public static User user;

        public static P2PListener wordListener;

        public static P2PListener fileListener;

        public static UDPListener udpListener;

        //public static AddressBook chattingBook = new AddressBook(); // TODO add

        static public bool LoadAddressBook(string userName) {
            addressBook = new AddressBook();
            string path = $"userInfo\\{userName}AddressList.txt";
            if (Directory.Exists("userInfo")) {
                Directory.CreateDirectory("userInfo");
            }
            if (!File.Exists(path)) {
                File.Create(path).Close();
            }
            try {                    
                using (StreamReader sr = new StreamReader(path)) {
                    string line;
                    while ((line = sr.ReadLine()) != null) {
                        string[] para = line.Split(';');
                        if (para.Length == 2) {
                            addressBook.items.Add(new AddressBook.Item() {
                                UserName = para[0],
                                Alias = para[1],
                                isGroup = false
                            });
                        } else {
                            string[] groupUser = new string[para.Length - 2];
                            for (int i = 0; i < groupUser.Length; i++) {
                                groupUser[i] = para[i + 2];
                            }
                            addressBook.items.Add(new AddressBook.Item() {
                                UserName = para[0],
                                Alias = para[1],
                                isGroup = true,
                                GroupUserName = groupUser
                            });
                        }
                        
                    }
                }
                QueryAllitem();
            } catch (Exception e) {
                MessageBox.Show(e.Message);
                return false;
            }
            return true;
        }

        public static void QueryAllitem() {
            bool isAllQuerySucc = true;
            foreach (var item in addressBook.items) {
                if (item.isGroup) { 
                    item.isOnline = false;
                    continue;
                }
                string recv = NetWork.CSClient.getInstance().SendAMsg($"q{item.UserName}");
                switch (recv) {
                    case "error":
                        isAllQuerySucc = false;
                        break;
                    case "n":
                        item.isOnline = false;
                        break;
                    default:
                        item.isOnline = true;
                        item.IPAddress = recv;
                        break;
                }
            }

            if (!isAllQuerySucc) {
                MessageBox.Show("部分好友在线状态未能成功查询");
            }
        }



        //static public void addChattingbook(AddressBook.Item newItem) {
        //    chattingBook.items.Add(newItem);
        //}


    }
}
