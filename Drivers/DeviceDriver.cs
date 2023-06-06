using System.Device.Gpio;

namespace sensors_test.Drivers
{
    public interface IDeviceDriver
    {
        
    }

    public interface IMotorDriver : IDeviceDriver
    {
        public PinValue Left { get; }
        public PinValue Right { get; }
        public PinValue Forwards { get; }
        public PinValue Backwards { get; }
        public void Start(PinValue Direction, byte Power);
        public void Stop();
        public void Dispose();
    }
}
