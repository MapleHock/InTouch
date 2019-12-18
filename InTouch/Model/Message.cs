using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InTouch.Model {
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

        public enum Type { Words, File, Photo}
        public Type type;
        public object msg;
        public string description { get; set; }
    }
}
