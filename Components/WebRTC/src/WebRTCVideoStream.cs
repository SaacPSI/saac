// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.WebRTC
{
    using Microsoft.Psi;
    using Microsoft.Psi.Audio;
    using Microsoft.Psi.Imaging;
    using SIPSorcery.Net;
    using SIPSorceryMedia.Abstractions;
    using SIPSorceryMedia.FFmpeg;

    /// <summary>
    /// WebRTC video stream component for Unreal Engine PixelStreaming and Unity.
    /// Uses FFMPEG for codecs. Inherits from WebRTCDataConnector.
    /// </summary>
    /// <remarks>
    /// Currently Audio is not fully working.
    /// </remarks>
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
        /// Initializes a new instance of the <see cref="WebRTCVideoStream"/> class.
        /// </summary>
        /// <param name="parent">The parent pipeline.</param>
        /// <param name="configuration">The configuration.</param>
        /// <param name="name">The component name.</param>
        public WebRTCVideoStream(Pipeline parent, WebRTCVideoStreamConfiguration configuration, string name = nameof(WebRTCVideoStream))
            : base(parent, configuration, name)
        {
            this.configuration = configuration;
            this.audioArray = new List<byte>();
            this.audioTimestamp = DateTime.Now;
            this.videoTimestamp = DateTime.Now;
            this.waveFormat = null;
            this.OutImage = parent.CreateEmitter<Shared<Image>>(parent, nameof(this.OutImage));
            this.OutAudio = parent.CreateEmitter<AudioBuffer>(this, nameof(this.OutAudio));
            FFmpegInit.Initialise(FfmpegLogLevelEnum.AV_LOG_TRACE, configuration.FFMPEGFullPath, this.Logger);
            this.videoDecoder = new FFmpegVideoEndPoint();
            if (configuration.AudioStreaming)
            {
                this.audioDecoder = new OpusCodec.OpusAudioEncoder(this.Logger);
                this.waveFormat = WaveFormat.Create16BitPcm(OpusCodec.OpusAudioEncoder.MEDIA_FORMAT_OPUS.ClockRate, OpusCodec.OpusAudioEncoder.MEDIA_FORMAT_OPUS.ChannelCount);
            }
        }

        /// <summary>
        /// Gets the image output emitter.
        /// </summary>
        public Emitter<Shared<Image>> OutImage { get; private set; }

        /// <summary>
        /// Gets the audio output emitter.
        /// </summary>
        public Emitter<AudioBuffer> OutAudio { get; private set; }

        /// <inheritdoc/>
        protected override void PrepareActions()
        {
            if (this.peerConnection == null)
            {
                return;
            }

            base.PrepareActions();
            MediaStreamTrack videoTrack = new MediaStreamTrack(this.videoDecoder.GetVideoSourceFormats(), MediaStreamStatusEnum.RecvOnly);
            this.peerConnection.addTrack(videoTrack);
            if (this.configuration.AudioStreaming)
            {
                MediaStreamTrack audioTrack = new MediaStreamTrack(this.audioDecoder.SupportedFormats, MediaStreamStatusEnum.RecvOnly);
                this.peerConnection.addTrack(audioTrack);
                this.peerConnection.AudioStream.OnRtpPacketReceivedByIndex += this.AudioStream_OnRtpPacketReceivedByIndex;
            }

            this.peerConnection.OnAudioFormatsNegotiated += this.PeerConnection_OnAudioFormatsNegotiated;
            this.peerConnection.OnVideoFormatsNegotiated += this.WebRTCPeer_OnVideoFormatsNegotiated;
            this.peerConnection.OnVideoFrameReceived += this.PeerConnection_OnVideoFrameReceived;
            this.videoDecoder.OnVideoSinkDecodedSample += this.VideoEncoder_OnVideoSinkDecodedSample;
            this.videoDecoder.OnVideoSinkDecodedSampleFaster += this.VideoDecoder_OnVideoSinkDecodedSampleFaster;
        }

        private void AudioStream_OnRtpPacketReceivedByIndex(int arg1, System.Net.IPEndPoint arg2, SDPMediaTypesEnum arg3, RTPPacket arg4)
        {
            if (arg3 != SDPMediaTypesEnum.audio || this.waveFormat == null)
            {
                return;
            }

            if (arg1 == 0)
            {
                if (this.audioArray.Count > 0)
                {
                    this.OutAudio.Post(new AudioBuffer(this.audioArray.ToArray(), this.waveFormat), this.audioTimestamp);
                    this.audioArray.Clear();
                }

                this.audioTimestamp = this.pipeline.GetCurrentTime();
            }

            short[] buffer = this.audioDecoder.DecodeAudio(arg4.Payload, OpusCodec.OpusAudioEncoder.MEDIA_FORMAT_OPUS);
            foreach (short pcm in buffer)
            {
                foreach (var data in BitConverter.GetBytes(pcm))
                {
                    this.audioArray.Add(data);
                }
            }
        }

        private void PeerConnection_OnVideoFrameReceived(System.Net.IPEndPoint arg1, uint arg2, byte[] arg3, VideoFormat arg4)
        {
            this.videoTimestamp = this.pipeline.GetCurrentTime();
            this.videoDecoder.SetVideoSourceFormat(arg4);
            this.videoDecoder.GotVideoFrame(arg1, arg2, arg3, arg4);
        }

        private void VideoEncoder_OnVideoSinkDecodedSample(byte[] sample, uint width, uint height, int stride, SIPSorceryMedia.Abstractions.VideoPixelFormatsEnum pixelFormat)
        {
            PixelFormat format = this.GetPixelFormat(pixelFormat);
            Shared<Image> imageEnc = ImagePool.GetOrCreate((int)width, (int)height, format);
            imageEnc.Resource.CopyFrom(sample);
            this.OutImage.Post(imageEnc, this.videoTimestamp);
        }

        private void VideoDecoder_OnVideoSinkDecodedSampleFaster(RawImage rawImage)
        {
            PixelFormat format = this.GetPixelFormat(rawImage.PixelFormat);
            Image image = new Image(rawImage.Sample, (int)rawImage.Width, (int)rawImage.Height, (int)rawImage.Stride, format);
            Shared<Image> imageS = ImagePool.GetOrCreate((int)rawImage.Width, (int)rawImage.Height, format);
            if (this.configuration.PixelStreamingConnection)
            {
                imageS.Resource.CopyFrom(rawImage.Sample);
            }
            else
            {
                imageS.Resource.CopyFrom(image.Flip(FlipMode.AlongHorizontalAxis));
            }

            this.OutImage.Post(imageS, this.videoTimestamp);
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
            this.videoDecoder.SetVideoSourceFormat(format);
            this.videoDecoder.SetVideoSinkFormat(format);
        }

        private void PeerConnection_OnAudioFormatsNegotiated(List<AudioFormat> obj)
        {
            AudioFormat format = obj.Last();
        }
    }
}
