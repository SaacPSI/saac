using System;
using UnityEngine;
using SAAC.PsiFormats;

public class PsiExporterInteger : PsiExporter<int>
{
    public void Post(int message)
    {
        if (CanSend())
        {
            Out.Post(message, Timestamp);
        }
    }

#if PSI_TCP_STREAMS
    protected override Microsoft.Psi.Interop.Serialization.IFormatSerializer<int> GetSerializer()
    {
        return PsiFormatInteger.GetFormat();
    }
#endif
}

