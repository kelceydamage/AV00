namespace sensors_test.Drivers.ExpansionBoards
{
    public interface IPwmGenerator : IDevice
    {
        public int PwmMaxFrequencyHz { get; }
        public int PwmMinFrequencyHz { get; }
        public int PwmBitDepth { get; }
        public int PwmChannelCount { get; }
        public float PwmMaxValue { get; }
        public void SetFrequency(float Frequency);
        public void Reset();
        public void SetChannelPwm(int ChannelId, ushort PwmAmount);
    }
}
