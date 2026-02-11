// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.Helpers
{
    using Microsoft.Psi;
    using Microsoft.Psi.Components;
    using Microsoft.Psi.Imaging;

    /// <summary>
    /// Component that decodes encoded images.
    /// </summary>
    public class ImageDecoder : IConsumerProducer<Shared<EncodedImage>, Shared<Image>>
    {
        private readonly string name;

        /// <summary>
        /// Initializes a new instance of the <see cref="ImageDecoder"/> class.
        /// </summary>
        /// <param name="parent">The parent pipeline.</param>
        /// <param name="name">The name of the component.</param>
        public ImageDecoder(Pipeline parent, string name = nameof(ImageDecoder))
        {
            this.name = name;
            this.In = parent.CreateReceiver<Shared<EncodedImage>>(this, this.Process, $"{name}-In");
            this.Out = parent.CreateEmitter<Shared<Image>>(parent, nameof(this.Out));
        }

        /// <summary>
        /// Gets the encoded image receiver.
        /// </summary>
        public Receiver<Shared<EncodedImage>> In { get; private set; }

        /// <summary>
        /// Gets the decoded image emitter.
        /// </summary>
        public Emitter<Shared<Image>> Out { get; private set; }

        /// <inheritdoc/>
        public override string ToString() => this.name;

        /// <summary>
        /// Processes an encoded image, decoding it to an Image.
        /// </summary>
        /// <param name="data">The encoded image.</param>
        /// <param name="envelope">The message envelope.</param>
        private void Process(Shared<EncodedImage> data, Envelope envelope)
        {
            Image decoded = data.Resource.Decode(new ImageFromBitmapStreamDecoder());
            Shared<Image> imageS = ImagePool.GetOrCreate((int)decoded.Width, (int)decoded.Height, decoded.PixelFormat);
            imageS.Resource.CopyFrom(decoded);
            this.Out.Post(imageS, envelope.OriginatingTime);
        }
    }
}
