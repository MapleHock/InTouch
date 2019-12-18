using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using Microsoft.Win32;

using InTouch.ViewModel;
using InTouch.NetWork;

namespace InTouch
{
    /// <summary>
    /// chattingPage.xaml 的交互逻辑
    /// </summary>
    public partial class chattingPage : Page {

        ChatViewModel chatViewModel;

        public chattingPage() {
            InitializeComponent();
            msgTbx.Style = null;
            chatViewModel = new ChatViewModel(App.wordListener.rawMessageList);
            // temp test
            chatRoomList.ItemsSource = chatViewModel.chatRoomViewModels;

            App.wordListener.RecvNewData += recvNewMsg;

            var msgtemplate = new DataTemplate();
           
            
        } // TODO enter down send

        private void ChatRoomList_SelectionChanged(object sender, SelectionChangedEventArgs e) {

            if (chatRoomList.SelectedIndex == -1)
                return;
            chatViewModel.selectedChatRoom = chatViewModel.chatRoomViewModels[chatRoomList.SelectedIndex];

            // TODO test set

            chatViewModel.selectedChatRoom.msgList = App.wordListener.rawMessageList;

            // updata page
            RoomTitle.Content = chatViewModel.selectedChatRoom.addressInfo.Alias;
            showingMsgList.ItemsSource = null;
            showingMsgList.ItemsSource = chatViewModel.selectedChatRoom.msgList;
        }

        private void SendWordMsg() {
            if (chatRoomList.SelectedIndex == -1) {
                // TODO hint pop
                return;
            }
            if (msgTbx.Text == "") {
                // TODO hint pop
                return;
            }

            string sendString = $"{App.user.userName}:\n" +
                                $"{msgTbx.Text}";
            string targetIP = chatViewModel.selectedChatRoom.addressInfo.IPAddress;
            // TODO debug temp
            int targetPort = P2PListener.WORDMSGLISTENPORT;
            P2PSender.getInstance().SendMsg(sendString, targetIP, targetPort);
            msgTbx.Text = "";
        }

        private void SendCircleOutlineIcon_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
            SendWordMsg();
        }

        private void recvNewMsg() {
            if (chatViewModel.selectedChatRoom == null)
                return;

            showingMsgList.ItemsSource = null;
            showingMsgList.ItemsSource = chatViewModel.selectedChatRoom.msgList;
            showingMsgList.ScrollIntoView(showingMsgList.Items[showingMsgList.Items.Count - 1]);
        }

        private void SendFileMsg() {
            OpenFileDialog dlg = new OpenFileDialog();
            Nullable<bool> result = dlg.ShowDialog();
            string filename = null;
            // Process open file dialog box results
            if (result == true) {
                // Open document
                filename = dlg.FileName;

            }
            string targetIP = chatViewModel.selectedChatRoom.addressInfo.IPAddress;
            // TODO debug temp
            int targetPort = P2PListener.FILEMSGLISTENPORT;
            P2PSender.getInstance().SendFile(filename, targetIP, targetPort);
        }

        private void FolderPlusIcon_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
            SendFileMsg();
        }
    }
}
