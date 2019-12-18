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
using NAudio.Wave;
using NAudio.Wave.SampleProviders;


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
            AppProtocol.WordDealer += recvNewWord;
            AppProtocol.FileDealer += recvNewFile;
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
            
            int targetPort = P2PListener.WORDMSGLISTENPORT;
            string srcId = App.user.userName;
            string destId = chatViewModel.selectedChatRoom.id;
            byte[] data = AppProtocol.PackWord(msgTbx.Text, srcId, destId);
            P2PSender.getInstance().SendData(data, targetIP, targetPort);
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
            bool isToCurrentWindow = srcId == chatViewModel.selectedChatRoom.id;
            isToCurrentWindow = true; // TODO check other window
            if (!isToCurrentWindow) {
                foreach (var item in chatViewModel.chatRoomViewModels) {
                    if (srcId == item.id) {
                        item.noReadCount++;
                        item.msgList.Add(new Model.Message { description = word, src = $"{srcId}:" });
                    }
                }
                return;
            }
            if (chatViewModel.selectedChatRoom == null)
                return;

            chatViewModel.selectedChatRoom.msgList.Add(new Model.Message { description = word, src = $"{srcId}:" });
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
            // Process open file dialog box results
            if (result == true) {
                // Open document
                filename = dlg.FileName;
            }
            FileStream fstream = null;
            try {
                fstream = new FileStream(filename, FileMode.Open, FileAccess.Read);
            } catch (Exception) {
                // TODO error message?
                return;
            }
            string srcId = App.user.userName;
            string destId = chatViewModel.selectedChatRoom.id;
            string targetIP = chatViewModel.selectedChatRoom.addressInfo.IPAddress;
            int targetPort = P2PListener.FILEMSGLISTENPORT;
            byte[] data = AppProtocol.PackFile(fstream, srcId, destId);
            fstream.Close();
            P2PSender.getInstance().SendData(data, targetIP, targetPort);
        }

        private void FolderPlusIcon_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
            SendFileMsg();
        }

        private void ShowingMsgList_Selected(object sender, RoutedEventArgs e) {

        }

        private void ShowingMsgList_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            var clickedMsg = ((Model.Message)showingMsgList.SelectedItem);
            if (clickedMsg.type == Model.Message.Type.File) {
                System.Diagnostics.Process.Start((string)clickedMsg.msg);
            }
        }

        private void AudioIcon_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {

            //var outputDevice = new WaveOutEvent();

            //var audioFile = new AudioFileReader("test.mp3");
            //wavein
            //outputDevice.Init(audioFile);
            //outputDevice.Play();
            // set up the recorder

            //recorder = new WaveIn();
            //recorder.DataAvailable += RecorderOnDataAvailable;

            //bufferedWaveProvider = new BufferedWaveProvider(recorder.WaveFormat);
            //player = new WaveOut();
            //player.Init(bufferedWaveProvider);
            //player.Play();
            //recorder.StartRecording();
            string targetIP = chatViewModel.selectedChatRoom.addressInfo.IPAddress;
           
            int targetPort = 9000;
            if (!isPlay) {
                audio = new Audio();
                audio.AudioChatBegin(System.Net.IPAddress.Parse(targetIP), targetPort);
                isPlay = true;
            } else {
                audio.AudioChatEnd();
                isPlay = false;
            }
        }
        private WaveIn recorder;
        private BufferedWaveProvider bufferedWaveProvider;
        private WaveOut player;

        Audio audio = new Audio();
        bool isPlay = false;

        private void RecorderOnDataAvailable(object sender, WaveInEventArgs waveInEventArgs) {
            bufferedWaveProvider.AddSamples(waveInEventArgs.Buffer, 0, waveInEventArgs.BytesRecorded);
        }
    }
}
