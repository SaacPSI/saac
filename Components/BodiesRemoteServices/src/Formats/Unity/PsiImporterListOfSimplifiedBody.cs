using Microsoft.Psi;
using System.Collections.Generic;
using UnityEngine;

public class PsiImporterListOfSimplifiedBody : PsiImporter<List<SimplifiedBody>>
{
    public delegate void RecieveBodies(List<SimplifiedBody> message);
    public RecieveBodies onBodiesRecieved;

    protected override void Process(List<SimplifiedBody> message, Envelope enveloppe)
    {
        onBodiesRecieved(message);
        Debug.Log($"PsiImporterListOfSimplifiedBody : Message recieved with {message.Count} bodies");
    }

#if PSI_TCP_SOURCE
    protected override Microsoft.Psi.Interop.Serialization.IFormatDeserializer<List<SimplifiedBody>> GetDeserializer()
    {
        return PsiFormatListOfSimplifiedBody.GetFormat(); 
    }
#endif
}
