using sensors_test.Drivers.ExpansionBoards;

namespace sensors_test.Drivers
{
    public interface IMotorDriver
    {
        public byte MotorDriverAddress { get; }
        public byte MotorDirectionPin { get; }
        public uint MotorPwmFrequency { get; }
        public byte Left { get; }
        public byte Right { get; }
        public byte Forwards { get; }
        public byte Backwards { get; }
        public byte DutyDownCycleStep { get; }
        public short DutyDownCycleIntervalMs { get; }
        public byte PwmChannel { get; }
        public void RunMotor() { }
        public void StopMotor() { }
    }

    public class PWMDeviceDriver
    {
        private readonly IExpansionBoard PWMController;
        private byte currentDirection = 0;

        public PWMDeviceDriver(IExpansionBoard PWMControllerBoard)
        {
            PWMController = PWMControllerBoard;
        }

        /* 
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

        private void SetPins(byte Direction, byte Duty, IExpansionBoard IOBoard)
        {
            // TODO: Set GPIO pin here to current direction
            IOBoard.SetPwmDutyCycle(PwmChannel, Duty);
        }

        private void SafeDirectionSwitch()
        {
            byte currentDutyCycle = IOBoard.CurrentDutyCycle;
            while (IOBoard.CurrentDutyCycle > 0)
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
        */
    }

    public class PWMDeviceRegistry
    {
        private readonly Dictionary<string, IMotorDriver> registry = new();

        public void Register<T>(T IMotorDriver) where T : IMotorDriver
        {
            string key = typeof(T).Name;
            if (registry.ContainsKey(key))
            {
                throw new Exception($"I2C Device {key} already found in registry");
            }
            registry.Add(key, IMotorDriver);
        }

        public void Unregister<T>() where T : IMotorDriver
        {
            string key = typeof(T).Name;
            if (!registry.ContainsKey(key))
            {
                throw new Exception($"I2C Device {key} not found in registry");
            }
            registry.Remove(key);
        }

        public T Get<T>() where T : IMotorDriver
        {
            string key = typeof(T).Name;
            if (!registry.ContainsKey(key))
            {
                throw new Exception($"I2C Device {key} not found in registry");
            }
            return (T)registry[key];
        }
    }
}
