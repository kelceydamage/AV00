using System.Device.Gpio;

namespace AV00.Drivers.Motors
{
    public readonly struct PwmCaps
    {
        public readonly float StopPwm;
        public readonly float StartPwm;
        public readonly float RunPwm;

        public PwmCaps(float StopPwm, float StartPwm, float RunPwm)
        {
            this.StopPwm = StopPwm;
            this.StartPwm = StartPwm;
            this.RunPwm = RunPwm;
        }
    }

    public class MDD10A55072 : IMotor
    {
        public short DirectionPin { get; }
        public int PwmChannelId { get; }
        public float DutyCycleChangeStepPct { get; } = 0.01f;
        public short DutyCycleChangeIntervalMs { get; } = 40;
        public string Name { get; set; }
        public PinValue CurrentDirection { get => currentDirection; set => currentDirection = value; }
        private PinValue currentDirection = PinValue.Low;
        public float CurrentPwmAmount { get => currentPwmAmount; set => currentPwmAmount = value; }
        private float currentPwmAmount = 0f;
        public string Type { get; } = "550";
        public float Voltage { get; } = 7.2f;
        public int Rpm { get; } = 18200;
        public PwmCaps PwmSoftCaps { get; } = new PwmCaps(0.07f, 0.25f, 0.5f);
        public PwmCaps PwmHardCaps { get; } = new PwmCaps(0.05f, 0.35f, 1.0f);
        public MDD10A55072(short MotorDirectionPinId, int ChannelId, string CommonName)
        {
            PwmChannelId = ChannelId;
            DirectionPin = MotorDirectionPinId;
            Name = CommonName;
        }
    }

    public class MDD10A39012 : IMotor
    {
        public short DirectionPin { get; }
        public int PwmChannelId { get; }
        public float DutyCycleChangeStepPct { get; } = 0.01f;
        public short DutyCycleChangeIntervalMs { get; } = 40;
        public string Name { get; set; }
        public PinValue CurrentDirection { get => currentDirection; set => currentDirection = value; }
        private PinValue currentDirection = PinValue.Low;
        public float CurrentPwmAmount { get => currentPwmAmount; set => currentPwmAmount = value; }
        private float currentPwmAmount = 0f;
        public string Type { get; } = "390";
        public float Voltage { get; } = 12.0f;
        public int Rpm { get; } = 6300;
        public PwmCaps PwmSoftCaps { get; } = new PwmCaps(0.12f, 0.50f, 0.95f);
        public PwmCaps PwmHardCaps { get; } = new PwmCaps(0.05f, 0.63f, 1.0f);
        public MDD10A39012(short MotorDirectionPinId, int ChannelId, string CommonName)
        {
            PwmChannelId = ChannelId;
            DirectionPin = MotorDirectionPinId;
            Name = CommonName;
        }
    }
}
