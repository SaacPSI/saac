using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class PsiExporterDateTime : PsiExporter<System.DateTime>
{
    // Update is called once per frame
    void Update()
    {
        if (CanSend())
        {
            Out.Post(Timestamp, Timestamp);
        }
    }


#if PSI_TCP_SOURCE
    protected override Microsoft.Psi.Interop.Serialization.IFormatSerializer<DateTime> GetSerializer()
    {
        return PsiFormatDateTime.GetFormat();
    }
#endif
}
