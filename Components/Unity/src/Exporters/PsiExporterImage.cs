using System;
using UnityEngine;
using Microsoft.Psi.Imaging;
using Microsoft.Psi;

public class PsiExporterImage : PsiExporter<Microsoft.Psi.Imaging.Image>
{
    private Texture2D _CameraTexture2D;
    private UnityEngine.Camera Camera;
    // Start is called before the first frame update
    override public void Start()
    {
        base.Start();
        Camera = gameObject.GetComponent<UnityEngine.Camera>();
        _CameraTexture2D = new Texture2D(Camera.pixelWidth, Camera.pixelHeight);
    }

    void OnRenderObject()
    {
        if (CanSend())
        {
            RenderTexture.active = Camera.activeTexture;
            _CameraTexture2D.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
            _CameraTexture2D.Apply();
            RenderTexture.active = null;

            //byte[] data = tex.GetRawTextureData();
            //Microsoft.Psi.Imaging.Image image = new Microsoft.Psi.Imaging.Image(tex.width, tex.height, PixelFormat.BGRA_32bpp);
            //for (int x = 0; x < width; x++)
            //{
            //    for (int y = 0; y < height; y++) 
            //    {
            //        var pix = tex.GetPixel(x, y);
            //        image.SetPixel(x, y, (int)(pix.r * 255), (int)(pix.g * 255), (int)(pix.b * 255), (int)(pix.a * 255));
            //    }
            //}


            //Microsoft.Psi.Imaging.Image image = null;
            //unsafe
            //{
            //    fixed (byte* p = _CameraTexture2D.GetRawTextureData())
            //    {
            //        IntPtr ptr = (IntPtr)p;
            //        image = new Microsoft.Psi.Imaging.Image(ptr, Screen.width, Screen.height, Screen.width * 4, PixelFormat.BGRA_32bpp);
            //    }
            //}

            var jpg = _CameraTexture2D.EncodeToJPG(50);
            Microsoft.Psi.Imaging.Image image = new Microsoft.Psi.Imaging.Image(Screen.width, Screen.height, PixelFormat.BGRA_32bpp);
 
            if (image != null)
                Out.Post(image, GetCurrentTime());
        }
    }

#if PLATFORM_ANDROID
    protected override Microsoft.Psi.Interop.Serialization.IFormatSerializer<Microsoft.Psi.Imaging.Image> GetSerializer()
    {
        return PsiFormatImage.GetFormat();
    }
#endif
}
