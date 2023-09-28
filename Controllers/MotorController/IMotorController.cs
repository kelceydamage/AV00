using System.Device.Gpio;

namespace AV00.Controllers.MotorController
{
    public interface IMotorController
    {
        public void Test();
        public void Move(PinValue Direction, ushort PwmAmount);
        public void Turn(PinValue Direction, ushort PwmAmount);
        public void Stop();
    }
}
