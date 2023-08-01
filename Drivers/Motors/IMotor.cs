using System.Device.Gpio;

namespace sensors_test.Drivers.Motors
{
    public interface IMotor : IDevice
    {
        public short DirectionPin { get; }
        public int PwmChannelId { get; }
        public PinValue CurrentDirection { get; set; }
        public ushort CurrentPwmAmount { get; set; }
        public float DutyCycleChangeStepPct { get; }
        public short DutyCycleChangeIntervalMs { get; }
        public string Type { get; }
        public float Voltage { get; }
        public int Rpm { get; }
        public PwmCaps PwmSoftCaps { get; }
        public PwmCaps PwmHardCaps { get; }
    }
}
