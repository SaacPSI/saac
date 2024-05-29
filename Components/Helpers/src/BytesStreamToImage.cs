using Microsoft.Psi;
using Microsoft.Psi.Components;
using Microsoft.Psi.Imaging;
using System.IO;
using System.Windows.Media.Imaging;
using System.Windows;

namespace SAAC.Helpers
{
    public class BytesStreamToImage : IConsumerProducer<byte[], Shared<Image>>
    {
        public Receiver<byte[]> In { get; private set; }
        public Emitter<Shared<Image>> Out { get; private set; }

        public BytesStreamToImage(Pipeline parent, string name = nameof(BytesStreamToImage))
        {
            In = parent.CreateReceiver<byte[]>(this, Process, $"{name}-In");
            Out = parent.CreateEmitter<Shared<Image>>(this, $"{name}-Out");
        }

        public void Process(byte[] data, Envelope envelope)
        {
            var decoder = BitmapDecoder.Create(new MemoryStream(data), BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);
            BitmapSource bitmapSource = decoder.Frames[0];
            using var img = ImagePool.GetOrCreate(bitmapSource.PixelWidth, bitmapSource.PixelHeight, bitmapSource.Format.ToPixelFormat());
            bitmapSource.CopyPixels(Int32Rect.Empty, img.Resource.ImageData, img.Resource.Stride * img.Resource.Height, img.Resource.Stride);
            Out.Post(img, envelope.OriginatingTime);
        }
    }
}
