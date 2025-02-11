using System;
using UnityEngine;
using SAAC.PsiFormats;

public class PsiExporterString : PsiExporter<System.String>
{
    public void Post(string message)
    {
        if (CanSend())
        {
            var now = GetCurrentTime();
            Out.Post(message, now);
            PsiManager.AddLog(message);
        }
    }


#if PSI_TCP_STREAMS
    protected override Microsoft.Psi.Interop.Serialization.IFormatSerializer<System.String> GetSerializer()
    {
        return PsiFormatString.GetFormat();
    }
#endif
}
