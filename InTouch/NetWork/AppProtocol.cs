using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows;

namespace InTouch.NetWork {
    static class AppProtocol {

        public delegate void RecvNewDataHandler(byte[] newData);
        public static RecvNewDataHandler WordDealer = null;
        public static RecvNewDataHandler FileDealer = null;
        public static RecvNewDataHandler PhotoDealer = null;
        public enum MessageType {
            Invalid,
            word,
            file,
            photo,
            control
        }
        
        public static void RecvData(byte[] dataPack) {
            int srcEnd = indexOfBytes(dataPack, Convert.ToByte('|'), 0, dataPack.Length);
            int destEnd = indexOfBytes(dataPack, Convert.ToByte('|'), srcEnd + 1, dataPack.Length);
            MessageType messageType = (MessageType)dataPack[destEnd + 1];
            switch (messageType) {
                case MessageType.Invalid:
                    break;
                case MessageType.word:
                    WordDealer?.Invoke(dataPack);
                    break;
                case MessageType.file:
                    FileDealer?.Invoke(dataPack);
                    break;
                case MessageType.photo:
                    PhotoDealer?.Invoke(dataPack);
                    break;
                case MessageType.control:
                    break;
                default:
                    break;
            }
        }
        
        const int SeparatorNum = 4;
        //---------------------- 格式 ---------------------
        //---------------------- 报头 ---------------------
        // id: 对用户——user name, 对群聊——group id
        // <src id>|<dest id>|<MessageType>|<optional bytes length>$
        // ---- optional data 文字/文件/图片等 -------
        public static byte[] PackWord(string word, string srcId, string destId) {
            byte[] appDataPack = null;
            byte[] srcIdSeg = Encoding.UTF8.GetBytes(srcId);
            byte[] destIdSeg = Encoding.UTF8.GetBytes(destId);
            byte[] wordSeg = Encoding.UTF8.GetBytes(word);
            byte[] lengthSeg = System.BitConverter.GetBytes(wordSeg.Length);
            appDataPack = new byte[srcIdSeg.Length + destIdSeg.Length + 1 + lengthSeg.Length + wordSeg.Length + SeparatorNum];
            int offset = 0;
            srcIdSeg.CopyTo(appDataPack, offset);
            offset += srcIdSeg.Length;
            appDataPack[offset] = Convert.ToByte('|');
            offset++;
            destIdSeg.CopyTo(appDataPack, offset);
            offset += destIdSeg.Length;
            appDataPack[offset] = Convert.ToByte('|');
            offset++;
            appDataPack[offset] = (byte)MessageType.word;
            offset++;
            appDataPack[offset] = Convert.ToByte('|'); // 24
            offset++;
            lengthSeg.CopyTo(appDataPack, offset);
            offset += lengthSeg.Length;
            appDataPack[offset] = Convert.ToByte('$'); // 36
            offset++;
            wordSeg.CopyTo(appDataPack, offset);
            return appDataPack;
        }

        public static void UnPackWord(byte[] recvData, ref string word, ref string srcId, ref string destId) {
            int headerEnd = indexOfBytes(recvData, Convert.ToByte('$'), 0, recvData.Length);
            int srcIdEnd = indexOfBytes(recvData, Convert.ToByte('|'), 0, headerEnd);
            srcId = Encoding.UTF8.GetString(recvData, 0, srcIdEnd - 0);
            int destIdEnd = indexOfBytes(recvData, Convert.ToByte('|'), srcIdEnd + 1, headerEnd);
            destId = Encoding.UTF8.GetString(recvData, srcIdEnd + 1, destIdEnd - srcIdEnd - 1);
            int optionalLength = BitConverter.ToInt32(recvData, destIdEnd + 3);
            
            word = Encoding.UTF8.GetString(recvData, headerEnd + 1, optionalLength);
        }

        public static byte[] PackFile(FileStream fstream, string srcId, string destId) {
            // optional 中，先写overview，以$结尾，然后写文件数据
            byte[] appDataPack = null;
            byte[] srcIdSeg = Encoding.UTF8.GetBytes(srcId);
            byte[] destIdSeg = Encoding.UTF8.GetBytes(destId);
            byte[] fileSeg = new byte[(int)fstream.Length];
            fstream.Read(fileSeg, 0, (int)fstream.Length);
            string fileName = fstream.Name.Substring(fstream.Name.LastIndexOf('\\') + 1);
            byte[] fileOverviewSeg = Encoding.UTF8.GetBytes(fileName + '$');
            byte[] lengthSeg = System.BitConverter.GetBytes(fileSeg.Length + fileOverviewSeg.Length);

            appDataPack = new byte[srcIdSeg.Length + destIdSeg.Length + 1 + lengthSeg.Length + fileOverviewSeg.Length + fileSeg.Length + SeparatorNum];
            int offset = 0;
            srcIdSeg.CopyTo(appDataPack, offset);
            offset += srcIdSeg.Length;
            appDataPack[offset] = Convert.ToByte('|');
            offset++;
            destIdSeg.CopyTo(appDataPack, offset);
            offset += destIdSeg.Length;
            appDataPack[offset] = Convert.ToByte('|');
            offset++;
            appDataPack[offset] = (byte)MessageType.file;
            offset++;
            appDataPack[offset] = Convert.ToByte('|'); // 124
            offset++;
            lengthSeg.CopyTo(appDataPack, offset);
            offset += lengthSeg.Length;
            appDataPack[offset] = Convert.ToByte('$'); // 36
            offset++;
            fileOverviewSeg.CopyTo(appDataPack, offset);
            offset += fileOverviewSeg.Length;
            fileSeg.CopyTo(appDataPack, offset);
            return appDataPack;
        }

        public static void UnPackFile(byte[] recvData, ref FileStream fstream, ref string srcId, ref string destId) {
            int headerEnd = indexOfBytes(recvData, Convert.ToByte('$'), 0, recvData.Length);
            int srcIdEnd = indexOfBytes(recvData, Convert.ToByte('|'), 0, headerEnd);
            srcId = Encoding.UTF8.GetString(recvData, 0, srcIdEnd - 0);
            int destIdEnd = indexOfBytes(recvData, Convert.ToByte('|'), srcIdEnd + 1, headerEnd);
            destId = Encoding.UTF8.GetString(recvData, srcIdEnd + 1, destIdEnd - srcIdEnd - 1);
            int optionalLength = BitConverter.ToInt32(recvData, destIdEnd + 3);

            int overviewEnd = indexOfBytes(recvData, Convert.ToByte('$'), headerEnd + 1, recvData.Length - headerEnd - 1);
            int overviewLength = overviewEnd - headerEnd;
            string fileName = Encoding.UTF8.GetString(recvData, headerEnd + 1, overviewEnd - headerEnd - 1);
            if (Directory.Exists("recvFile")) {
                Directory.CreateDirectory("recvFile");
            }
            try {
                fstream = new FileStream($"recvFile\\{fileName}", FileMode.Create, FileAccess.Write);
                fstream.Write(recvData, overviewEnd + 1, optionalLength - overviewLength);
            } catch (Exception e) {
                MessageBox.Show(e.Message);
                return;
            }
            
        }

        public static byte[] PackPhoto(System.Drawing.Bitmap bitmap, string srcId, string destId) {
            // optional 中，先写overview，以$结尾，然后写文件数据
            byte[] appDataPack = null;
            byte[] srcIdSeg = Encoding.UTF8.GetBytes(srcId);
            byte[] destIdSeg = Encoding.UTF8.GetBytes(destId);
            MemoryStream mStream = new MemoryStream();
            bitmap.Save(mStream, System.Drawing.Imaging.ImageFormat.Jpeg);
            byte[] PhotoSeg = mStream.GetBuffer();
            byte[] lengthSeg = System.BitConverter.GetBytes(PhotoSeg.Length);
            appDataPack = new byte[srcIdSeg.Length + destIdSeg.Length + 1 + lengthSeg.Length + PhotoSeg.Length + SeparatorNum];
            int offset = 0;
            srcIdSeg.CopyTo(appDataPack, offset);
            offset += srcIdSeg.Length;
            appDataPack[offset] = Convert.ToByte('|');
            offset++;
            destIdSeg.CopyTo(appDataPack, offset);
            offset += destIdSeg.Length;
            appDataPack[offset] = Convert.ToByte('|');
            offset++;
            appDataPack[offset] = (byte)MessageType.photo;
            offset++;
            appDataPack[offset] = Convert.ToByte('|'); // 124
            offset++;
            lengthSeg.CopyTo(appDataPack, offset);
            offset += lengthSeg.Length;
            appDataPack[offset] = Convert.ToByte('$'); // 36
            offset++;
            PhotoSeg.CopyTo(appDataPack, offset);
            return appDataPack;
        }

        public static System.Drawing.Bitmap UnPackPhoto(byte[] recvData, ref string srcId, ref string destId) {
            int headerEnd = indexOfBytes(recvData, Convert.ToByte('$'), 0, recvData.Length);
            int srcIdEnd = indexOfBytes(recvData, Convert.ToByte('|'), 0, headerEnd);
            srcId = Encoding.UTF8.GetString(recvData, 0, srcIdEnd - 0);
            int destIdEnd = indexOfBytes(recvData, Convert.ToByte('|'), srcIdEnd + 1, headerEnd);
            destId = Encoding.UTF8.GetString(recvData, srcIdEnd + 1, destIdEnd - srcIdEnd - 1);
            int optionalLength = BitConverter.ToInt32(recvData, destIdEnd + 3);

            MemoryStream mStream = new MemoryStream(recvData, headerEnd + 1, optionalLength);
            return (System.Drawing.Bitmap)System.Drawing.Image.FromStream(mStream);
        }

        // ---------------- 辅助函数,返回第一个匹配的byte
        private static int indexOfBytes(byte[] bytes, byte pattern, int offset, int count) {
            int i = offset;
            for (; i < count + offset; i++) {
                if (bytes[i] == pattern) {
                    break;
                }
            }
            return i;
        }
    }
}
