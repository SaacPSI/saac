using Microsoft.Psi;
using Microsoft.Psi.Components;
using TinyJson;

namespace SAAC.WebRTC
{
    /// <summary>
    /// Small delegate implementation for WebRTCDataReceiverToChannel class
    /// </summary>
    public class IWebRTCDataReceiverToChannel
    {
        public delegate bool OnMessageDelegate<T>(T message, string label);
       
        public enum MessageType { Json, Bytes };
        public MessageType Type { get; protected set; } = MessageType.Json;

        public IWebRTCDataReceiverToChannel()
        { }

        public virtual void SetOnMessageDelegateJson(OnMessageDelegate<string> onMessage)
        {
            throw new NotImplementedException();
        }

        public virtual void SetOnMessageDelegateBytes(OnMessageDelegate<byte[]> onMessage)
        {
            throw new NotImplementedException();
        }
    }

    public class WebRTCDataReceiverToChannel<T> : IWebRTCDataReceiverToChannel, IConsumer<T>
    {
        public Connector<T> InConnector { get; private set; }
        public Receiver<T> In => InConnector.In;

        public string Label { get; private set; }

        internal WebRTCDataReceiverToChannel(Pipeline parent, string label, DeliveryPolicy? defaultDeliveryPolicy = null)
        {
            Label = label;
            InConnector = parent.CreateConnector<T>(Label);
            InConnector.Out.Do(Process, defaultDeliveryPolicy);
        }

        protected virtual void Process(T message, Envelope envelope)
        {
            throw new NotImplementedException();
        }
    }

    public class WebRTCDataReceiverToChannelJson<T> : WebRTCDataReceiverToChannel<T>
    {
        private OnMessageDelegate<string>? OnMessage;

        internal WebRTCDataReceiverToChannelJson(Pipeline parent, string label, DeliveryPolicy? defaultDeliveryPolicy = null)
            : base(parent, label, defaultDeliveryPolicy )
        {
            Type = MessageType.Json;
        }

        public override void SetOnMessageDelegateJson(OnMessageDelegate<string> onMessage)
        {
            OnMessage = onMessage;
        }

        private struct JSONStructT { public string Timestamp; public T Data; }

        protected override void Process(T message, Envelope envelope)
        {
            if (OnMessage == null)
                return;
            JSONStructT jSONStructT = new JSONStructT();
            jSONStructT.Data = message;
            jSONStructT.Timestamp = envelope.OriginatingTime.ToString();
            OnMessage(JSONWriter.ToJson(jSONStructT), Label);
        }
    }

    public class WebRTCDataReceiverToChannelBytes<T> : WebRTCDataReceiverToChannel<T>
    {
        private OnMessageDelegate<byte[]>? OnMessage;
        private System.Reflection.MethodInfo ToBytesMethod;

        internal WebRTCDataReceiverToChannelBytes(Pipeline parent, string label, System.Reflection.MethodInfo toBytesMethod, DeliveryPolicy? defaultDeliveryPolicy = null)
             : base(parent, label, defaultDeliveryPolicy)
        {
            ToBytesMethod = toBytesMethod;
            Type = MessageType.Bytes;
        }

        public override void SetOnMessageDelegateBytes(OnMessageDelegate<byte[]> onMessage)
        {
            OnMessage = onMessage;
        }

        protected override void Process(T message, Envelope envelope)
        {
            if (OnMessage == null)
                return;
            byte[] buffer = (byte[])ToBytesMethod.Invoke(message, null);
            OnMessage(buffer, Label);
        }
    }

    public static class WebRTCDataReceiverToChannelFactory
    {
        public static WebRTCDataReceiverToChannel<T> Create<T>(Pipeline parent, string label, bool hasBytesMethod = false, DeliveryPolicy? defaultDeliveryPolicy = null) 
        {
            if (hasBytesMethod)
                foreach (var method in typeof(T).GetMethods())
                    if (method.ReturnType == typeof(byte[]))
                        return new WebRTCDataReceiverToChannelBytes<T>(parent, label, method, defaultDeliveryPolicy);
            return new WebRTCDataReceiverToChannelJson<T>(parent, label, defaultDeliveryPolicy);
        }
    }
}
