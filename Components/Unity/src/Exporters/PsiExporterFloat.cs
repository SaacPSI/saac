using System;
using UnityEngine;
using SAAC.PsiFormats;

public class PsiExporterFloat : PsiExporter<float>
{
    public void Post(float message)
    {
        if (CanSend())
        {
            Out.Post(message, Timestamp);
        }
    }

#if PSI_TCP_STREAMS
    protected override Microsoft.Psi.Interop.Serialization.IFormatSerializer<float> GetSerializer()
    {
        return PsiFormatFloat.GetFormat();
    }
#endif
}

