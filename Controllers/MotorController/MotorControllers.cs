using sensors_test.Drivers;

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
            driveMotor.Start(driveMotor.Forwards, 25);
            Thread.Sleep(1000);
            driveMotor.Stop();
        }
        public void Move() { }
        public void Turn() { }

    }
}
