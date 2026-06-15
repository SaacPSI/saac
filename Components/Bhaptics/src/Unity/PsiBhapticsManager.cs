using Bhaptics.SDK2;
using UnityEngine;

public class PsiBhapticsManager : MonoBehaviour
{
    public string TopicPrefix = "Bhaptics";
    public PsiPipelineManager.ExportType ExportType;
    private PsiExporterBhapticsHapticPlay _exporterHapticPlay;
    private PsiExporterBhapticsMotorPlay _exporterMotorPlay;
    private PsiExporterBhapticsPauseResumeStop _exporterPauseResumeStop;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        BhapticsLibrary.OnPauseResumeStop += OnPauseResumeStop;
        BhapticsLibrary.OnMotorPlay += OnMotorPlay;
        BhapticsLibrary.OnHapticPlay += OnHapticPlay;

        _exporterHapticPlay = new PsiExporterBhapticsHapticPlay($"{TopicPrefix}_HapticPlay");
        _exporterHapticPlay.ExportType = ExportType;
        _exporterMotorPlay = new PsiExporterBhapticsMotorPlay($"{TopicPrefix}_MotorPlay");
        _exporterMotorPlay.ExportType = ExportType;
        _exporterPauseResumeStop = new PsiExporterBhapticsPauseResumeStop($"{TopicPrefix}_PauseResumeStop");
        _exporterPauseResumeStop.ExportType = ExportType;
    }

    public void OnHapticPlay(string eventId, int requestId, int startMillis, float intensity, float duration, float angleX, float offsetY, int count = 0)
    {
        _exporterHapticPlay.Post(new SAAC.Bhaptic.Helpers.HapticPlay(eventId, requestId, startMillis, intensity, duration, angleX, offsetY, count));
    }
    public void OnMotorPlay(int position, int requestId, int[] motors, int durationMillis)
    { 
        _exporterMotorPlay.Post(new SAAC.Bhaptic.Helpers.MotorPlay(position, requestId, motors, durationMillis));
    }
    public void OnPauseResumeStop(string eventId, int state)
    { 
        _exporterPauseResumeStop.Post(new SAAC.Bhaptic.Helpers.PauseResumeStop(eventId, (SAAC.Bhaptic.Helpers.PauseResumeStop.EPauseResumeStop)state));
    }
}
