class PsiExporterBhapticsMotorPlay : PsiExporter<SAAC.Bhaptic.Helpers.MotorPlay>
{
    public PsiExporterBhapticsMotorPlay(string topicName) { TopicName = topicName; }
    public void Post(SAAC.Bhaptic.Helpers.MotorPlay message)
    {
        if (CanSend())
        {
            Out.Post(message, Timestamp);
        }
    }

#if PSI_TCP_STREAMS
    protected override Microsoft.Psi.Interop.Serialization.IFormatSerializer<SAAC.Bhaptic.Helpers.MotorPlay> GetSerializer()
    {
        return SAAC.Bhaptics.PsiFormats.PsiFormatMotorPlay.GetFormat();
    }
#endif
}