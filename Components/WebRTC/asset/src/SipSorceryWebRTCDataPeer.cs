using System.Threading.Tasks;
using UnityEngine;
using Microsoft.Extensions.Logging;
using SIPSorcery.Net;
using WebSocketSharp.Server;
using System.Collections.Generic;
using System;

public class SipSorceryWebRTCDataPeer : MonoBehaviour
{
    public int WebsocketPort = 80;
    public string[] DataChannels;

    protected static Microsoft.Extensions.Logging.ILogger logger;
    protected WebSocketServer _webSocketServer = null;
    protected Dictionary<string, RTCDataChannel> _RTCDataChannel;
    protected PsiPipelineManager _PsiPipelineManager = null;

    public SipSorceryWebRTCDataPeer()
    {
        _RTCDataChannel = new Dictionary<string, RTCDataChannel>();
    }

    public virtual Task Start()
    {
        _PsiPipelineManager = FindAnyObjectByType<PsiPipelineManager>();
        SIPSorcery.LogFactory.Set(new UnityLoggerFactory());
        logger = SIPSorcery.LogFactory.CreateLogger("webrtc");
        _webSocketServer = new WebSocketServer(WebsocketPort);
        _webSocketServer.AddWebSocketService<WebRTCWebSocketPeer>("/", (peer) => peer.CreatePeerConnection = CreatePeerConnection);
        _webSocketServer.Start();
        return Task.CompletedTask;
    }

    public void Close(string reason)
    { 
        if(_webSocketServer != null)
            _webSocketServer.Stop();
    }

    private void OnApplicationQuit()
    {
        Close("application exit");
    }

    protected void CreateDataChannels(RTCPeerConnection pc)
    {
        foreach (string channel in DataChannels)
        {
            _RTCDataChannel.Add(channel, pc.createDataChannel(channel, null));
            _RTCDataChannel[channel].onmessage += RtChannel_onmessage;
        }
        pc.ondatachannel += Pc_ondatachannel;
        var offer = pc.createOffer(new RTCOfferOptions());
        pc.setLocalDescription(offer);
    }
    protected virtual Task<SIPSorcery.Net.RTCPeerConnection> CreatePeerConnection()
    {
        var pc = new SIPSorcery.Net.RTCPeerConnection(null);
        CreateDataChannels(pc);
        pc.OnTimeout += (mediaType) => logger.LogDebug($"Timeout on media {mediaType}.");
        pc.oniceconnectionstatechange += (state) => logger.LogDebug($"ICE connection state changed to {state}.");
        pc.onconnectionstatechange += (state) =>
        {
            logger.LogDebug($"Peer connection connected changed to {state}.");
        };

        return Task.FromResult(pc);
    }

    private void Pc_ondatachannel(RTCDataChannel obj)
    {
        obj.onmessage += Obj_onmessage;
    }

    private void Obj_onmessage(string obj)
    {
        logger.LogDebug($"Recieved message : {obj}.");
    }

    private void RtChannel_onmessage(string obj)
    {
        logger.LogDebug($"RTCChannel recieve {obj}.");
    }

    protected DateTime GetTime()
    {
        return _PsiPipelineManager ? _PsiPipelineManager.GetPipeline().GetCurrentTime() : System.DateTime.Now;
    }
    public void SendData(string channel, string message)
    {
        if (!_RTCDataChannel.ContainsKey(channel))
            return;
        JSONStruct str;
        str.Timestamp = GetTime().ToString();
        str.Data = message;
        string send = JsonUtility.ToJson(str);
        _RTCDataChannel[channel].send(send);
    }

    private struct JSONStruct { public string Timestamp; public string Data; }
}