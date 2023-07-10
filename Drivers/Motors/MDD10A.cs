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
        private PinValue currentDirection = 0;
        private byte currentDutyCycle = 0;
        private readonly HardwareIODriver IO;
        private readonly GpioController Gpio;

        public MDD10A(HardwareIODriver IODriver, short MotorDirectionPinId, HardwareIODriver.PwmChannelRegisters PwmChannelRegister, string name)
        {
            Console.WriteLine($"Motor Init Start");
            IO = IODriver;
            Console.WriteLine($"-- Add GPIO");
            try
            {
                //Gpio = new GpioController(PinNumberingScheme.Logical, new SysFsDriver());
                Gpio = new GpioController(PinNumberingScheme.Logical, new LibGpiodDriver(1));
                Console.WriteLine($"-- Created GpioController");
            }
            catch (Exception e)
            {
                Console.WriteLine($"-- Add GPIO Error: {e.Message}");
                throw new Exception($"Failed to initialize GPIO Controller: {e.Message}");
            }
            Console.WriteLine($"-- Pin Count: {Gpio.PinCount}");
            Console.WriteLine($"-- Open Pin");
            Gpio.OpenPin(MotorDirectionPinId);
            Console.WriteLine($"-- Pin Mode: {Gpio.GetPinMode(MotorDirectionPinId)}");
            Console.WriteLine($"-- Set Direction Pin Value");
            MotorDirectionPin = MotorDirectionPinId;
            Console.WriteLine($"-- Set PWM Channel");
            PwmChannel = (byte)PwmChannelRegister;
            Console.WriteLine($"Motor Initialized");
            Name = name;
        }

        public void Start(PinValue Direction, byte Power)
        {
            Console.WriteLine($"Check Direction");
            if (Direction != currentDirection)
            {
                SafeDirectionSwitch();
            }
            Console.WriteLine($"Power Motor");
            SetDutyAndDirection(Direction, Power);
            Console.WriteLine($"Set PWM Duty Cycle Board Status: {IO.LastOperationStatus}");
            currentDirection = Direction;
        }

        public void Stop()
        {
            SafeDirectionSwitch();
        }

        public void Dispose()
        {
            Stop();
            Gpio.ClosePin(MotorDirectionPin);
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
                Console.WriteLine($"Set PWM Duty Cycle Board Status: {IO.LastOperationStatus}");
                Thread.Sleep(DutyDownCycleIntervalMs);
            }
        }
    }
}
