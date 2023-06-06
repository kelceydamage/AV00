using sensors_test.Drivers.IO;

namespace sensors_test.Drivers.Motors
{
    public class MDD10A : IMotorDriver
    {
        public byte MotorDirectionPin { get; }
        public byte Left { get; } = 0;
        public byte Right { get; } = 1;
        public byte Forwards { get; } = 0;
        public byte Backwards { get; } = 1;
        public byte DutyDownCycleStep { get; } = 1;
        public short DutyDownCycleIntervalMs { get; } = 300;
        public byte PwmChannel { get; }
        private byte currentDirection = 0;
        private byte currentDutyCycle = 0;
        private readonly HardwareIODriver IO;

        public MDD10A(HardwareIODriver IODriver, byte MotorDirectionPinId, HardwareIODriver.PwmChannelRegisters PwmChannelRegister)
        {
            IO = IODriver;
            MotorDirectionPin = MotorDirectionPinId;
            PwmChannel = (byte)PwmChannelRegister;
        }

        public void Start(byte Direction, byte Power)
        {
            if (Direction != currentDirection)
            {
                SafeDirectionSwitch();
            }
            SetDutyAndDirection(Direction, Power);
            currentDirection = Direction;
        }

        public void Stop()
        {
            SafeDirectionSwitch();
        }

        private byte SetDutyAndDirection(byte Direction, byte Duty)
        {
            // TODO: Set GPIO pin here to current direction
            return IO.SetPwmDutyCycle(PwmChannel, Duty);
        }

        private void SafeDirectionSwitch()
        {
            while (currentDutyCycle > 0)
            {
                currentDutyCycle -= DutyDownCycleStep;
                if (currentDutyCycle < 0)
                {
                    currentDutyCycle = 0;
                }
                SetDutyAndDirection(currentDirection, currentDutyCycle);
                Thread.Sleep(DutyDownCycleIntervalMs);
            }
        }
    }
}
