using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InTouch.Model {
    // 消息模型，用于之后的列表显示
    public class Message {
        public Message() {
            
        }

        public void UpdateDesciptioin() {
            switch (type) {
                case Type.Words:
                    description = (string)msg;
                    break;
                case Type.File:
                    break;
                case Type.Photo:
                    break;
                default:
                    break;
            }
        }

        // 三类型的消息
        // 文字消息，文件消息，图片（表情包）消息
        public enum Type { Words, File, Photo}
        public Type type;
        public object msg;
        public string description { get; set; }
        public string src { get; set; }
    }
}
