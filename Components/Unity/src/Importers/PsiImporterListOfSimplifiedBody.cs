using Microsoft.Psi;
using System.Collections.Generic;
using UnityEngine;

public class PsiImporterListOfSimplifiedBody : PsiImporter<List<SimplifiedBody>>
{
 
#if PSI_TCP_STREAMS
    protected override Microsoft.Psi.Interop.Serialization.IFormatDeserializer<List<SimplifiedBody>> GetDeserializer()
    {
        return PsiFormatListOfSimplifiedBody.GetFormat(); 
    }
#endif
}
