// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.Helpers
{
    using System.IO;
    using System.Windows;
    using System.Windows.Media.Imaging;
    using Microsoft.Psi;
    using Microsoft.Psi.Components;
    using Microsoft.Psi.Imaging;

    /// <summary>
    /// Component that converts a byte array stream to a shared image.
    /// </summary>
    public class BytesStreamToImage : IConsumerProducer<byte[], Shared<Image>>
    {
        private readonly string name;

        /// <summary>
        /// Initializes a new instance of the <see cref="BytesStreamToImage"/> class.
        /// </summary>
        /// <param name="parent">The parent pipeline.</param>
        /// <param name="name">The name of the component.</param>
        public BytesStreamToImage(Pipeline parent, string name = nameof(BytesStreamToImage))
        {
            this.name = name;
            this.In = parent.CreateReceiver<byte[]>(this, this.Process, $"{name}-In");
            this.Out = parent.CreateEmitter<Shared<Image>>(this, $"{name}-Out");
        }

        /// <summary>
        /// Gets the receiver for input byte arrays.
        /// </summary>
        public Receiver<byte[]> In { get; private set; }

        /// <summary>
        /// Gets the emitter for output shared images.
        /// </summary>
        public Emitter<Shared<Image>> Out { get; private set; }

        /// <summary>
        /// Processes a byte array, decoding it to a shared image.
        /// </summary>
        /// <param name="data">The byte array containing the encoded image.</param>
        /// <param name="envelope">The message envelope.</param>
        public void Process(byte[] data, Envelope envelope)
        {
            var decoder = BitmapDecoder.Create(new MemoryStream(data), BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);
            BitmapSource bitmapSource = decoder.Frames[0];
            using var img = ImagePool.GetOrCreate(bitmapSource.PixelWidth, bitmapSource.PixelHeight, bitmapSource.Format.ToPixelFormat());
            bitmapSource.CopyPixels(Int32Rect.Empty, img.Resource.ImageData, img.Resource.Stride * img.Resource.Height, img.Resource.Stride);
            this.Out.Post(img, envelope.OriginatingTime);
        }
    }
}
