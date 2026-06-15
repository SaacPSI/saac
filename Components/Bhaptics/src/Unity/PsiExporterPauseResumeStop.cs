class PsiExporterBhapticsPauseResumeStop : PsiExporter<SAAC.Bhaptic.Helpers.PauseResumeStop>
{
    public PsiExporterBhapticsPauseResumeStop(string topicName) { TopicName = topicName; }

    public void Post(SAAC.Bhaptic.Helpers.PauseResumeStop message)
    {
        if (CanSend())
        {
            Out.Post(message, Timestamp);
        }
    }
#if PSI_TCP_STREAMS
    protected override Microsoft.Psi.Interop.Serialization.IFormatSerializer<SAAC.Bhaptic.Helpers.PauseResumeStop> GetSerializer()
    {
        return SAAC.Bhaptics.PsiFormats.PsiFormatPauseResumeStop.GetFormat();
    }
#endif
}