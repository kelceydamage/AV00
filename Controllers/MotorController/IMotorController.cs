using AV00.Drivers.Motors;

namespace AV00.Controllers.MotorController
{
    public interface IMotorController
    {
        public IMotor GetMotorByCommand(EnumMotorCommands MotorCommand);
        public void Test();
        // Compatability API
        public void Move(MotorCommandData MotorRequest);
        // Compatability API
        public void Turn(MotorCommandData MotorRequest);
        // Compatability API
        public void Stop(MotorCommandData MotorRequest);
        public void Run(MotorCommandData MotorRequest);
    }
}
