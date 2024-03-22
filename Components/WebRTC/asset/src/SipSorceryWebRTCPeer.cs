using System;
using System.Net;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using Microsoft.Extensions.Logging;
using SIPSorcery.Net;
using SIPSorceryMedia.Abstractions;
using WebSocketSharp.Server;
using WebRTC.OpusCodec;
using Unity.Jobs;
using Org.BouncyCastle.Utilities;

public class SipSorceryWebRTCPeer : SipSorceryWebRTCDataPeer
{
    public VideoCodecsEnum PreferedCodec = VideoCodecsEnum.VP8;
    public double FrameRateLimit = 30.0;
    public bool IsStreamAudio = false;
    
    private SIPSorceryMedia.Encoders.VideoEncoderEndPoint VideoEncoderEndPoint;
    private OpusAudioEncoder AudioEncoder;
    private RTCPeerConnection PeerConnection = null;
    private Texture2D _CameraTexture2D;
    private DateTime timestamp;
    private UnityEngine.Camera Camera;
    private bool IsConnected = false;
    private AudioFormat OpusFormat;

    struct WebRTCJob : IJob
    {
        public uint milliseconds;
        public int width, height;
        public byte[] arr;
        SIPSorceryMedia.Encoders.VideoEncoderEndPoint VideoEncoderEndPoint;

        void IJob.Execute()
        {
            VideoEncoderEndPoint.ExternalVideoSourceRawSample(milliseconds, width, height, arr, VideoPixelFormatsEnum.Bgra);
        }
    }

    public SipSorceryWebRTCPeer()
    {
    }

    public override Task Start()
    {
        Camera = gameObject.GetComponent<UnityEngine.Camera>();
        SIPSorcery.LogFactory.Set(new UnityLoggerFactory());
        logger = SIPSorcery.LogFactory.CreateLogger("webrtc");
        VideoEncoderEndPoint = new SIPSorceryMedia.Encoders.VideoEncoderEndPoint();
        _webSocketServer = new WebSocketServer(IPAddress.Any, WebsocketPort);
        _webSocketServer.AddWebSocketService<WebRTCWebSocketPeer>("/", (peer) => peer.CreatePeerConnection = CreatePeerConnection);
        _webSocketServer.Start();
        _CameraTexture2D = new Texture2D(Camera.pixelWidth, Camera.pixelHeight);
        timestamp = GetTime();
        if(IsStreamAudio)
        {
            AudioEncoder = new OpusAudioEncoder(logger);
            IsStreamAudio = gameObject.GetComponent<AudioListener>() != null;
            OpusFormat = AudioEncoder.SupportedFormats.First();
        }
        return Task.CompletedTask;
    }

    private void SendAudio(TimeSpan span)
    {
        int audioBufferSize = (int)( AudioSettings.outputSampleRate * span.TotalSeconds);
        audioBufferSize = audioBufferSize < 960 ? audioBufferSize : 960; // shloud be useless
        float[] audioBuffer = new float[/*audioBufferSize*/8192];
        for (int channel = 0; channel < 2; channel++)
        {
            AudioListener.GetOutputData(audioBuffer, channel);
            PeerConnection.SendAudio((uint)span.TotalMilliseconds, AudioEncoder.EncodeAudio(audioBuffer, OpusFormat));
        }
    }

    private void OnRenderObject()
    {
        var now = GetTime();
        TimeSpan span = now - timestamp;
        if (IsStreamAudio && IsConnected)
            SendAudio(span);

        if (span.TotalSeconds < (1.0 / FrameRateLimit) || !IsConnected) 
            return;

        RenderTexture.active = Camera.activeTexture;
        _CameraTexture2D.ReadPixels(new Rect(0, 0, Camera.pixelWidth, Camera.pixelHeight), 0, 0);
        _CameraTexture2D.Apply();
        RenderTexture.active = null;
        int width = _CameraTexture2D.width;
        int height = _CameraTexture2D.height;
      
        // This call to get the raw pixels seems to be the biggest performance hit. On my Win10 i7 machine
        // frame rate reduces from approx. 200 fps to around 20fps with this call.
        //var arr = _CameraTexture2D.GetRawTextureData();
       // WebRTCJob frame = new WebRTCJob() { milliseconds = (uint)span.TotalMilliseconds, width = width, height = height, arr = arr };
       // frame.Schedule();
        //    VideoEncoderEndPoint.ExternalVideoSourceRawSample((uint)span.TotalMilliseconds, width, height, arr, VideoPixelFormatsEnum.Bgra);
        timestamp = now;

    }

    protected override Task<SIPSorcery.Net.RTCPeerConnection> CreatePeerConnection()
    {
        PeerConnection = new RTCPeerConnection(null);
        CreateDataChannels(PeerConnection);
        // Set up sources and hook up send events to peer connection.
        var videoTrack = new SIPSorcery.Net.MediaStreamTrack(VideoEncoderEndPoint.GetVideoSourceFormats(), SIPSorcery.Net.MediaStreamStatusEnum.SendOnly);
        PeerConnection.addTrack(videoTrack);
        VideoEncoderEndPoint.OnVideoSourceEncodedSample += PeerConnection.SendVideo;
        PeerConnection.OnVideoFormatsNegotiated += WebRTCPeer_OnVideoFormatsNegotiated;

        if (IsStreamAudio)
        {
            var audioTrack = new SIPSorcery.Net.MediaStreamTrack(AudioEncoder.SupportedFormats, SIPSorcery.Net.MediaStreamStatusEnum.SendOnly);
            PeerConnection.addTrack(audioTrack);
        }
        //pc.OnAudioFormatsNegotiated += (sdpFormat) => AudioEncoder.SetAudioSourceFormat(sdpFormat.First());
        

        // Handlers to set the codecs to use on the sources once the SDP negotiation is complete.
        PeerConnection.OnTimeout += (mediaType) => logger.LogDebug($"Timeout on media {mediaType}.");
        PeerConnection.oniceconnectionstatechange += (state) => logger.LogDebug($"ICE connection state changed to {state}.");
        PeerConnection.onconnectionstatechange += (state) =>
        {
            logger.LogDebug($"Peer connection connected changed to {state}.");
            if (state == SIPSorcery.Net.RTCPeerConnectionState.connected)
            {
                IsConnected = true;
                VideoEncoderEndPoint.ForceKeyFrame();
            }
            else if (state == SIPSorcery.Net.RTCPeerConnectionState.closed || state == SIPSorcery.Net.RTCPeerConnectionState.failed)
            {
                IsConnected = false;
            }
        };

        return Task.FromResult(PeerConnection);
    }

    private void WebRTCPeer_OnVideoFormatsNegotiated(List<VideoFormat> obj)
    {
        VideoFormat format = obj.First();
        foreach (var video in obj)
        {
            if (video.Codec == PreferedCodec)
            {
                format = video;
                break;
            }
        }
        VideoEncoderEndPoint.SetVideoSinkFormat(format);
    }
}