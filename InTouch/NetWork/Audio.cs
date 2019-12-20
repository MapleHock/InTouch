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
    class Audio {
        public WaveIn recorder = null;
        public WaveOut player = null;
        BufferedWaveProvider bufferedWaveProvider = null;

        UdpClient udpSender;
        UdpClient udpListener;
        IPEndPoint ipe;
        bool isChatting = false;
        public Audio() {
            recorder = new WaveIn();
            player = new WaveOut();
            bufferedWaveProvider = new BufferedWaveProvider(recorder.WaveFormat);
        }

        public void AudioChatBegin(IPAddress address, int port) {
            // network setting 
            
            ipe = new IPEndPoint(address, port);
            udpSender = new UdpClient();

            udpListener = new UdpClient(port);  // TODO System.Net.Sockets.SocketException:“通常每个套接字地址(协议/网络地址/端口)只允许使用一次
            udpSender.Connect(ipe);
            isChatting = true;
            var task = new Task(UDPReceive);
            task.Start();

            // local setting
            try {
                isChatting = true;
                player.Init(bufferedWaveProvider);
                recorder.BufferMilliseconds = 500;
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
            player.Stop();
            recorder.StopRecording();
            player?.Dispose();
            recorder?.Dispose();
        }

        private void RecorderOnDataAvailable(object sender, WaveInEventArgs waveInEventArgs) {
            // bufferedWaveProvider.AddSamples(waveInEventArgs.Buffer, 0, waveInEventArgs.BytesRecorded);
            udpSender.Send(waveInEventArgs.Buffer, waveInEventArgs.BytesRecorded);
        }

        private void UDPReceive() {
            while (isChatting) {
                try {
                    byte[] udpBuffer = udpListener.Receive(ref ipe);
                    bufferedWaveProvider.AddSamples(udpBuffer, 0, udpBuffer.Length);
                } catch (Exception) {
                }                
            }
        }
    }
}
