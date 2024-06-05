using Microsoft.Psi;
using System.Collections.Generic;
using UnityEngine;

public class PsiImporterPing : PsiImporter<System.DateTime>
{
    private List<System.DateTime> Buffer = new List<System.DateTime>();

    protected override void Process(System.DateTime message, Envelope enveloppe)
    {
        Buffer.Add(message);
    }

    // Update is called once per frame
    void Update()
    {
        if (IsInitialized)
        {
            if (Buffer.Count > 0)
            {
                PsiManager.AddLog(".");
            }
            Buffer.Clear();
        }
    }

#if PLATFORM_ANDROID
    protected override Microsoft.Psi.Interop.Serialization.IFormatDeserializer<System.DateTime> GetDeserializer()
    {
        return PsiFormatDateTime.GetFormat();
    }
#endif
}