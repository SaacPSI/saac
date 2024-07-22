namespace SAAC.TeslaSuit
{
    public struct HapticParams
    {
        public int Frequency { get; private set; }
        public int Amplitude { get; private set; }
        public int PulseWidth { get; private set; }
        public long Duration { get; private set; }

        public HapticParams(int frequency, int amplitude, int pulseWidth, long duration)
        {
            Frequency = frequency;
            Amplitude = amplitude;
            PulseWidth = pulseWidth;
            Duration = duration;
        }
    }
}