using sensors_test.Drivers;
using System.Device.Gpio.Drivers;
using System.Device.Gpio;
using System.Numerics;

namespace sensors_test.Controllers.MotorController
{

    internal class PDSGBGearboxMotorController
    {
        private readonly IMotorDriver turningMotor;
        private readonly IMotorDriver driveMotor;

        public PDSGBGearboxMotorController(IMotorDriver TurningMotor, IMotorDriver DriveMotor)
        {
            turningMotor = TurningMotor;
            driveMotor = DriveMotor;
        }

        public void Test()
        {
            Console.WriteLine($"Drive: {driveMotor.Backwards} speed: {25}");
            driveMotor.Start(driveMotor.Backwards, 25);
            Thread.Sleep(1000);
            driveMotor.Stop();
        }
        public void Move() { }
        public void Turn() { }

    }
}
