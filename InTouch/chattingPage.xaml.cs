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

        // 绑定的聊天窗ViewModel
        public ChatViewModel chatViewModel;
        
       // 初始化，绑定ViewModel， 绑定各类应用层消息的处理函数
        public chattingPage() {
            InitializeComponent();
            msgTbx.Style = null;

            // 绑定ViewMode
            chatViewModel = new ChatViewModel();
            chatRoomList.ItemsSource = chatViewModel.chatRoomViewModels;


            // 绑定各类应用层消息的处理函数
            App.generalListener.RecvCallBack += AppProtocol.RecvData;
            App.fileListener.RecvCallBack += AppProtocol.RecvData;
            UDPListener.getInstance().RecvCallBack += AppProtocol.RecvData;
            AppProtocol.WordDealer += recvNewWord;
            AppProtocol.FileDealer += recvNewFile;
            AppProtocol.PhotoDealer += recvNewPhoto;
            AppProtocol.ControlDealer += RecvControl;
                
        }

        // 切换不同的聊天室
        private void ChatRoomList_SelectionChanged(object sender, SelectionChangedEventArgs e) {

            if (chatRoomList.SelectedIndex == -1)
                return;
            chatViewModel.selectedChatRoom = chatViewModel.chatRoomViewModels[chatRoomList.SelectedIndex];
            chatViewModel.selectedChatRoom.noReadCount = 0;

            // updata page
            RoomTitle.Content = chatViewModel.selectedChatRoom.addressInfo.Alias;
            updateUI();
        }

        // -------------------------- 文字收发和处理-----------------------------

        private void SendCircleOutlineIcon_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
            SendWordMsg();
        }

        // 发送快捷键右ctrl
        private void MsgTbx_KeyDown(object sender, KeyEventArgs e) {
            if (e.Key == Key.RightCtrl) {
                SendWordMsg();
            }
        }

        // 发送文字消息，调用应用层AppProtocol的接口封包，调用传输层P2PSender/UDPSender发送
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

            int targetPort = P2PListener.GENERALLISTENPORT;
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

            updateUI();

            msgTbx.Text = "";
        }

        // 收到新消息
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

            // 新消息分发，根据是否为当前聊天人，选加入不同的messageList，并且设置未读计数
            bool isToCurrentWindow;
            if (chatViewModel.selectedChatRoom == null)
                isToCurrentWindow = false;
            else
                isToCurrentWindow = srcId == chatViewModel.selectedChatRoom.id;

            // isToCurrentWindow = true; // TODO debug temp
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

            updateUI();

        }

        // -------------------------- 文字收发和处理 END--------------------------
        // -------------------------- 文件收发和处理------------------------------

        private void FolderPlusIcon_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
            SendFileMsg();
        }

        // 发送文件消息，调用应用层AppProtocol的接口封包，调用传输层P2PSender发送
        private void SendFileMsg() {
            // 选择文件
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

            // 调用应用层封包，调用传输层发送
            string srcId = App.user.userName;
            string destId = chatViewModel.selectedChatRoom.id;
            string targetIP = chatViewModel.selectedChatRoom.addressInfo.IPAddress;
            int targetPort = P2PListener.FILELISTENPORT;

            byte[][] dataGroup = AppProtocol.PackFile(fstream, srcId, destId);
            fstream.Close();
            P2PSender.getInstance().SendDataGroup(dataGroup, targetIP, targetPort);

            chatViewModel.selectedChatRoom.msgList.Add(new Model.Message { description = $"文件：{filename.Substring(filename.LastIndexOf('\\') + 1)}", src = "我" });

            updateUI();
        }


        // 接受到新文件碎块
        // 计入在buffer中，并且判定是否为最后分组，若为最后分组则组合拼接
        public byte[][] fileBuffer = null;
        public int recvCount = 0;
        private void recvNewFile(byte[] newData) {
            int totalNum = AppProtocol.findFileTotalNum(newData);
            if (fileBuffer == null) {
                fileBuffer = new byte[totalNum][];
            }
            int seq = AppProtocol.findFileSeq(newData);
            var task = new Task(() =>{ fileBuffer[seq - 1] = AppProtocol.UnPackFileSeg(newData); });
            task.Start();
            recvCount++;
            if (recvCount == totalNum) {
                task.Wait();
                string srcId = null;
                string destId = null;
                FileStream fstream = null;
                AppProtocol.UnPackFile(fileBuffer, newData, ref fstream, ref srcId, ref destId);

                double lenInMB = (double)fstream.Length / 1024 / 1024;
                string fileName = fstream.Name.Substring(fstream.Name.LastIndexOf('\\') + 1);
                chatViewModel.selectedChatRoom.msgList.Add(new Model.Message() {
                    msg = fstream.Name,
                    type = Model.Message.Type.File,
                    description = $"文件： {fileName} / { lenInMB.ToString("F2")}MB",
                    src = $"{srcId}:"
                });
                fstream.Close();

                updateUI();

                fileBuffer = null;
                recvCount = 0;
            }
            
        }

        // 对选中文件运行或打开程序
        private void ShowingMsgList_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            var clickedMsg = ((Model.Message)showingMsgList.SelectedItem);
            if (clickedMsg == null) {
                return;
            }
            if (clickedMsg.type == Model.Message.Type.File) {
                try { // 防止没有对应程序运行
                    System.Diagnostics.Process.Start((string)clickedMsg.msg);
                } catch (Exception) { }
            }
        }

        // -------------------------- 文件收发和处理 END---------------------
        // -------------------------- 图片表情包收发和处理 ------------------

        private void ImageIcon_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
            SendPhoto();
        }

        // 发送图片消息，调用应用层AppProtocol的接口封包，调用传输层P2PSender发送
        private void SendPhoto() {
            // 选择表情包
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

            // 封包发送
            string srcId = App.user.userName;
            string destId = chatViewModel.selectedChatRoom.id;
            string targetIP = chatViewModel.selectedChatRoom.addressInfo.IPAddress;
            int targetPort = P2PListener.GENERALLISTENPORT;
            byte[] data = AppProtocol.PackPhoto(bitmap, srcId, destId);

            P2PSender.getInstance().SendData(data, targetIP, targetPort);

            chatViewModel.selectedChatRoom.msgList.Add(new Model.Message { src = "我", msg = bitmap });

            updateUI();
        }

        // 接受到图片消息
        private void recvNewPhoto(byte[] newData) {
            string srcId = null;
            string destId = null;
            System.Drawing.Bitmap bitmap = AppProtocol.UnPackPhoto(newData, ref srcId, ref destId);

            var newMsg = new Model.Message() { description = "", src = srcId, msg = bitmap, type = Model.Message.Type.Photo };

            chatViewModel.selectedChatRoom.msgList.Add(newMsg);

            updateUI();
        }


        // 控制报文的发送散落于音视频会话，群聊创建等有需要的过程中
        // --------------------------- 收到控制报文 -------------------------

        private void RecvControl(byte[] newData) {
            string srcId = null;
            string destId = null;
            string optional = null;
            var ctype = AppProtocol.UnPackControl(newData, ref srcId, ref destId, ref optional);

            if (ctype == AppProtocol.ControlType.NEWGROUP) {
                App.addressBook.items.Add(new Model.AddressBook.Item() { UserName = srcId, GroupUserName = optional.Split(';'), Alias=srcId, isGroup=true});
                return;
            }

            string targetIP = null;
            foreach (var item in chatViewModel.chatRoomViewModels) {
                if (item.addressInfo.UserName == srcId)
                    targetIP = item.addressInfo.IPAddress;
            }
            targetIP = chatViewModel.chatRoomViewModels[0].addressInfo.IPAddress; // debugTemp
            if (targetIP == null) // 收到非好友视频邀请不处理
                return;

            switch (ctype) {
                case AppProtocol.ControlType.QAUDIO:
                    var result = MessageBox.Show($"{srcId}邀请您进行音频聊天，是否接受？", "新音频邀请", MessageBoxButton.YesNo);
                    if (result == MessageBoxResult.No) {
                        P2PSender.getInstance().SendData(AppProtocol.PackControl(AppProtocol.ControlType.RAUDIO, destId, srcId), targetIP, P2PListener.GENERALLISTENPORT);
                    } else {
                        P2PSender.getInstance().SendData(AppProtocol.PackControl(AppProtocol.ControlType.AAUDIO, destId, srcId), targetIP, P2PListener.GENERALLISTENPORT);
                        var audio = new Audio(targetIP);
                        audioWindow = new audioWindow(audio);
                        audioWindow.Show();
                    }
                    break;
                case AppProtocol.ControlType.AAUDIO:

                    break;
                case AppProtocol.ControlType.RAUDIO:
                    audioWindow.audio.isChatting = false;
                    audioWindow.Close();
                    break;
                case AppProtocol.ControlType.QVIDEO:
                    result = MessageBox.Show($"{srcId}邀请您进行视频聊天，是否接受？", "新视频邀请", MessageBoxButton.YesNo);
                    if (result == MessageBoxResult.No) {
                        P2PSender.getInstance().SendData(AppProtocol.PackControl(AppProtocol.ControlType.RVIDEO, destId, srcId), targetIP, P2PListener.GENERALLISTENPORT);
                    } else {
                        P2PSender.getInstance().SendData(AppProtocol.PackControl(AppProtocol.ControlType.AVIDEO, destId, srcId), targetIP, P2PListener.GENERALLISTENPORT);
                        var video = new Video(targetIP);
                        var videoWindow = new videoWindow(video);
                        videoWindow.Show();
                    }
                    break;
                case AppProtocol.ControlType.AVIDEO:

                    break;
                case AppProtocol.ControlType.RVIDEO:
                    videoWindow.video.isReject = true;
                    videoWindow.Close();
                    MessageBox.Show("对方拒绝了你的视频邀请");
                    break;
                default:
                    break;
            }
        }



        // --------------------------- 音频处理 -----------------------------

        // 新建音频窗口并在由音频窗口处理拨打问题
        audioWindow audioWindow = null;
        private void AudioIcon_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
            string srcId = App.user.userName;
            string destId = chatViewModel.selectedChatRoom.id;
            string targetIP = chatViewModel.selectedChatRoom.addressInfo.IPAddress;
            byte[] data = AppProtocol.PackControl(AppProtocol.ControlType.QAUDIO, srcId, destId);            

            P2PSender.getInstance().SendData(data, targetIP, P2PListener.GENERALLISTENPORT);
            var audio = new Audio(targetIP);
            audioWindow = new audioWindow(audio);
            audioWindow.Show();
        }



        // --------------------------- 视频处理 ----------------------------

        // 新建视频窗口并在由音频窗口处理拨打问题
        videoWindow videoWindow = null;
        private void VideoIcon_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
            string srcId = App.user.userName;
            string destId = chatViewModel.selectedChatRoom.id;
            string targetIP = chatViewModel.selectedChatRoom.addressInfo.IPAddress;
            byte[] data = AppProtocol.PackControl(AppProtocol.ControlType.QVIDEO, srcId, destId);          
            
            P2PSender.getInstance().SendData(data, targetIP, P2PListener.GENERALLISTENPORT);

            var video = new Video(targetIP);
            videoWindow = new videoWindow(video);
            videoWindow.Show();
        }
        


         
        // ----------------------- UI 更新处理------------------------------
        // 主要把本地发送的消息放到右侧
        // 把图片消息按照放置到对应的Image中
        private void updateUI() {
            showingMsgList.ItemsSource = null;
            showingMsgList.ItemsSource = chatViewModel.selectedChatRoom.msgList;
            if (showingMsgList.Items.Count == 0)
                return;
            showingMsgList.UpdateLayout();
            showingMsgList.ScrollIntoView(showingMsgList.Items[showingMsgList.Items.Count - 1]);
            var userName = App.user.userName;
            foreach (Model.Message item in showingMsgList.Items) {
                if (item.msg is System.Drawing.Bitmap) {
                    ListViewItem listViewItem = showingMsgList.ItemContainerGenerator.ContainerFromItem(item) as ListViewItem;
                    Image image = FindVisualChild<Image>(listViewItem);
                    image.Source = BitmaptoSrc((System.Drawing.Bitmap)item.msg);
                }

                if (item.src == "我") {
                    ListViewItem listViewItem = showingMsgList.ItemContainerGenerator.ContainerFromItem(item) as ListViewItem;
                    listViewItem.HorizontalContentAlignment = HorizontalAlignment.Right;
                }
            }
        }


        // ------------------------ 辅助函数格式转换

        // 转换函数，把winform 的bitmap类，变为wpf中的Image能使用的source
        // 引用自： https://blog.csdn.net/jiuzaizuotian2014/article/details/81279423
        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr hObject);
        private BitmapSource BitmaptoSrc(System.Drawing.Bitmap bitmap) {
            IntPtr myImagePtr = bitmap.GetHbitmap();     //创建GDI对象，返回指针 
            BitmapSource imgsource = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(myImagePtr, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());  //创建imgSource
            DeleteObject(myImagePtr);
            return imgsource;
        }


        // 控件查找修改函数，用于动态显示图片等
        // 引用自 https://www.cnblogs.com/fuchongjundream/p/3898978.html
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
