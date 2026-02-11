// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.WebRTC
{
    using System.Runtime.InteropServices;
    using Microsoft.Psi;
    using TinyJson;

    /// <summary>
    /// Specialized template class to convert and post receiving WebRTC data.
    /// </summary>
    /// <typeparam name="T">The type of data to emit.</typeparam>
    public class WebRTCDataChannelToEmitter<T> : IWebRTCDataChannelToEmitter, IProducer<T>
    {
        private string name;

        /// <summary>
        /// Initializes a new instance of the <see cref="WebRTCDataChannelToEmitter{T}"/> class.
        /// </summary>
        /// <param name="parent">The parent pipeline.</param>
        /// <param name="name">The component name.</param>
        public WebRTCDataChannelToEmitter(Pipeline parent, string name = nameof(WebRTCDataChannelToEmitter<T>))
        {
            this.name = name;
            this.Out = parent.CreateEmitter<T>(parent, $"{name}-Out");
        }

        /// <summary>
        /// Gets the output emitter.
        /// </summary>
        public Emitter<T> Out { get; private set; }

        /// <inheritdoc/>
        public override string ToString() => this.name;

        /// <inheritdoc/>
        public bool Post(string data)
        {
            try
            {
                JsonStructT structT = JSONParser.FromJson<JsonStructT>(data);
                DateTime timestamp = DateTime.Now;
                DateTime.TryParse(structT.Timestamp, out timestamp);
                this.Out.Post(structT.Data, timestamp);
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

        /// <inheritdoc/>
        public bool Post(byte[] data, DateTime timestamp)
        {
            T dataStruct = this.BytesToStructure(data);
            if (dataStruct == null || dataStruct.Equals(default(T)))
            {
                return false;
            }

            this.Out.Post(dataStruct, timestamp);
            return true;
        }

        private T BytesToStructure(byte[] bytes)
        {
            int size = Marshal.SizeOf(typeof(T));
            if (bytes.Length < size)
            {
                return default(T);
            }

            IntPtr ptr = Marshal.AllocHGlobal(size);
            try
            {
                Marshal.Copy(bytes, 0, ptr, size);
                return (T)Marshal.PtrToStructure(ptr, typeof(T));
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
        }

        private struct JsonStructT
        {
            public string Timestamp;
            public T Data;
        }
    }
}
