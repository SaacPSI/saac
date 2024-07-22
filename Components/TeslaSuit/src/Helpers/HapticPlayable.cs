namespace SAAC.TeslaSuit
{
    public struct HapticPlayable
    {
        public ulong Id { get; private set; }
        public HapticParams HapticParams { get; private set; }

        public HapticPlayable(ulong id, HapticParams hapticParams)
        {
            Id = id;
            HapticParams = hapticParams;
        }

        public HapticPlayable(ulong id, int frequency, int amplitude, int pulseWidth, long duration)
        {
            Id = id;
            HapticParams = new HapticParams(frequency, amplitude, pulseWidth, duration);
        }
    }
}
