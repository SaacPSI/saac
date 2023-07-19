using Microsoft.Psi;
using Microsoft.Psi.Components;
using System.Windows.Media.Animation;
using TinyJson;

namespace WebRTC
{
    /// <summary>
    /// Small delegate implementation for WebRTCDataReceiverToChannel class
    /// </summary>
    public class IWebRTCDataReceiverToChannel
    {
        public delegate bool OnJsonMessageDelegate(string message, string label);
        public OnJsonMessageDelegate? OnJSONMessage;
        public delegate bool OnBytesMessageDelegate(byte[] message, string label);
        public OnBytesMessageDelegate? OnBytesMessage;

        public IWebRTCDataReceiverToChannel(OnJsonMessageDelegate? onJsonMessage, OnBytesMessageDelegate? onBytesMessage)
        {
            OnJSONMessage = onJsonMessage;
            OnBytesMessage = onBytesMessage;
        }

        public void SetOnJsonMessageDelegate(OnJsonMessageDelegate jsonDelegate)
        {

            OnJSONMessage = jsonDelegate;
        }

        public void SetOnBytesMessageDelegate(OnBytesMessageDelegate bytesDelegate)
        {
            OnBytesMessage = bytesDelegate;
        }
    }

    //public interface RTCDataEntity
    //{
    //    public byte[] ToBytes();
    //}

    public class WebRTCDataReceiverToChannel<T> : IWebRTCDataReceiverToChannel, IConsumer<T>  //where T : RTCDataEntity
    {
        public Connector<T> InConnector { get; private set; }
        public Receiver<T> In => InConnector.In;

        public string Label { get; private set; }

        public WebRTCDataReceiverToChannel(Pipeline parent, string label, OnJsonMessageDelegate? onJsonMessage, OnBytesMessageDelegate? onBytesMessage, DeliveryPolicy? defaultDeliveryPolicy = null)
            : base(onJsonMessage, onBytesMessage)
        {
            Label = label;
            InConnector = parent.CreateConnector<T>(Label);
            InConnector.Out.Do(Process, defaultDeliveryPolicy);
        }

        private void Process(T message, Envelope envelope)
        {
            if(OnJSONMessage != null)
                ProcessJson(message, envelope);
            else if (OnBytesMessage != null)
                ProcessBytes(message, envelope);
        }

        private struct JSONStructT { public string Timestamp; public T Data; }

        private void ProcessJson(T message, Envelope envelope)
        {
            JSONStructT jSONStructT = new JSONStructT();
            jSONStructT.Data = message;
            jSONStructT.Timestamp = envelope.OriginatingTime.ToString();
            OnJSONMessage(JSONWriter.ToJson(jSONStructT), Label);
        }

        private unsafe void ProcessBytes(T message, Envelope envelope)
        {
            //OnBytesMessage(message.ToBytes(), Label);
            throw new NotImplementedException();
        }
    }
}
