using System;
using Microsoft.Psi;
using Microsoft.Psi.Components;
using Microsoft.Psi.Imaging;
using Microsoft.SqlServer.Server;

namespace SAAC.Helpers
{  
    /// <summary>
    /// Class that decode image.
    /// </summary>
    public class ImageDecoder : Subpipeline
    {
        /// <summary>
        /// Encoded Image connector
        /// </summary>
        private Connector<Shared<EncodedImage>> InConnector;

        /// <summary>
        /// Encoded Image receiver
        /// </summary>
        public Receiver<Shared<EncodedImage>> In => InConnector.In;

        /// <summary>
        /// Image emitter
        /// </summary>
        public Emitter<Shared<Image>> Out;


        public ImageDecoder(Pipeline parent, string? name = null, DeliveryPolicy? defaultDeliveryPolicy = null)
            : base(parent, name, defaultDeliveryPolicy)
        {
           
            InConnector = parent.CreateConnector<Shared<EncodedImage>>(nameof(In));
            Out = parent.CreateEmitter<Shared<Image>>(parent, nameof(Out));
            InConnector.Out.Do(Process);
        }

        private void Process(Shared<EncodedImage> data, Envelope envelope)
        {
            Image decoded = data.Resource.Decode(new ImageFromBitmapStreamDecoder());
            Shared<Image> imageS = ImagePool.GetOrCreate((int)decoded.Width, (int)decoded.Height, decoded.PixelFormat);
            imageS.Resource.CopyFrom(decoded);
            Out.Post(imageS, envelope.OriginatingTime);
        }
    }
}
