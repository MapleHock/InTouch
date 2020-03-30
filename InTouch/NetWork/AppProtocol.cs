using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows;

namespace InTouch.NetWork {
    // 应用层数据协议
    // 定义了消息类型和各消息类型的封包函数、解包函数
    // 还定义根据传输层结果分拣报文，调用不同类消息处理函数的委托
    static class AppProtocol {

        // 对于收到不同类型的消息执行不同的委托
        // 五大消息类型，非法消息，文字消息，文件消息，图片消息，控制消息
        public delegate void RecvNewDataHandler(byte[] newData);
        public static RecvNewDataHandler WordDealer = null;
        public static RecvNewDataHandler FileDealer = null;
        public static RecvNewDataHandler PhotoDealer = null;
        public static RecvNewDataHandler ControlDealer = null; 
        public enum MessageType {
            Invalid,
            word,
            file,
            photo,
            control
        }
        
        // 由传输层收到数据引发事件时调用
        // 根据消息类型调用不同的处理函数
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
                    ControlDealer?.Invoke(dataPack);
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

        // ------------------------- 文字消息 -------------------------------
        // optional 填写
        // 私聊：       文字
        // 群聊：   from:发送者ID\n文字
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

        //--------------------------文字消息END-------------------------------

        // ------------------------- 文件消息 --------------------------------
        
        // optional 填写：   文件名;序列号seq/总分段数TotalSeg$
        
        // 打包文件为byte[][],也即完成分块
        public static byte[][] PackFile(FileStream fstream, string srcId, string destId) {
            // optional 中，先写overview，以$结尾，然后写文件数据
            byte[][] appDataPack = null;
            byte[] srcIdSeg = Encoding.UTF8.GetBytes(srcId);
            byte[] destIdSeg = Encoding.UTF8.GetBytes(destId);
            byte[] lengthSeg = null;
            byte[] fileOverviewSeg = null;
            int fileSegNum = (int)(fstream.Length / (P2PListener.byteBufferSize - 1024) + 1);
            string fileName = fstream.Name.Substring(fstream.Name.LastIndexOf('\\') + 1);

            
            appDataPack = new byte[fileSegNum][];
            
            for (int i = 0; i < fileSegNum; i++) {
                byte[] fileSeg = new byte[Math.Min((P2PListener.byteBufferSize - 1024), fstream.Length - i * (P2PListener.byteBufferSize - 1024))];

                fstream.Read(fileSeg, 0, fileSeg.Length);
                fileOverviewSeg = Encoding.UTF8.GetBytes($"{fileName};{i + 1}/{fileSegNum}$");
                lengthSeg = System.BitConverter.GetBytes(fileSeg.Length + fileOverviewSeg.Length);
                appDataPack[i] = new byte[srcIdSeg.Length + destIdSeg.Length + 1 + lengthSeg.Length + fileOverviewSeg.Length + fileSeg.Length + SeparatorNum];
                int offset = 0;
                srcIdSeg.CopyTo(appDataPack[i], offset);
                offset += srcIdSeg.Length;
                appDataPack[i][offset] = Convert.ToByte('|');
                offset++;
                destIdSeg.CopyTo(appDataPack[i], offset);
                offset += destIdSeg.Length;
                appDataPack[i][offset] = Convert.ToByte('|');
                offset++;
                appDataPack[i][offset] = (byte)MessageType.file;
                offset++;
                appDataPack[i][offset] = Convert.ToByte('|'); // 124
                offset++;
                lengthSeg.CopyTo(appDataPack[i], offset);
                offset += lengthSeg.Length;
                appDataPack[i][offset] = Convert.ToByte('$'); // 36
                offset++;
                fileOverviewSeg.CopyTo(appDataPack[i], offset);
                offset += fileOverviewSeg.Length;
                fileSeg.CopyTo(appDataPack[i], offset);
            }
          return appDataPack;
        }

        // 从收集齐全的文件块组recvDataGroup,解包组合为文件，并写入工作目录recvFile文件夹下
        public static void UnPackFile(byte[][]recvDataGroup, byte[] lastData, ref FileStream fstream, ref string srcId, ref string destId) {
            int headerEnd = indexOfBytes(lastData, Convert.ToByte('$'), 0, lastData.Length);
            int srcIdEnd = indexOfBytes(lastData, Convert.ToByte('|'), 0, headerEnd);
            srcId = Encoding.UTF8.GetString(lastData, 0, srcIdEnd - 0);
            int destIdEnd = indexOfBytes(lastData, Convert.ToByte('|'), srcIdEnd + 1, headerEnd);
            destId = Encoding.UTF8.GetString(lastData, srcIdEnd + 1, destIdEnd - srcIdEnd - 1);
            int optionalLength = BitConverter.ToInt32(lastData, destIdEnd + 3);

            int fileNameEnd = indexOfBytes(lastData, Convert.ToByte(';'), headerEnd + 1, lastData.Length - headerEnd - 1);

            string fileName = Encoding.UTF8.GetString(lastData, headerEnd + 1, fileNameEnd - headerEnd - 1);
            if (!Directory.Exists("recvFile")) {
                Directory.CreateDirectory("recvFile");
            }
            try {
                fstream = new FileStream($"recvFile\\{fileName}", FileMode.Create, FileAccess.Write);
                for (int i = 0; i < recvDataGroup.Length; i++) {
                    fstream.Write(recvDataGroup[i],0,recvDataGroup[i].Length);
                }               
            } catch (Exception e) {
                MessageBox.Show(e.Message);
                return;
            }
            
        }

        // 解包文件段，返回文件数据
        public static byte[] UnPackFileSeg(byte[] recvData) {
            int headerEnd = indexOfBytes(recvData, Convert.ToByte('$'), 0, recvData.Length);
            int srcIdEnd = indexOfBytes(recvData, Convert.ToByte('|'), 0, headerEnd);
            int destIdEnd = indexOfBytes(recvData, Convert.ToByte('|'), srcIdEnd + 1, headerEnd);
            int optionalLength = BitConverter.ToInt32(recvData, destIdEnd + 3);
            int overviewEnd = indexOfBytes(recvData, Convert.ToByte('$'), headerEnd + 1, recvData.Length - headerEnd - 1);
            int overviewLength = overviewEnd - headerEnd;
            int fileSeglength = optionalLength - overviewLength;
            byte[] fileSeg = recvData.Skip(overviewEnd).Take(fileSeglength).ToArray();
            return fileSeg;
        }

        // 辅助函数， 从文件分块的报文中查找文件总分块数
        public static int findFileTotalNum(byte[] recvData) {
            int headerEnd = indexOfBytes(recvData, Convert.ToByte('$'), 0, recvData.Length);
            int overViewEnd = indexOfBytes(recvData, Convert.ToByte('$'), headerEnd + 1, recvData.Length - headerEnd - 1);
            int slashEnd = indexOfBytes(recvData, Convert.ToByte('/'), headerEnd, overViewEnd - headerEnd);
            int totalNum = Convert.ToInt32(Encoding.UTF8.GetString(recvData, slashEnd + 1, overViewEnd - slashEnd - 1));
            return totalNum;
        }

        // 辅助函数，从文件分块的报文中查找文件序列数
        public static int findFileSeq(byte[] recvData) {
            int headerEnd = indexOfBytes(recvData, Convert.ToByte('$'), 0, recvData.Length);
            int overViewEnd = indexOfBytes(recvData, Convert.ToByte('$'), headerEnd + 1, recvData.Length - headerEnd - 1);
            int slashEnd = indexOfBytes(recvData, Convert.ToByte('/'), headerEnd, overViewEnd - headerEnd);
            int semicolonEnd = indexOfBytes(recvData, Convert.ToByte(';'), headerEnd, overViewEnd - headerEnd);
            int Seq = Convert.ToInt32(Encoding.UTF8.GetString(recvData, semicolonEnd + 1, slashEnd - semicolonEnd - 1));
            return Seq;
        }


        // ------------------------- 文件消息END------------------------------
        // ------------------------- 图片消息 --------------------------------
        // optional 填写： 图片存为bitmap并且使用jpeg格式下，用bitmap内建函数填写
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

        // ------------------------- 图片消息END -----------------------------

        // ------------------------- 控制消息 --------------------------------

        // 控制报文类型
        // 音频/视频的 请求Q， 接受A， 拒绝R
        // 新群组消息
        
        public enum ControlType {
            QAUDIO,
            AAUDIO,
            RAUDIO,
            QVIDEO,
            AVIDEO,
            RVIDEO,
            NEWGROUP
        }

        // optional 填写 controlType的编码 其他补充内容(如新群组中的群成员信息) 
        public static byte[] PackControl(ControlType controlType, string srcId, string destId, string optional = null) {
            byte[] appDataPack = null;
            byte[] srcIdSeg = Encoding.UTF8.GetBytes(srcId);
            byte[] destIdSeg = Encoding.UTF8.GetBytes(destId);
            byte[] controlSeg = { (byte)controlType };
            byte[] optionalSeg = { };
            if (optional != null) {
                optionalSeg = Encoding.UTF8.GetBytes(optional);
            }
            byte[] lengthSeg = System.BitConverter.GetBytes(controlSeg.Length + optionalSeg.Length);

            appDataPack = new byte[srcIdSeg.Length + destIdSeg.Length + 1 + lengthSeg.Length + controlSeg.Length + optionalSeg.Length + SeparatorNum];
            int offset = 0;
            srcIdSeg.CopyTo(appDataPack, offset);
            offset += srcIdSeg.Length;
            appDataPack[offset] = Convert.ToByte('|');
            offset++;
            destIdSeg.CopyTo(appDataPack, offset);
            offset += destIdSeg.Length;
            appDataPack[offset] = Convert.ToByte('|');
            offset++;
            appDataPack[offset] = (byte)MessageType.control;
            offset++;
            appDataPack[offset] = Convert.ToByte('|'); // 124
            offset++;
            lengthSeg.CopyTo(appDataPack, offset);
            offset += lengthSeg.Length;
            appDataPack[offset] = Convert.ToByte('$'); // 36
            offset++;
            controlSeg.CopyTo(appDataPack, offset);
            offset += controlSeg.Length;
            optionalSeg.CopyTo(appDataPack, offset);
            return appDataPack;
        }

        public static ControlType UnPackControl(byte[] recvData, ref string srcId, ref string destId, ref string optional) {
            int headerEnd = indexOfBytes(recvData, Convert.ToByte('$'), 0, recvData.Length);
            int srcIdEnd = indexOfBytes(recvData, Convert.ToByte('|'), 0, headerEnd);
            srcId = Encoding.UTF8.GetString(recvData, 0, srcIdEnd - 0);
            int destIdEnd = indexOfBytes(recvData, Convert.ToByte('|'), srcIdEnd + 1, headerEnd);
            destId = Encoding.UTF8.GetString(recvData, srcIdEnd + 1, destIdEnd - srcIdEnd - 1);

            int optionalLength = BitConverter.ToInt32(recvData, destIdEnd + 3);
            if (headerEnd + 2 < recvData.Length) {
                optional = Encoding.UTF8.GetString(recvData, headerEnd + 2, optionalLength - 1);
            }
            
            return (ControlType)recvData[headerEnd + 1];
        }

        // ------------------------- 控制消息END -----------------------------

        // ---------------- 辅助函数,返回第一个匹配的byte的位置，主要用于解包
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
