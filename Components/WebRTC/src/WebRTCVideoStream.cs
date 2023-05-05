using Microsoft.Psi.Imaging;
using Microsoft.Psi;
using SIPSorceryMedia.Abstractions;
using SIPSorceryMedia.FFmpeg;
using SIPSorcery.Net;
using Microsoft.Psi.Audio;
using WebSocketSharp;

namespace WebRTC
{
    /// <summary>
    /// WebRTCVideoStream component class, working for Unreal Engine PixelStreaming and Unity (package in asset folder of this project)
    /// Use FFMPEG for codecs.
    /// Inherit form WebRTConnector class 
    /// </summary>
    /// Currently Audio is not working

    public class WebRTCVideoStream : WebRTConnector
    {
        private FFmpegVideoEndPoint VideoDecoder;
        private OpusCodec.OpusAudioEncoder AudioDecoder;
        private WebRTCVideoStreamConfiguration Configuration;
        private List<byte> AudioArray = new List<byte>(); 
        private DateTime AudioTimestamp = DateTime.Now;

        /// <summary>
        /// Gets the emitter images.
        /// </summary>
        public Emitter<Shared<Image>> OutImage { get; private set; }

        /// <summary>
        /// Gets the emitter audio.
        /// </summary>
        public Emitter<AudioBuffer> OutAudio { get; private set; }

        public WebRTCVideoStream(Pipeline parent, WebRTCVideoStreamConfiguration configuration, string name = nameof(WebRTCVideoStream), DeliveryPolicy? defaultDeliveryPolicy = null)
            : base(parent, configuration, name, defaultDeliveryPolicy)
        {
            Configuration = configuration;
            OutImage = parent.CreateEmitter<Shared<Image>>(this, nameof(OutImage));
            OutAudio = parent.CreateEmitter<AudioBuffer>(this, nameof(OutAudio));
            FFmpegInit.Initialise(FfmpegLogLevelEnum.AV_LOG_VERBOSE, configuration.FFMPEGFullPath);
            VideoDecoder = new FFmpegVideoEndPoint();
            if (configuration.AudioStreaming)
            {
                AudioDecoder = new OpusCodec.OpusAudioEncoder();
            }
        }

        protected override void PrepareActions()
        {
            MediaStreamTrack videoTrack = new MediaStreamTrack(VideoDecoder.GetVideoSourceFormats(), MediaStreamStatusEnum.RecvOnly);
            PeerConnection.addTrack(videoTrack);
            if (Configuration.AudioStreaming)
            {
                MediaStreamTrack audioTrack = new MediaStreamTrack(AudioDecoder.SupportedFormats, MediaStreamStatusEnum.RecvOnly);
                PeerConnection.addTrack(audioTrack);
                PeerConnection.AudioStream.OnRtpPacketReceivedByIndex += AudioStream_OnRtpPacketReceivedByIndex;
            }
            PeerConnection.OnAudioFormatsNegotiated += PeerConnection_OnAudioFormatsNegotiated;
            PeerConnection.OnVideoFormatsNegotiated += WebRTCPeer_OnVideoFormatsNegotiated;
            PeerConnection.OnVideoFrameReceived += PeerConnection_OnVideoFrameReceived;
            VideoDecoder.OnVideoSinkDecodedSample += VideoEncoder_OnVideoSinkDecodedSample;
            //VideoDecoder.OnVideoSinkDecodedSampleFaster += VideoDecoder_OnVideoSinkDecodedSampleFaster;
        }


        private void AudioStream_OnRtpPacketReceivedByIndex(int arg1, System.Net.IPEndPoint arg2, SDPMediaTypesEnum arg3, RTPPacket arg4)
        {
            if (arg3 != SDPMediaTypesEnum.audio)
                return;

            var rtp = arg4.GetBytes();
            int dataSize = rtp.Length - arg4.Header.GetBytes().Length;
            byte[] realData = rtp.SubArray(arg4.Header.Length, dataSize);
            short[] buffer = AudioDecoder.DecodeAudio(realData, OpusCodec.OpusAudioEncoder.MEDIA_FORMAT_OPUS);
            WaveFormat wave = WaveFormat.Create16BitPcm(OpusCodec.OpusAudioEncoder.MEDIA_FORMAT_OPUS.ClockRate, 1);
            foreach (short pcm in buffer)
                foreach (var data in BitConverter.GetBytes(pcm))
                    AudioArray.Add(data);

            if (AudioTimestamp.CompareTo(arg4.Header.ReceivedTime) != 0)
            {
                OutAudio.Post(new AudioBuffer(AudioArray.ToArray(), wave), AudioTimestamp.ToUniversalTime());
                AudioTimestamp = arg4.Header.ReceivedTime;
                AudioArray.Clear();
            }
        }

        private void PeerConnection_OnVideoFrameReceived(System.Net.IPEndPoint arg1, uint arg2, byte[] arg3, VideoFormat arg4)
        {
            VideoDecoder.SetVideoSourceFormat(arg4);
            VideoDecoder.GotVideoFrame(arg1, arg2, arg3, arg4);
        }

        private void VideoEncoder_OnVideoSinkDecodedSample(byte[] sample, uint width, uint height, int stride, SIPSorceryMedia.Abstractions.VideoPixelFormatsEnum pixelFormat)
        {
            PixelFormat format = GetPixelFormat(pixelFormat);
            Shared<Image> imageEnc = ImagePool.GetOrCreate((int)width, (int)height, format);
            imageEnc.Resource.CopyFrom(sample);
            OutImage.Post(imageEnc, Pipeline.GetCurrentTime());
        }

        private void VideoDecoder_OnVideoSinkDecodedSampleFaster(RawImage rawImage)
        {
            PixelFormat format = GetPixelFormat(rawImage.PixelFormat);
            Image image = new Image(rawImage.Sample, (int)rawImage.Width, (int)rawImage.Height, (int)rawImage.Stride, PixelFormat.BGR_24bpp);
            Shared<Image> imageS = ImagePool.GetOrCreate((int)rawImage.Width, (int)rawImage.Height, format);
            if(Configuration.PixelStreamingConnection)
                imageS.Resource.CopyFrom(image);
            else
                imageS.Resource.CopyFrom(image.Flip(FlipMode.AlongHorizontalAxis));
            OutImage.Post(imageS, Pipeline.GetCurrentTime());
            image.Dispose();
        }

        private PixelFormat GetPixelFormat(VideoPixelFormatsEnum pixelFormat)
        {         
            switch (pixelFormat)
            {
                case VideoPixelFormatsEnum.Bgra:
                    return PixelFormat.BGRA_32bpp;
                case VideoPixelFormatsEnum.Bgr:
                    return PixelFormat.BGR_24bpp;
                case VideoPixelFormatsEnum.Rgb:
                    return PixelFormat.RGB_24bpp;
                default:
                case VideoPixelFormatsEnum.NV12:
                case VideoPixelFormatsEnum.I420:
                    throw new Exception("PixelFormat: " + pixelFormat.ToString() + " not supported.");
            } 
        }

        private void WebRTCPeer_OnVideoFormatsNegotiated(List<VideoFormat> obj)
        {
            VideoFormat format = obj.Last();

            VideoDecoder.SetVideoSourceFormat(format);
            VideoDecoder.SetVideoSinkFormat(format);
        }

        private void PeerConnection_OnAudioFormatsNegotiated(List<AudioFormat> obj)
        {
            AudioFormat format = obj.Last();
        }
    }
}
