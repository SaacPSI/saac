using Microsoft.Psi;
using System.Collections.Generic;
using UnityEngine;

public class PsiPositionImporter : PsiImporter<System.Numerics.Vector3>
{
    private List<System.Numerics.Vector3> Buffer = new List<System.Numerics.Vector3>();

    protected override void Process(System.Numerics.Vector3 message, Envelope enveloppe)
    {
        Buffer.Add(message);
    }

    // Update is called once per frame
    void Update()
    {
        if (IsInitialized)
        {
            if(Buffer.Count > 0)
            {
                var pos = Buffer[0];
                gameObject.transform.position = new Vector3(pos.X, pos.Y, pos.Z);
            }
            Buffer.Clear();
        }
    }
}
