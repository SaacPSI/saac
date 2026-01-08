using Microsoft.Psi;
using Microsoft.Psi.Components;
using Microsoft.Psi.Imaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SAAC.Helpers
{
    /// <summary>
    /// Component that crops a shared image stream to a specified rectangular area.
    /// </summary>
    public class ImageCropper : IConsumerProducer<Shared<Image>, Shared<Image>>
    {
        private string name;
        private System.Drawing.Rectangle area;

        /// <summary>
        /// Gets the receiver for incoming images.
        /// </summary>
        public Receiver<Shared<Image>> In { get; private set; }

        /// <summary>
        /// Gets the emitter for cropped images.
        /// </summary>
        public Emitter<Shared<Image>> Out { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ImageCropper"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to attach the component to.</param>
        /// <param name="area">The rectangular area to crop from the original image.</param>
        /// <param name="name">The name of the component.</param>
        public ImageCropper(Pipeline pipeline, System.Drawing.Rectangle area, string name = nameof(ImageCropper))
        {
            this.name = name;
            this.area = area;
            this.In = pipeline.CreateReceiver<Shared<Image>>(this, Process, $"{this.name}-In");
            this.Out = pipeline.CreateEmitter<Shared<Image>>(this, $"{this.name}-Out");
        }

        /// <summary>
        /// Returns the name of the component.
        /// </summary>
        public override string ToString() => this.name;

        /// <summary>
        /// Processes an incoming image by cropping it to the specified area.
        /// </summary>
        /// <param name="image">The incoming image to crop.</param>
        /// <param name="envelope">The envelope containing metadata about the image.</param>
        private void Process(Shared<Image> image, Envelope envelope)
        {
            // Create a new image with the specified crop dimensions
            Shared<Image> croppedimage = Microsoft.Psi.Imaging.ImagePool.GetOrCreate(this.area.Width, this.area.Height, image.Resource.PixelFormat);

            // Copy the memory from the original image to the cropped image depending of the offset and size.
            int bytesPerPixel = image.Resource.BitsPerPixel / 8;
            int sourceStride = image.Resource.Stride;
            int destStride = croppedimage.Resource.Stride;

            unsafe
            {
                byte* sourcePtr = (byte*)image.Resource.ImageData.ToPointer();
                byte* destPtr = (byte*)croppedimage.Resource.ImageData.ToPointer();

                // Copy each line of pixels from the source to the destination
                for (int y = 0; y < this.area.Height; y++)
                {
                    // Calculate offsets for both source and destination
                    int sourceLineOffset = (this.area.Top + y) * sourceStride + this.area.Left * bytesPerPixel;
                    int destLineOffset = y * destStride;

                    byte* sourceLine = sourcePtr + sourceLineOffset;
                    byte* destLine = destPtr + destLineOffset;

                    // Copy one line of pixels
                    System.Buffer.MemoryCopy(sourceLine, destLine, destStride, this.area.Width * bytesPerPixel);
                }
            }

            // Post the cropped image with the same originating time
            Out.Post(croppedimage, envelope.OriginatingTime);
        }
    }
}
