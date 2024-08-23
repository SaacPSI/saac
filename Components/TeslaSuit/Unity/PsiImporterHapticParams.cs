using Microsoft.Psi;
using UnityEngine;

public class PsiImporterHapticParams : PsiImporter<HapticParams>
{
    private TsHapticPlayer hapticPlayer;
    // Start is called before the first frame update
    public override void Start()
    {
        base.Start();
        hapticPlayer = FindAnyObjectByType<TsHapticPlayer>();
        if (hapticPlayer == null)
            Debug.LogError("Could not found TsHapticPlayer script ! Have you put it in your scene ?");
    }

    protected override void Process(HapticParams message, Envelope enveloppe)
    {
        if (hapticPlayer)
            hapticPlayer.CreateTouch(message.Frequency, message.Amplitude, message.PulseWidth, message.Duration);
    }

#if PSI_TCP_SOURCE
    protected override Microsoft.Psi.Interop.Serialization.IFormatDeserializer<HapticParams> GetDeserializer()
    {
        return PsiFormatHapticParams.GetFormat();
    }
#endif
}
