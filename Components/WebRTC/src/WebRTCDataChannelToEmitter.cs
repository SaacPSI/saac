using Microsoft.Psi;
using System.Runtime.InteropServices;
using TinyJson;

namespace WebRTC
{
    /// <summary>
    /// Empty Interface class allowing to use specialized template class
    /// </summary>
    public interface IWebRTCDataChannelToEmitter
    {
        bool Post(byte[] data, DateTime timestamp);
        bool Post(string data);
    }

    /// <summary>
    /// Specialized template class to convert en post receiving WRTCData
    /// </summary>
    public class WebRTCDataChannelToEmitter<T> : IWebRTCDataChannelToEmitter, IProducer<T> 
    {
        public Emitter<T> Out { get; private set; }

        public WebRTCDataChannelToEmitter(Pipeline parent, string name = nameof(WebRTCDataChannelToEmitter<T>), DeliveryPolicy? defaultDeliveryPolicy = null)
        {
            Out = parent.CreateEmitter<T>(parent, name);
        }

        private struct JSONStructT{ public string Timestamp; public T Data; }

        public bool Post(string data) 
        {
            try
            {
                JSONStructT structT = JSONParser.FromJson<JSONStructT>(data);
                DateTime timestamp = DateTime.Now;
                DateTime.TryParse(structT.Timestamp, out timestamp); 
                Out.Post(structT.Data, timestamp);
            }
            catch (Exception ex)
            {
                return false;
            }
            return true;
        }

        public bool Post(byte[] data, DateTime timestamp)
        {
            T dataStruct = BytesToStructure(data);
            if(dataStruct==null || dataStruct.Equals(default(T)))
                return false;
            Out.Post(dataStruct, timestamp);
            return true;
        }

        private T BytesToStructure(byte[] bytes)
        {
            int size = Marshal.SizeOf(typeof(T));
            if (bytes.Length<size)
            {
                return default(T);
            }

            IntPtr ptr = Marshal.AllocHGlobal(size);
            try
            {
                Marshal.Copy(bytes, 0, ptr, size);
                return (T) Marshal.PtrToStructure(ptr, typeof(T));
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
        }
    }
}
