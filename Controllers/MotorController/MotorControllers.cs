using sensors_test.Drivers;
using System.Device.Gpio;
using sensors_test.Drivers.IO;

namespace sensors_test.Controllers.MotorController
{
    public class MotorDirection
    {
        private static readonly PinValue forwards = PinValue.High;
        private static readonly PinValue backwards = PinValue.Low;
        private static readonly PinValue left = PinValue.High;
        private static readonly PinValue right = PinValue.Low;

        public static PinValue Forwards { get => forwards; }
        public static PinValue Backwards { get => backwards; }
        public static PinValue Left { get => left; }
        public static PinValue Right { get => right; }
    }

    internal class PDSGBGearboxMotorController
    {
        private readonly IMotorDriver turningMotor;
        private readonly IMotorDriver driveMotor;
        private readonly PWM servoBoardController;
        private readonly GPIO gpio;

        public PDSGBGearboxMotorController(GPIO Gpio, PWM ServoBoardController, IMotorDriver TurningMotor, IMotorDriver DriveMotor)
        {
            gpio = Gpio;
            turningMotor = TurningMotor;
            driveMotor = DriveMotor;
            servoBoardController = ServoBoardController;
        }

        public void Test()
        {
            Console.WriteLine($"Drive: {MotorDirection.Forwards} speed: {50}");
            Move(MotorDirection.Forwards);
            Console.WriteLine($"Press key to end test");
            Console.ReadKey();
            Stop();
        }
        public void Move(PinValue Direction, int BlockingRunTime = 0)
        {
            RunMotor(driveMotor, Direction, 2048, BlockingRunTime);
        }

        public void Turn(PinValue Direction, int BlockingRunTime = 0)
        {
            RunMotor(turningMotor, Direction, 2048, BlockingRunTime);
        }

        public void Stop()
        {
            HardStop(turningMotor);
            HardStop(driveMotor);
        }

        private void RunMotor(IMotorDriver Motor, PinValue Direction, ushort PwmAmount, int BlockingRunTime)
        {
            if (Direction != Motor.CurrentDirection)
            {
                GradualStop(Motor);
            }
            Motor.CurrentDirection = Direction;
            Motor.CurrentPwmAmount = PwmAmount;
            SetDutyAndDirection(Motor);
            if (BlockingRunTime > 0)
            {
                Thread.Sleep(BlockingRunTime);
                GradualStop(Motor);
            }
        }

        private void SetDutyAndDirection(IMotorDriver Motor)
        {
            gpio.SafeWritePin(Motor.DirectionPin, Motor.CurrentDirection);
            servoBoardController.SetChannelPWM(Motor.PwmChannelId, Motor.CurrentPwmAmount);
        }

        private void GradualStop(IMotorDriver Motor)
        {
            while (Motor.CurrentPwmAmount > 0)
            {
                Motor.CurrentPwmAmount -= Motor.DutyDownCycleStep;
                if (Motor.CurrentPwmAmount < 0)
                {
                    Motor.CurrentPwmAmount = 0;
                }
                Console.WriteLine($"-- Set Duty Cycle: {Motor.CurrentPwmAmount}");
                SetDutyAndDirection(Motor);
                Thread.Sleep(Motor.DutyDownCycleIntervalMs);
            }
        }

        private void HardStop(IMotorDriver Motor)
        {
            Motor.CurrentPwmAmount = 0;
            SetDutyAndDirection(Motor);
        }
    }
}