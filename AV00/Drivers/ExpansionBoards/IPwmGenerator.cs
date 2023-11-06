namespace AV00.Drivers.ExpansionBoards
{
    public interface IPwmGenerator : IDevice
    {
        public int PwmMaxFrequencyHz { get; }
        public int PwmMinFrequencyHz { get; }
        public int PwmBitDepth { get; }
        public int PwmChannelCount { get; }
        public float PwmMaxValue { get; }
        public float PwmMaxPercent { get; }
        public void SetFrequency(int Frequency);
        public EnumBoardStatus Init();
        public void SetChannelPwm(int ChannelId, float PwmAmountPercent);
    }
    public enum EnumBoardStatus : byte
    {
        StatusOk = 0x00,
        StatusError = 0x01,
        StatusErrorDeviceNotDetected = 0x02,
        StatusErrorSoftwareVersion = 0x03,
        StatusErrorParameter = 0x04,
        StatusErrorUnableToRead = 0x05,
        StatusErrorUnableToWrite = 0x06,
    }
}
