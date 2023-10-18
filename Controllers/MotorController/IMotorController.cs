namespace AV00.Controllers.MotorController
{
    public interface IMotorController
    {
        public LockableMotor GetMotorByCommand(EnumMotorCommands MotorCommand);
        public Dictionary<EnumMotorCommands, Queue<MotorCommandEventModel>> MotorCommandQueues { get; }
        public void Test();
        // Compatability API
        public void Move(MotorCommandEventModel MotorRequest, CancellationToken Token);
        // Compatability API
        public void Turn(MotorCommandEventModel MotorRequest, CancellationToken Token);
        // Compatability API
        public void Stop(MotorCommandEventModel MotorRequest, CancellationToken Token);
        public void Run(MotorCommandEventModel MotorRequest, CancellationToken Token);
    }
}