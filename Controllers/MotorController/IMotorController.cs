using AV00.Controllers.MotorController;
using AV00.Drivers.Motors;

namespace AV00.Controllers.MotorController
{
    public interface IMotorController
    {
        public LockableMotor GetMotorByCommand(EnumMotorCommands MotorCommand);
        public Dictionary<EnumMotorCommands, Queue<MotorCommandData>> MotorCommandQueues { get; }
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