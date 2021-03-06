﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using InTouch.Model;

namespace InTouch.ViewModel {
    // ChatView的ViewModel，处理本地数据和UI的交互
    // 这个ViewModel与ChatPage绑定
    public class ChatRoomViewModel : ViewModelBase {
        private AddressBook.Item _addressInfo;
        private List<Message> _msgList = new List<Message>();
        private int _noReadCount;
        public string id;

        public AddressBook.Item addressInfo {
            get { return _addressInfo; }
            set { SetProperty(ref _addressInfo, value); }
        }

        public List<Message> msgList {
            get { return _msgList; }
            set { SetProperty(ref _msgList, value); }
        }

        public int noReadCount {
            get { return _noReadCount; }
            set { SetProperty(ref _noReadCount, value); }
        }
    }

    public class ChatViewModel : ViewModelBase {
        
        public ObservableCollection<ChatRoomViewModel> chatRoomViewModels { get; set; }

        public ChatRoomViewModel selectedChatRoom;

        public ChatViewModel() {
            chatRoomViewModels = new ObservableCollection<ChatRoomViewModel>();
            foreach (var item in App.addressBook.items) {
                var chatRoomViewModel = new ChatRoomViewModel();
                chatRoomViewModel.addressInfo = item; 
                chatRoomViewModel.id = item.UserName;
                //chatRoomViewModel.msgList = new List<Message>();
                chatRoomViewModels.Add(chatRoomViewModel);
            }
        }

        public void updateChatList(AddressBook.Item item) {
            var chatRoomViewModel = new ChatRoomViewModel();
            chatRoomViewModel.addressInfo = item;
            chatRoomViewModel.id = item.UserName;
            chatRoomViewModels.Add(chatRoomViewModel);
        }
    }


}
