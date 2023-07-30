using System.Device.Gpio;

namespace sensors_test.Drivers.Motors
{
    public class MDD10A : IMotor
    {
        public short DirectionPin { get; }
        public int PwmChannelId { get; }
        public byte DutyDownCycleStep { get; } = 1;
        public short DutyDownCycleIntervalMs { get; } = 300;
        public string Name { get; set; }
        public PinValue CurrentDirection { get => currentDirection; set => currentDirection = value; }
        private PinValue currentDirection = PinValue.Low;
        public ushort CurrentPwmAmount { get => currentPwmAmount; set => currentPwmAmount = value; }
        private ushort currentPwmAmount = 0;
        public MDD10A(short MotorDirectionPinId, int ChannelId, string CommonName)
        {
            PwmChannelId = ChannelId;
            DirectionPin = MotorDirectionPinId;
            Name = CommonName;
        }
    }
}
