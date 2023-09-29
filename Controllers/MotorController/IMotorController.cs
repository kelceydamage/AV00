namespace AV00.Controllers.MotorController
{
    public interface IMotorController
    {
        public void Test();
        public void Move(MotorCommandData MotorRequest);
        public void Turn(MotorCommandData MotorRequest);
        public void Stop(MotorCommandData MotorRequest);
    }
}
