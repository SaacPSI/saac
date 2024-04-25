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
        public Receiver<T> In { get; private set; }

        protected string name;
        internal WebRTCDataReceiverToChannel(Pipeline parent, string label)
        {
            this.name = label;
            In = parent.CreateReceiver<T>(this, Process, $"{name}-In");
        }

        /// <inheritdoc/>
        public override string ToString() => this.name;

        protected virtual void Process(T message, Envelope envelope)
        {
            throw new NotImplementedException();
        }
    }

    public class WebRTCDataReceiverToChannelJson<T> : WebRTCDataReceiverToChannel<T>
    {
        private OnMessageDelegate<string>? OnMessage;

        internal WebRTCDataReceiverToChannelJson(Pipeline parent, string label)
            : base(parent, label)
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
            OnMessage(JSONWriter.ToJson(jSONStructT), name);
        }
    }

    public class WebRTCDataReceiverToChannelBytes<T> : WebRTCDataReceiverToChannel<T>
    {
        private OnMessageDelegate<byte[]>? onMessage;
        private System.Reflection.MethodInfo toBytesMethod;

        internal WebRTCDataReceiverToChannelBytes(Pipeline parent, string label, System.Reflection.MethodInfo toBytesMethod)
             : base(parent, label)
        {
            toBytesMethod = toBytesMethod;
            Type = MessageType.Bytes;
        }

        public override void SetOnMessageDelegateBytes(OnMessageDelegate<byte[]> onMessage)
        {
            this.onMessage = onMessage;
        }

        protected override void Process(T message, Envelope envelope)
        {
            if (onMessage == null)
                return;
            byte[] buffer = (byte[])toBytesMethod.Invoke(message, null);
            onMessage(buffer, name);
        }
    }

    public static class WebRTCDataReceiverToChannelFactory
    {
        public static WebRTCDataReceiverToChannel<T> Create<T>(Pipeline parent, string label, bool hasBytesMethod = false) 
        {
            if (hasBytesMethod)
                foreach (var method in typeof(T).GetMethods())
                    if (method.ReturnType == typeof(byte[]))
                        return new WebRTCDataReceiverToChannelBytes<T>(parent, label, method);
            return new WebRTCDataReceiverToChannelJson<T>(parent, label);
        }
    }
}
