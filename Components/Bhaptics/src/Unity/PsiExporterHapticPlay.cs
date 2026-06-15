class PsiExporterBhapticsHapticPlay : PsiExporter<SAAC.Bhaptic.Helpers.HapticPlay>
{
    public PsiExporterBhapticsHapticPlay(string topicName) { TopicName = topicName; }
    public void Post(SAAC.Bhaptic.Helpers.HapticPlay message)
    {
        if (CanSend())
        {
            Out.Post(message, Timestamp);
        }
    }
#if PSI_TCP_STREAMS
    protected override Microsoft.Psi.Interop.Serialization.IFormatSerializer<SAAC.Bhaptic.Helpers.HapticPlay> GetSerializer()
    {
        return SAAC.Bhaptics.PsiFormats.PsiFormatHapticPlay.GetFormat();
    }
#endif
}