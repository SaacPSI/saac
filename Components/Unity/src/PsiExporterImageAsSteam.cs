using System;
using UnityEngine;

public class PsiImageExporterAsStream : PsiExporter<byte[]>
{
    public float FramePerSecond = 15.0f;
    public int JpegEncodingLevel = 50;
    private DateTime Timestamp = DateTime.UtcNow;
    private Texture2D CameraTexture2D;
    private UnityEngine.Camera Camera;
    private float FrameTime;
    // Start is called before the first frame update
    override public void Start()
    {
        base.Start();
        Camera = gameObject.GetComponent<UnityEngine.Camera>();
        CameraTexture2D = new Texture2D(Camera.pixelWidth, Camera.pixelHeight);
        FrameTime = 1.0f / FramePerSecond;
    }

    void OnRenderObject()
    {
        var now = GetCurrentTime();
        if (CanSend() && Timestamp != now && (now.Subtract(Timestamp).TotalSeconds) > FrameTime)
        {
            RenderTexture.active = Camera.activeTexture;
            CameraTexture2D.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
            CameraTexture2D.Apply();
            RenderTexture.active = null;
            Out.Post(CameraTexture2D.EncodeToJPG(JpegEncodingLevel), now);
            Timestamp = now;
        }
    }

#if PLATFORM_ANDROID
    protected override Microsoft.Psi.Interop.Serialization.IFormatSerializer<byte[]> GetSerializer()
    {
        return PsiFormatBytes.GetFormat();
    }
#endif
}
