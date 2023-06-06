using sensors_test.Drivers.IO;
using System.Device.Gpio;

namespace sensors_test.Drivers.Motors
{
    public class MDD10A : IMotorDriver
    {
        public byte MotorDirectionPin { get; }
        public PinValue Left { get; } = PinValue.Low;
        public PinValue Right { get; } = PinValue.High;
        public PinValue Forwards { get; } = PinValue.Low;
        public PinValue Backwards { get; } = PinValue.High;
        public byte DutyDownCycleStep { get; } = 1;
        public short DutyDownCycleIntervalMs { get; } = 300;
        public byte PwmChannel { get; }
        private PinValue currentDirection = 0;
        private byte currentDutyCycle = 0;
        private readonly HardwareIODriver IO;
        private readonly GpioController Gpio = new();

        public MDD10A(HardwareIODriver IODriver, byte MotorDirectionPinId, HardwareIODriver.PwmChannelRegisters PwmChannelRegister)
        {
            IO = IODriver;
            Gpio.OpenPin(MotorDirectionPinId, PinMode.Output);
            MotorDirectionPin = MotorDirectionPinId;
            PwmChannel = (byte)PwmChannelRegister;
        }

        public void Start(PinValue Direction, byte Power)
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

        public void Dispose()
        {
            Stop();
            Gpio.Dispose();
        }

        private byte SetDutyAndDirection(PinValue Direction, byte Duty)
        {
            Gpio.Write(MotorDirectionPin, Direction);
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
