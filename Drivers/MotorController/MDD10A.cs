using sensors_test.Drivers.ExpansionBoards;
using sensors_test.Drivers.MotorControllers;
using System.Device.I2c;
using static sensors_test.Drivers.ExpansionBoards.DFRIOExpansion;

namespace sensors_test.Drivers.MotorController
{
    public class MDD10A: IMotorDriver
    {
        public DFRIOExpansion Controller { get; }
        public byte MotorDriverAddress { get; } = 0x10;
        public byte MotorDirectionPin { get; }
        public uint MotorPwmFrequency { get; } = 18000;
        public byte Left { get; } = 0;
        public byte Right { get; } = 1;
        public byte Forwards { get; } = 0;
        public byte Backwards { get; } = 1;
        public byte DutyDownCycleStep { get; } = 1;
        public short DutyDownCycleIntervalMs { get; } = 300;
        private readonly byte pwmChannel;
        private readonly byte motorDirectionPin;
        private byte currentDirection = 0;

        public MDD10A(I2cConnectionSettings Settings, byte MotorDirectionPinId, PwmChannelRegisters PwmChannel)
        {
            Controller = new DFRIOExpansion(Settings);
            MotorDirectionPin = MotorDirectionPinId;
            pwmChannel = (byte)PwmChannel;
        }

        public void RunMotor(byte Direction, byte Power)
        {
            if (Direction != currentDirection)
            {
                SafeDirectionSwitch();
            }
            SetPins(Direction, Power);
            currentDirection = Direction;
        }

        public void StopMotor()
        {
            SafeDirectionSwitch();
        }

        private void SetPins(byte Direction, byte Duty)
        {
            // TODO: Set GPIO pin here to current direction
            Controller.SetPwmDutyCycle(pwmChannel, Duty);
        }

        private void SafeDirectionSwitch()
        {
            byte currentDutyCycle = Controller.CurrentDutyCycle;
            while (Controller.CurrentDutyCycle > 0)
            {
                currentDutyCycle -= DutyDownCycleStep;
                if (currentDutyCycle < 0)
                {
                    currentDutyCycle = 0;
                }
                SetPins(currentDirection, currentDutyCycle);
                Thread.Sleep(DutyDownCycleIntervalMs);
            }
        }
    }
}
