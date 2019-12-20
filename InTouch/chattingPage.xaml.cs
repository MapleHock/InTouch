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
using System.Threading;



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
            chatViewModel = new ChatViewModel();
            chatRoomList.ItemsSource = chatViewModel.chatRoomViewModels;

            App.wordListener.RecvCallBack += AppProtocol.RecvData;
            App.fileListener.RecvCallBack += AppProtocol.RecvData;
            UDPListener.getInstance().RecvCallBack += AppProtocol.RecvData;
            AppProtocol.WordDealer += recvNewWord;
            AppProtocol.FileDealer += recvNewFile;
            AppProtocol.PhotoDealer += recvNewPhoto;
            var msgtemplate = new DataTemplate();                    
        }

        private void ChatRoomList_SelectionChanged(object sender, SelectionChangedEventArgs e) {

            if (chatRoomList.SelectedIndex == -1)
                return;
            chatViewModel.selectedChatRoom = chatViewModel.chatRoomViewModels[chatRoomList.SelectedIndex];
            chatViewModel.selectedChatRoom.noReadCount = 0;

            // updata page
            RoomTitle.Content = chatViewModel.selectedChatRoom.addressInfo.Alias;
            showingMsgList.ItemsSource = null;
            showingMsgList.ItemsSource = chatViewModel.selectedChatRoom.msgList;
        }

        private void SendWordMsg() {
            if (chatRoomList.SelectedIndex == -1) {
                MessageBox.Show("请选择聊天对象");
                return;
            }
            if (msgTbx.Text == "") {
                MessageBox.Show("所发消息不能为空");
                return;
            }

            
            string targetIP = chatViewModel.selectedChatRoom.addressInfo.IPAddress;

            int targetPort = P2PListener.WORDLISTENPORT;
            string srcId = App.user.userName;
            string destId = chatViewModel.selectedChatRoom.id;
            if (groupChat.isGroupChatMsg(destId)) {
                // 群聊消息单独处理
                groupChat.SendGroupWord(msgTbx.Text, destId);
                
            } else {
                byte[] data = AppProtocol.PackWord(msgTbx.Text, srcId, destId);
                if (isUDPCbx.IsChecked == true) {
                    UDPSender.getInstance().ReliableSendData(data, targetIP); // udp 发送不支持指定端口
                } else {
                    P2PSender.getInstance().SendData(data, targetIP, targetPort);
                }
            }
                     
            
            // 本地显示自己的话
            chatViewModel.selectedChatRoom.msgList.Add(new Model.Message { description = msgTbx.Text, src = "我" });
            showingMsgList.ItemsSource = null;
            showingMsgList.ItemsSource = chatViewModel.selectedChatRoom.msgList;
            showingMsgList.ScrollIntoView(showingMsgList.Items[showingMsgList.Items.Count - 1]);


            msgTbx.Text = "";
        }

        private void SendCircleOutlineIcon_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
            SendWordMsg();
        }

        private void recvNewWord(byte[] newData) {
            string srcId = null;
            string destId = null;
            string word = null;
            AppProtocol.UnPackWord(newData, ref word, ref srcId, ref destId);
            
            var newMsg = new Model.Message();
            if (groupChat.isGroupChatMsg(srcId)) {
                string realSrcId = null;
                string realword = null;
                groupChat.UnPackGroupMsg(word, ref realword, ref realSrcId);
                newMsg.description = realword;
                newMsg.src = realSrcId;
            } else {
                newMsg.description = word;
                newMsg.src = srcId;    
            }

            bool isToCurrentWindow = srcId == chatViewModel.selectedChatRoom.id;
            isToCurrentWindow = true; // TODO check other window
            if (!isToCurrentWindow) {
                foreach (var item in chatViewModel.chatRoomViewModels) {
                    if (srcId == item.id) {
                        item.noReadCount++;
                        item.msgList.Add(newMsg);
                    }
                }
                return;
            }

            if (chatViewModel.selectedChatRoom == null)
                return;

            chatViewModel.selectedChatRoom.msgList.Add(newMsg);
            showingMsgList.ItemsSource = null;
            showingMsgList.ItemsSource = chatViewModel.selectedChatRoom.msgList;
            showingMsgList.ScrollIntoView(showingMsgList.Items[showingMsgList.Items.Count - 1]);

        }

        private void MsgTbx_KeyDown(object sender, KeyEventArgs e) {
            if (e.Key == Key.RightCtrl) {
                SendWordMsg();            
            }
        }

        private void recvNewFile(byte[] newData) {
            // TODO other count
            string srcId = null;
            string destId = null;
            FileStream fstream = null;
            AppProtocol.UnPackFile(newData, ref fstream, ref srcId, ref destId);

            double lenInMB = (double)fstream.Length / 1024 / 1024;
            string fileName = fstream.Name.Substring(fstream.Name.LastIndexOf('\\') + 1);
            chatViewModel.selectedChatRoom.msgList.Add(new Model.Message() {
                msg = fstream.Name,
                type = Model.Message.Type.File,
                description = $"文件： {fileName} / { lenInMB.ToString("F2")}MB" ,
                src = $"{srcId}:"});
            fstream.Close();

            showingMsgList.ItemsSource = null;
            showingMsgList.ItemsSource = chatViewModel.selectedChatRoom.msgList;
            showingMsgList.ScrollIntoView(showingMsgList.Items[showingMsgList.Items.Count - 1]);
        }

        private void SendFileMsg() {
            OpenFileDialog dlg = new OpenFileDialog();
            Nullable<bool> result = dlg.ShowDialog();
            string filename = null;
            if (result != true) {
                return;
            }
            filename = dlg.FileName;
            FileStream fstream = null;
            try {
                fstream = new FileStream(filename, FileMode.Open, FileAccess.Read);
            } catch (Exception e) {
                MessageBox.Show(e.Message, "文件不存在");
                return;
            }
            string srcId = App.user.userName;
            string destId = chatViewModel.selectedChatRoom.id;
            string targetIP = chatViewModel.selectedChatRoom.addressInfo.IPAddress;
            int targetPort = P2PListener.FILELISTENPORT;
            byte[] data = AppProtocol.PackFile(fstream, srcId, destId);
            fstream.Close();
            P2PSender.getInstance().SendData(data, targetIP, targetPort);

            chatViewModel.selectedChatRoom.msgList.Add(new Model.Message { description = $"文件：{filename.Substring(filename.LastIndexOf('\\') + 1)}", src = "我" });
            showingMsgList.ItemsSource = null;
            showingMsgList.ItemsSource = chatViewModel.selectedChatRoom.msgList;
            showingMsgList.ScrollIntoView(showingMsgList.Items[showingMsgList.Items.Count - 1]);
        }

        private void FolderPlusIcon_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
            SendFileMsg();
        }

        private void ShowingMsgList_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            var clickedMsg = ((Model.Message)showingMsgList.SelectedItem);
            if (clickedMsg.type == Model.Message.Type.File) {
                try { // 防止没有对应程序运行
                    System.Diagnostics.Process.Start((string)clickedMsg.msg);
                } catch (Exception) { }               
            }
        }

        private void AudioIcon_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
            string targetIP = chatViewModel.selectedChatRoom.addressInfo.IPAddress;
            
            int targetPort = 9000; // TODO
            if (!isPlayAudio) {
                audio = new Audio();
                audio.AudioChatBegin(System.Net.IPAddress.Parse(targetIP), targetPort);
                isPlayAudio = true;
            } else {
                audio.AudioChatEnd();
                isPlayAudio = false;
            }
        }
        bool isPlayAudio = false;
        Audio audio; //TODO position

        private void VideoIcon_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
            string targetIP = chatViewModel.selectedChatRoom.addressInfo.IPAddress;

            if (!isPlayVideo) {
                Video video = new Video();
                if (!video.SelectedDevice()) {
                    MessageBox.Show("没有可识别的摄像设备");
                    return;
                }
                video.RecvNewFrame += RecvNewFrame;
                video.BeginVideoChatting(System.Net.IPAddress.Parse(targetIP));
                isPlayVideo = true;
            } else {
                isPlayVideo = false;
            }

        }


        
        private void RecvNewFrame(System.Drawing.Bitmap newFrame) {
            debugVideoImg.Source = BitmaptoSrc(newFrame);
        }
        bool isPlayVideo = false;

        private void ImageIcon_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Filter = "图片文件|*.jpg;*.gif;*.bmp;*.png";
            Nullable<bool> result = dlg.ShowDialog();
            string filename = null;
            if (result != true) {
                return;
            }
            filename = dlg.FileName;
            System.Drawing.Bitmap bitmap;
            try {
                bitmap = new System.Drawing.Bitmap(filename);
            } catch (Exception ex) {
                MessageBox.Show(ex.Message, "图片不存在");
                return;
            }
            string srcId = App.user.userName;
            string destId = chatViewModel.selectedChatRoom.id;
            string targetIP = chatViewModel.selectedChatRoom.addressInfo.IPAddress;
            int targetPort = P2PListener.WORDLISTENPORT;
            byte[] data = AppProtocol.PackPhoto(bitmap, srcId, destId);
            
            P2PSender.getInstance().SendData(data, targetIP, targetPort);

            chatViewModel.selectedChatRoom.msgList.Add(new Model.Message { description = $"图片：{filename.Substring(filename.LastIndexOf('\\') + 1)}", src = "我" });
            showingMsgList.ItemsSource = null;
            showingMsgList.ItemsSource = chatViewModel.selectedChatRoom.msgList;
            showingMsgList.ScrollIntoView(showingMsgList.Items[showingMsgList.Items.Count - 1]);
        }

        private void recvNewPhoto(byte[] newData) {
            string srcId = null;
            string destId = null;
            System.Drawing.Bitmap bitmap = AppProtocol.UnPackPhoto(newData, ref srcId, ref destId);

            var newMsg = new Model.Message() { description = "", src = srcId, msg = BitmaptoSrc(bitmap), type = Model.Message.Type.Photo};

            debugVideoImg.Source = BitmaptoSrc(bitmap);
            chatViewModel.selectedChatRoom.msgList.Add(newMsg);
            showingMsgList.ItemsSource = null;
            showingMsgList.ItemsSource = chatViewModel.selectedChatRoom.msgList;
            showingMsgList.ScrollIntoView(showingMsgList.Items[showingMsgList.Items.Count - 1]);

            ListViewItem listViewItem = showingMsgList.ItemContainerGenerator.ContainerFromItem(newMsg) as ListViewItem;
            Image image = FindVisualChild<Image>(listViewItem);
            image.Source = BitmaptoSrc(bitmap);
        }

        private void UpdateBitmap() {
            if (chatViewModel.selectedChatRoom == null)
                return;
            for (int i = 0; i < showingMsgList.Items.Count; i++) {
                ListViewItem listViewItem = (ListViewItem)showingMsgList.ItemContainerGenerator.ContainerFromIndex(i);
              
            }
        }


        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr hObject);
        private BitmapSource BitmaptoSrc(System.Drawing.Bitmap bitmap) {
            IntPtr myImagePtr = bitmap.GetHbitmap();     //创建GDI对象，返回指针 //TODO
            BitmapSource imgsource = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(myImagePtr, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());  //创建imgSource
            DeleteObject(myImagePtr);
            return imgsource;
        }
        
        // TODO MARK update function, including right.. image..
        
        // TODO ref
        private ChildType FindVisualChild<ChildType>(DependencyObject obj) where ChildType : DependencyObject {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++) {
                DependencyObject child = VisualTreeHelper.GetChild(obj, i);
                if (child != null && child is ChildType) {
                    return child as ChildType;
                } else {
                    ChildType childOfChildren = FindVisualChild<ChildType>(child);
                    if (childOfChildren != null) {
                        return childOfChildren;
                    }
                }
            }
            return null;

        }
    }
}
