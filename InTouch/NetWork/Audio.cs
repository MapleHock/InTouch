using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Windows;

namespace InTouch.NetWork {
    // 语音聊天类
    public class Audio {
        // 本地采集播放音频相关
        public WaveIn recorder = null;
        public WaveOut player = null;
        BufferedWaveProvider bufferedWaveProvider = null;
        int StandardSampleNum = 0;
        const int bufferMillisecond = 100;

        // 网络相关
        UdpClient udpSender;
        UdpClient udpListener;
        IPEndPoint RemoteListenIPE;
        IPEndPoint RemoteSendIPE;

        Thread AudioListenThread = null;
        const int AUDIOLISTENPORT = 9000;
        const int AUDIOSENDERPORT = 9001;
        public bool isChatting = false;


        public Audio(string targetIP) {
            RemoteListenIPE = new IPEndPoint(IPAddress.Parse(targetIP), AUDIOLISTENPORT);
            RemoteSendIPE = new IPEndPoint(IPAddress.Any, AUDIOSENDERPORT);
            recorder = new WaveIn();
            player = new WaveOut();
            bufferedWaveProvider = new BufferedWaveProvider(recorder.WaveFormat);
            StandardSampleNum = recorder.WaveFormat.ConvertLatencyToByteSize(bufferMillisecond);
        }

        public void AudioChatBegin() {
            // 网络部分设置
            // 打开两个UDP端口，并启动侦听线程
            isChatting = true;
            udpSender = new UdpClient(AUDIOSENDERPORT);
            udpListener = new UdpClient(AUDIOLISTENPORT);

            AudioListenThread = new Thread(UDPReceive) { Name="audio listen thread"};
            AudioListenThread.Start();

            // 本机设置
            try {
                
                player.Init(bufferedWaveProvider);
                recorder.BufferMilliseconds = bufferMillisecond;
                player.Play();
                recorder.DataAvailable += RecorderOnDataAvailable;
                recorder.StartRecording();
            } catch (Exception e) {
                MessageBox.Show(e.Message, "没有可驱动的音频输入或输出设备");
                isChatting = false;
                return;
            }

        }

        public void AudioChatEnd() {
            isChatting = false;
            AudioListenThread.Join();
            udpListener.Close();
            udpSender.Close();
            player.Stop();
            recorder.StopRecording();
            player?.Dispose();
            recorder?.Dispose();
        }

        // 一个采样周期后，发送采集的音频
        private void RecorderOnDataAvailable(object sender, WaveInEventArgs waveInEventArgs) {
            // bufferedWaveProvider.AddSamples(waveInEventArgs.Buffer, 0, waveInEventArgs.BytesRecorded);
            if (isChatting) {
                udpSender.Send(waveInEventArgs.Buffer, waveInEventArgs.BytesRecorded, RemoteListenIPE);
            }                       
        }

        // 接受到新音频并波方
        private void UDPReceive() {
            while (isChatting) {
                
                if (udpListener.Available != 0) {
                    RemoteSendIPE.Address = IPAddress.Any;
                    try {
                        byte[] udpBuffer = udpListener.Receive(ref RemoteSendIPE);
                        bufferedWaveProvider.AddSamples(udpBuffer, 0, udpBuffer.Length);
                        
                    } catch (Exception) {
                    }
                }

                Thread.Sleep(10); // 防止CPU使用率过高
            }
        }
    }
}
