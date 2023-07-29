using System.Device.Gpio;

namespace sensors_test.Drivers
{
    public interface IDeviceDriver
    {
        public string Name { get; set; }
    }

    public interface IMotorDriver : IDeviceDriver
    {
        public short DirectionPin { get; }
        public int PwmChannelId { get; }
        public PinValue CurrentDirection { get; set; }
        public ushort CurrentPwmAmount { get; set; }
        public byte DutyDownCycleStep { get; }
        public short DutyDownCycleIntervalMs { get; }
    }

    public interface IPwmController : IDeviceDriver
    {
        public int PwmMaxFrequencyHz { get; }
        public int PwmMinFrequencyHz { get; }
        public int PwmBitDepth { get; }
        public int PwmChannelCount { get; }
        public void SetFrequency(float Frequency);
        public void Reset();
        public void SetChannelPwm(int ChannelId, ushort PwmAmount);
    }
}
