using System.Device.Gpio;

namespace sensors_test.Controllers.MotorController
{
    public interface IMotorController
    {
        public void Test();
        public void Move(PinValue Direction, ushort PwmAmount);
        public void Turn(PinValue Direction, ushort PwmAmount);
        public void Stop();
    }
}
