using Microsoft.Psi.Imaging;
using Microsoft.Psi;
using SIPSorceryMedia.Abstractions;
using SIPSorceryMedia.FFmpeg;
using SIPSorcery.Net;
using Microsoft.Psi.Audio;

namespace SAAC.WebRTC
{
    /// <summary>
    /// WebRTCVideoStream component class, working for Unreal Engine PixelStreaming and Unity (package in asset folder of this project)
    /// Use FFMPEG for codecs.
    /// Inherit form WebRTCDataConnector class to send and recieve from datachannels 
    /// </summary>
    /// Currently Audio is not working

    public class WebRTCVideoStream : WebRTCDataConnector
    {
        private FFmpegVideoEndPoint videoDecoder;
        private OpusCodec.OpusAudioEncoder audioDecoder;
        private WebRTCVideoStreamConfiguration configuration;
        private List<byte> audioArray; 
        private DateTime audioTimestamp;
        private WaveFormat? waveFormat;
        private DateTime videoTimestamp;

        /// <summary>
        /// Gets the emitter images.
        /// </summary>
        public Emitter<Shared<Image>> OutImage { get; private set; }

        /// <summary>
        /// Gets the emitter audio.
        /// </summary>
        public Emitter<AudioBuffer> OutAudio { get; private set; }

        public WebRTCVideoStream(Pipeline parent, WebRTCVideoStreamConfiguration configuration, string name = nameof(WebRTCVideoStream))
            : base(parent, configuration, name)
        {
            this.configuration = configuration;
            audioArray = new List<byte>();
            audioTimestamp = DateTime.Now;
            videoTimestamp = DateTime.Now;
            waveFormat = null;
            OutImage = parent.CreateEmitter<Shared<Image>>(parent, nameof(OutImage));
            OutAudio = parent.CreateEmitter<AudioBuffer>(this, nameof(OutAudio));
            FFmpegInit.Initialise(FfmpegLogLevelEnum.AV_LOG_TRACE, configuration.FFMPEGFullPath, logger);
            videoDecoder = new FFmpegVideoEndPoint();
            if (configuration.AudioStreaming)
            {
                audioDecoder = new OpusCodec.OpusAudioEncoder(logger);
                waveFormat = WaveFormat.Create16BitPcm(OpusCodec.OpusAudioEncoder.MEDIA_FORMAT_OPUS.ClockRate, OpusCodec.OpusAudioEncoder.MEDIA_FORMAT_OPUS.ChannelCount);
            }
        }

        protected override void PrepareActions()
        {
            if (peerConnection == null)
                return;
            base.PrepareActions();
            MediaStreamTrack videoTrack = new MediaStreamTrack(videoDecoder.GetVideoSourceFormats(), MediaStreamStatusEnum.RecvOnly);
            peerConnection.addTrack(videoTrack);
            if (configuration.AudioStreaming)
            {
                MediaStreamTrack audioTrack = new MediaStreamTrack(audioDecoder.SupportedFormats, MediaStreamStatusEnum.RecvOnly);
                peerConnection.addTrack(audioTrack);
                peerConnection.AudioStream.OnRtpPacketReceivedByIndex += AudioStream_OnRtpPacketReceivedByIndex;
            }
            peerConnection.OnAudioFormatsNegotiated += PeerConnection_OnAudioFormatsNegotiated;
            peerConnection.OnVideoFormatsNegotiated += WebRTCPeer_OnVideoFormatsNegotiated;
            peerConnection.OnVideoFrameReceived += PeerConnection_OnVideoFrameReceived;
            videoDecoder.OnVideoSinkDecodedSample += VideoEncoder_OnVideoSinkDecodedSample;
            videoDecoder.OnVideoSinkDecodedSampleFaster += VideoDecoder_OnVideoSinkDecodedSampleFaster;
        }

        private void AudioStream_OnRtpPacketReceivedByIndex(int arg1, System.Net.IPEndPoint arg2, SDPMediaTypesEnum arg3, RTPPacket arg4)
        {
            if (arg3 != SDPMediaTypesEnum.audio || waveFormat == null)
                return;

            if (arg1 == 0)
            {
                if (audioArray.Count > 0)
                {
                    OutAudio.Post(new AudioBuffer(audioArray.ToArray(), waveFormat), audioTimestamp);
                    audioArray.Clear();
                }
                audioTimestamp = pipeline.GetCurrentTime();
            }

            short[] buffer = audioDecoder.DecodeAudio(arg4.Payload, OpusCodec.OpusAudioEncoder.MEDIA_FORMAT_OPUS);
            foreach (short pcm in buffer)
                foreach (var data in BitConverter.GetBytes(pcm))
                    audioArray.Add(data);
        }

        private void PeerConnection_OnVideoFrameReceived(System.Net.IPEndPoint arg1, uint arg2, byte[] arg3, VideoFormat arg4)
        {
           videoTimestamp = pipeline.GetCurrentTime();
           videoDecoder.SetVideoSourceFormat(arg4);
           videoDecoder.GotVideoFrame(arg1, arg2, arg3, arg4);
        }

        private void VideoEncoder_OnVideoSinkDecodedSample(byte[] sample, uint width, uint height, int stride, SIPSorceryMedia.Abstractions.VideoPixelFormatsEnum pixelFormat)
        {
            PixelFormat format = GetPixelFormat(pixelFormat);
            Shared<Image> imageEnc = ImagePool.GetOrCreate((int)width, (int)height, format);
            imageEnc.Resource.CopyFrom(sample);
            OutImage.Post(imageEnc, videoTimestamp);
        }

        private void VideoDecoder_OnVideoSinkDecodedSampleFaster(RawImage rawImage)
        {
            PixelFormat format = GetPixelFormat(rawImage.PixelFormat);
            Image image = new Image(rawImage.Sample, (int)rawImage.Width, (int)rawImage.Height, (int)rawImage.Stride, format);
            Shared<Image> imageS = ImagePool.GetOrCreate((int)rawImage.Width, (int)rawImage.Height, format);
            if (configuration.PixelStreamingConnection)
                imageS.Resource.CopyFrom(rawImage.Sample);
            else
                imageS.Resource.CopyFrom(image.Flip(FlipMode.AlongHorizontalAxis));
            OutImage.Post(imageS, videoTimestamp);
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
            videoDecoder.SetVideoSourceFormat(format);
            videoDecoder.SetVideoSinkFormat(format);
        }

        private void PeerConnection_OnAudioFormatsNegotiated(List<AudioFormat> obj)
        {
            AudioFormat format = obj.Last();
        }
    }
}
