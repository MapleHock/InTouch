using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using InTouch.Model;

using System.Windows;

namespace InTouch.NetWork {
    static class groupChat {
        public static bool isGroupChatMsg(string id) {
            if (id.Length !=0 && id[0] == 'g') 
               return true;
            else 
               return false;
        }

        public static void newGroup(string userNameList) {
            string groupId = $"g{new Random().Next(0, 1000)}";
            string srcId = groupId;
            string optional = App.user.userName + ";" + userNameList;
            string[] destIDList = userNameList.Split(';');
            foreach (var item in destIDList) {
                string recv = CSClient.getInstance().SendAMsg($"q{item}");
                switch (recv) {
                    case "error":
                        MessageBox.Show("服务器查询错误");
                        continue;
                    case "Please send the correct message.":
                        MessageBox.Show("群聊中有未知用户");
                        continue;
                    case "n":
                        continue;
                    default:
                        break;
                }
                string destId = item;
                byte[] data = AppProtocol.PackControl(AppProtocol.ControlType.NEWGROUP, srcId, destId, optional);
                P2PSender.getInstance().SendData(data, recv, P2PListener.WORDLISTENPORT);
            }

            App.addressBook.items.Add(new Model.AddressBook.Item() { UserName = srcId, GroupUserName = optional.Split(';'), Alias = srcId, isGroup = true });
        }


        public static void UnPackGroupMsg(string msg, ref string word, ref string speakerId) {
            int speakerIdEnd = msg.IndexOf('\n');
            speakerId = msg.Substring(0 + "from:".Length, speakerIdEnd - (0 + "from:".Length));
            word = msg.Substring(speakerIdEnd + 1);
        }

        public static void SendGroupWord (string word, string destId) { // destId 群Id
            AddressBook.Item targetItem = null;
            foreach (var item in App.addressBook.items) {
                if (!item.isGroup)
                    continue;
                if (item.UserName == destId) {
                    targetItem = item;
                    break;
                }
            }
            if (targetItem == null) {
                MessageBox.Show("您不属于当前群聊");
                return;
            }
            // 因为群聊中可能存在陌生人，重新向服务器查询并发送
            foreach (var item in targetItem.GroupUserName) {
                if (item == App.user.userName)
                    continue;
                string recv = CSClient.getInstance().SendAMsg($"q{item}");
                switch (recv) {
                    case "error":
                        MessageBox.Show("服务器查询错误");
                        continue;
                    case "Please send the correct message.":
                        MessageBox.Show("群聊中有未知用户");
                        continue;
                    case "n":
                        continue;                        
                    default:
                        break;
                }
                string groupId = destId;
                string singleUserId = item;
                byte[] data = AppProtocol.PackWord($"from:{App.user.userName}\n{word}", groupId, singleUserId);
                P2PSender.getInstance().SendData(data, recv, P2PListener.WORDLISTENPORT);
            }
        }
    }
}
