using Microsoft.Psi.Imaging;
using Microsoft.Psi.Interop.Serialization;
using System;
using System.IO;

namespace SAAC.PsiFormats
{ 
    public class PsiFormatImage 
    {
        public static Format<Image> GetFormat()
        {
            return new Format<Image>(WriteImage, ReadImage);
        }

        public static void WriteImage(Image image, BinaryWriter writer)
        {
            int bytesPerPixel = image.BitsPerPixel / 8;
            writer.Write(image.Width);
            writer.Write(image.Height);
            writer.Write((int)image.PixelFormat);
            writer.Write(bytesPerPixel);
            writer.Write(image.ReadBytes(image.Width * image.Height * bytesPerPixel));
        }

        public static Image ReadImage(BinaryReader reader)
        {
            int width = reader.ReadInt32();
            int height = reader.ReadInt32();
            PixelFormat format = (PixelFormat)reader.ReadInt32();
            int bytesPerPixel = reader.ReadInt32();
            byte[] data = reader.ReadBytes(width * height * bytesPerPixel);
            Microsoft.Psi.Imaging.Image image = null;
            unsafe
            {
                fixed (byte* p = data)
                {
                    IntPtr ptr = (IntPtr)p;
                    image = new Microsoft.Psi.Imaging.Image(ptr, width, height, width * bytesPerPixel, format);
                }
            }
            return image;
        }
    }
}