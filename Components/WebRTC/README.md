﻿# WebRTC with SipSorcery

## Summary
This project allow the use of WebRTC protocol to stream from Unity and Unreal Engine.

## Files
* [OpusAudioEncoder](src/OpusAudioEncoder.cs) from https://github.com/sipsorcery-org/SIPSorceryMedia.SDL2 repository. We have modified it in order to use it with our log system for WebRTC and add float encoding method for Unity. 
* [WebRTCLogger](src/WebRTCLogger.cs) is the logger class used for WebRTC and SipSorcery.
* [WebRTConnectorConfiguration](src/WebRTConnectorConfiguration.cs) is the base class for configuring connection with peers.
* [WebRTConnector](src/WebRTConnector.cs) is the base internal class handling connection with peers.
* [WebRTCDataChannelToEmitter](src/WebRTCDataChannelToEmitter.cs) transform an incoming data form WebRTC data channel to a \\psi stream.
* [WebRTCDataReceiverToChannel](src/WebRTCDataReceiverToChannel.cs) tranform an incoming \\psi stream to a WebRTC data channel. 
* [WebRTCDataConnectorConfiguration](src/WebRTCDataConnectorConfiguration.cs) configuration class that inherit from *WebRTConnectorConfiguration*, adding data channels informations.
* [WebRTCDataConnector](src/WebRTCDataConnector.cs), inherit from *WebRTConnector* adding data channels management.
* [WebRTCVideoStreamConfiguration](src/WebRTCVideoStreamConfiguration.cs) configuration class that inherit from *WebRTCDataConnectorConfiguration*, adding audio and video informations.
* [WebRTCVideoStream](src/WebRTCVideoStream.cs), inherit from *WebRTCDataConnector* adding audio and video stream  management.

## Curent issues
* Audio stream is currently not working well.
* Randomised VP8 glicth (not seen for a while).
* Audio streaming with Unreal Engine provoke crash.

## Future works
* Correct issues
* Bring H264 to Unity
* DataChannel with Unreal Engine
* Test & code with STUN server and server provider.
* Test with other game engine

## WebRTC with Unity
A unity package is in the *asset* folder containg scripts and dlls.

## WebRTC with Unreal Engine
Our components can be used with PixelStreaming, with H264 encoding. We have not tested data channels as we used directly [HTTP Request](../UnrealRemoteConnector). 

## Example

        static void FullWebRTC(Pipeline p)
        {
            WebRTCVideoStreamConfiguration config = new WebRTCVideoStreamConfiguration();
            config.WebsocketAddress = System.Net.IPAddress.Parse("127.0.0.1");
            config.WebsocketPort = 80;
            config.AudioStreaming = false;
            config.PixelStreamingConnection = false;
            config.FFMPEGFullPath = "D:\\ffmpeg\\bin\\";
            config.Log = Microsoft.Extensions.Logging.LogLevel.Information;

            var emitter = new WebRTCDataChannelToEmitter<string>(p);
            var incoming = WebRTCDataReceiverToChannelFactory.Create<TimeSpan>(p, "timing");
            config.OutputChannels.Add("Events", emitter);
            config.InputChannels.Add("Timing", incoming);

            WebRTCVideoStream stream = new WebRTCVideoStream(p, config);
            var store = PsiStore.Create(p, "WebRTC", "D:\\Stores");

            store.Write(stream.OutImage.EncodeJpeg(), "Image");
            store.Write(stream.OutAudio, "Audio");
            store.Write(emitter.Out, "Events");

            var timer = Timers.Timer(p, TimeSpan.FromSeconds(1));
            timer.Out.PipeTo(incoming.In);    
        }