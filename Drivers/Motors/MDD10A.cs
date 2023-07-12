using sensors_test.Drivers.IO;
using System.Device.Gpio;
using System.Device.Gpio.Drivers;

namespace sensors_test.Drivers.Motors
{
    public class MDD10A : IMotorDriver
    {
        public short MotorDirectionPin { get; }
        public PinValue Left { get; } = PinValue.Low;
        public PinValue Right { get; } = PinValue.High;
        public PinValue Forwards { get; } = PinValue.Low;
        public PinValue Backwards { get; } = PinValue.High;
        public byte DutyDownCycleStep { get; } = 1;
        public short DutyDownCycleIntervalMs { get; } = 300;
        public byte PwmChannel { get; }
        public string Name { get; set; }
        private PinValue currentDirection = PinValue.Low;
        private byte currentDutyCycle = 0;
        private readonly HardwareIODriver IO;

        public MDD10A(HardwareIODriver IODriver, short MotorDirectionPinId, HardwareIODriver.PwmChannelRegisters PwmChannelRegister, string name)
        {
            Console.WriteLine($"Motor Init Start");
            IO = IODriver;
            Console.WriteLine($"-- Pin Count: {IO.GetGpioPinCount()}");
            MotorDirectionPin = MotorDirectionPinId;
            Console.WriteLine($"-- Set PWM Channel");
            PwmChannel = (byte)PwmChannelRegister;
            Console.WriteLine($"Motor Initialized");
            Name = name;
            IO.SafeWritePin(MotorDirectionPin, PinValue.Low);
            Thread.Sleep(1000);
        }

        public void Start(PinValue Direction, byte Power)
        {
            Console.WriteLine("-------------------------");
            Console.WriteLine($"Check Direction");
            if (Direction != currentDirection)
            {
                SafeDirectionSwitch();
            }
            Console.WriteLine($"Power Motor");
            currentDirection = Direction;
            SetDutyAndDirection(Direction, Power);
            Console.WriteLine($"Set PWM Duty Cycle Board Status: {IO.LastOperationStatus}");
            Console.WriteLine("Motor Run Complete --");
        }

        public void Stop()
        {
            Console.WriteLine($"Stop Motor");
            SafeDirectionSwitch();
        }

        public void GradualStop()
        {
            while (currentDutyCycle > 0)
            {
                currentDutyCycle -= DutyDownCycleStep;
                if (currentDutyCycle < 0)
                {
                    currentDutyCycle = 0;
                }
                Console.WriteLine($"-- Set Duty Cycle: {currentDutyCycle}");
                SetDutyAndDirection(currentDirection, currentDutyCycle);
                Console.WriteLine($"Set PWM Duty Cycle Board Status: {IO.LastOperationStatus}");
                Thread.Sleep(DutyDownCycleIntervalMs);
            }
        }

        private byte SetDutyAndDirection(PinValue Direction, byte Duty)
        {
            Console.WriteLine($"Set Duty Cycle: {Duty}, Set Direction: {Direction}, Pin: {MotorDirectionPin}");
            IO.SafeWritePin(MotorDirectionPin, Direction);
            Console.WriteLine($"GPIO Written: {Direction}");
            return IO.SetPwmDutyCycle(PwmChannel, Duty);
        }

        private void SafeDirectionSwitch()
        {
            Console.WriteLine($"Starting Safe Direction Switch, Current Duty Cycle: {currentDutyCycle}");
            GradualStop();
        }
    }
}
