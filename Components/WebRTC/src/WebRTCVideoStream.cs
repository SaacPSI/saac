using Microsoft.Psi.Imaging;
using Microsoft.Psi;
using SIPSorceryMedia.Abstractions;
using SIPSorceryMedia.FFmpeg;
using SIPSorcery.Net;
using Microsoft.Psi.Audio;

namespace WebRTC
{
    /// <summary>
    /// WebRTCVideoStream component class, working for Unreal Engine PixelStreaming and Unity (package in asset folder of this project)
    /// Use FFMPEG for codecs.
    /// Inherit form WebRTCDataConnector class to send and recieve from datachannels 
    /// </summary>
    /// Currently Audio is not working

    public class WebRTCVideoStream : WebRTCDataConnector
    {
        private FFmpegVideoEndPoint VideoDecoder;
        private OpusCodec.OpusAudioEncoder AudioDecoder;
        private WebRTCVideoStreamConfiguration Configuration;
        private List<byte> AudioArray = new List<byte>(); 
        private DateTime AudioTimestamp = DateTime.Now;
        private WaveFormat? WaveFormat = null;
        private DateTime VideoTimestamp = DateTime.Now;

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
            OutImage = parent.CreateEmitter<Shared<Image>>(parent, nameof(OutImage));
            OutAudio = parent.CreateEmitter<AudioBuffer>(this, nameof(OutAudio));
            FFmpegInit.Initialise(FfmpegLogLevelEnum.AV_LOG_TRACE, configuration.FFMPEGFullPath, Logger);
            VideoDecoder = new FFmpegVideoEndPoint();
            if (configuration.AudioStreaming)
            {
                AudioDecoder = new OpusCodec.OpusAudioEncoder(Logger);
                WaveFormat = WaveFormat.Create16BitPcm(OpusCodec.OpusAudioEncoder.MEDIA_FORMAT_OPUS.ClockRate, OpusCodec.OpusAudioEncoder.MEDIA_FORMAT_OPUS.ChannelCount);
            }
        }

        protected override void PrepareActions()
        {
            if (PeerConnection == null)
                return;
            base.PrepareActions();
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
            VideoDecoder.OnVideoSinkDecodedSampleFaster += VideoDecoder_OnVideoSinkDecodedSampleFaster;
        }

        private void AudioStream_OnRtpPacketReceivedByIndex(int arg1, System.Net.IPEndPoint arg2, SDPMediaTypesEnum arg3, RTPPacket arg4)
        {
            if (arg3 != SDPMediaTypesEnum.audio || WaveFormat == null)
                return;

            if (arg1 == 0)
            {
                if (AudioArray.Count > 0)
                {
                    OutAudio.Post(new AudioBuffer(AudioArray.ToArray(), WaveFormat), AudioTimestamp);
                    AudioArray.Clear();
                }
                AudioTimestamp = Pipeline.GetCurrentTime();
            }

            short[] buffer = AudioDecoder.DecodeAudio(arg4.Payload, OpusCodec.OpusAudioEncoder.MEDIA_FORMAT_OPUS);
            foreach (short pcm in buffer)
                foreach (var data in BitConverter.GetBytes(pcm))
                    AudioArray.Add(data);

        }

        private void PeerConnection_OnVideoFrameReceived(System.Net.IPEndPoint arg1, uint arg2, byte[] arg3, VideoFormat arg4)
        {
           VideoTimestamp = Pipeline.GetCurrentTime();
           VideoDecoder.SetVideoSourceFormat(arg4);
           VideoDecoder.GotVideoFrame(arg1, arg2, arg3, arg4);
        }

        private void VideoEncoder_OnVideoSinkDecodedSample(byte[] sample, uint width, uint height, int stride, SIPSorceryMedia.Abstractions.VideoPixelFormatsEnum pixelFormat)
        {
            PixelFormat format = GetPixelFormat(pixelFormat);
            Shared<Image> imageEnc = ImagePool.GetOrCreate((int)width, (int)height, format);
            imageEnc.Resource.CopyFrom(sample);
            OutImage.Post(imageEnc, VideoTimestamp);
        }

        private void VideoDecoder_OnVideoSinkDecodedSampleFaster(RawImage rawImage)
        {
            PixelFormat format = GetPixelFormat(rawImage.PixelFormat);
            Image image = new Image(rawImage.Sample, (int)rawImage.Width, (int)rawImage.Height, (int)rawImage.Stride, format);
            Shared<Image> imageS = ImagePool.GetOrCreate((int)rawImage.Width, (int)rawImage.Height, format);
            if (Configuration.PixelStreamingConnection)
                imageS.Resource.CopyFrom(rawImage.Sample);
            else
                imageS.Resource.CopyFrom(image.Flip(FlipMode.AlongHorizontalAxis));
            OutImage.Post(imageS, VideoTimestamp);
            image.Dispose();
        }

        private PixelFormat GetPixelFormat(VideoPixelFormatsEnum pixelFormat)
        {         
            switch (pixelFormat)
            {
                case VideoPixelFormatsEnum.Bgra:
                    return PixelFormat.BGRA_32bpp;
                case VideoPixelFormatsEnum.Bgr:
                    return PixelFormat.RGB_24bpp;
                case VideoPixelFormatsEnum.Rgb:
                    return PixelFormat.BGR_24bpp;
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
