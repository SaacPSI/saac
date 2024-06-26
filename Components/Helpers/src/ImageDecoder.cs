﻿using Microsoft.Psi;
using Microsoft.Psi.Components;
using Microsoft.Psi.Imaging;

namespace SAAC.Helpers
{  
    /// <summary>
    /// Class that decode image.
    /// </summary>
    public class ImageDecoder : IConsumerProducer<Shared<EncodedImage>, Shared<Image>>
    {

        /// <summary>
        /// Encoded Image receiver
        /// </summary>
        public Receiver<Shared<EncodedImage>> In { get; private set; }

        /// <summary>
        /// Image emitter
        /// </summary>
        public Emitter<Shared<Image>> Out { get; private set; }

        private string name;

        public ImageDecoder(Pipeline parent, string name = nameof(ImageDecoder))
        {
            this.name = name;
            In = parent.CreateReceiver<Shared<EncodedImage>>(this, Process, $"{name}-In");
            Out = parent.CreateEmitter<Shared<Image>>(parent, nameof(Out));
        }

        /// <inheritdoc/>
        public override string ToString() => this.name;

        private void Process(Shared<EncodedImage> data, Envelope envelope)
        {
            Image decoded = data.Resource.Decode(new ImageFromBitmapStreamDecoder());
            Shared<Image> imageS = ImagePool.GetOrCreate((int)decoded.Width, (int)decoded.Height, decoded.PixelFormat);
            imageS.Resource.CopyFrom(decoded);
            Out.Post(imageS, envelope.OriginatingTime);
        }
    }
}
