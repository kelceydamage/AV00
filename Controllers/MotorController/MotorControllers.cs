using System.Device.Gpio;
using System.Text.Json.Serialization;
using AV00.Shared;
using AV00.Drivers.Motors;
using AV00.Drivers.IO;

namespace AV00.Controllers.MotorController
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

    public enum EnumMotorCommands
    {
        Move,
        Turn,
        Stop,
        Null,
    }

    [Serializable]
    public readonly struct MotorCommandData
    {
        public PinValue Direction { get => direction; }
        private readonly PinValue direction;
        public ushort PwmAmount { get => pwmAmount; }
        private readonly ushort pwmAmount;
        public EnumMotorCommands Command { get => command; }
        private readonly EnumMotorCommands command;
        public EnumExecutionMode Mode { get => mode; }
        private readonly EnumExecutionMode mode;

        [JsonConstructor]
        public MotorCommandData(EnumMotorCommands Command, PinValue Direction, ushort PwmAmount, EnumExecutionMode Mode = EnumExecutionMode.Blocking)
        {
            command = Command;
            direction = Direction;
            pwmAmount = PwmAmount;
            mode = Mode;
        }
    }

    internal class PDSGBGearboxMotorController: IMotorController
    {
        private readonly IMotor turningMotor;
        private readonly IMotor driveMotor;
        private readonly PWM servoBoardController;
        private readonly GPIO gpio;
        private readonly Dictionary<EnumMotorCommands, IMotor> MotorRegistry = new();

        public PDSGBGearboxMotorController(GPIO Gpio, PWM ServoBoardController, IMotor TurningMotor, IMotor DriveMotor)
        {
            gpio = Gpio;
            turningMotor = TurningMotor;
            driveMotor = DriveMotor;
            servoBoardController = ServoBoardController;
            MotorRegistry.Add(EnumMotorCommands.Move, driveMotor);
            MotorRegistry.Add(EnumMotorCommands.Turn, turningMotor);
        }

        public void Test()
        {
            using (StreamWriter outputFile = new(Path.Combine(Environment.CurrentDirectory, "pwm-log.txt"), true))
            {
                outputFile.WriteLine("New Test -----------------");
            }
            Console.WriteLine("New Test -----------------");

            Console.WriteLine($"{turningMotor.Name} Current PWM: {turningMotor.CurrentPwmAmount}");
            Console.WriteLine($"{driveMotor.Name} Current PWM: {driveMotor.CurrentPwmAmount}");
            Console.WriteLine($"{turningMotor.Name} - Test Complete -----------------");
            Console.WriteLine($"{driveMotor.Name} - Test Complete -----------------");
        }

        private static void WriteLogFile(IMotor RequestedMotor, MotorCommandData MotorRequest)
        {
            using (StreamWriter outputFile = new(Path.Combine(Environment.CurrentDirectory, "pwm-log.txt"), true))
            {
                outputFile.WriteLine($"-- Run Motor: {RequestedMotor.Name}-{RequestedMotor.PwmChannelId}, Direction: {MotorRequest.Direction}, Speed: {MotorRequest.PwmAmount}");
            }
            Console.WriteLine($"-- Run Motor: {RequestedMotor.Name}-{RequestedMotor.PwmChannelId}, Direction: {MotorRequest.Direction}, Speed: {MotorRequest.PwmAmount}");
        }

        // Compatability API
        public void Move(MotorCommandData MotorRequest)
        {
            RunMotor(MotorRequest);
        }

        // Compatability API
        public void Turn(MotorCommandData MotorRequest)
        {
            RunMotor(MotorRequest);
        }

        // Compatability API
        public void Stop(MotorCommandData MotorRequest)
        {
            HardStop(turningMotor, MotorRequest.Mode);
            HardStop(driveMotor, MotorRequest.Mode);
        }

        // Execution control goals:
        // If blocking, do not allow motor to be used until lock has expired
        // If non-blocking, queue or blend the new command with the current command
        // If override, stop the current command and run the new command
        private void RunMotor(MotorCommandData MotorRequest)
        {
            IMotor RequestedMotor = MotorRegistry[MotorRequest.Command];
            WriteLogFile(RequestedMotor, MotorRequest);
            if (MotorRequest.Direction != RequestedMotor.CurrentDirection)
            {
                GradualStop(RequestedMotor);
            }
            RequestedMotor.CurrentDirection = MotorRequest.Direction;
            if (MotorRequest.PwmAmount == RequestedMotor.CurrentPwmAmount) { return; }
            else if (MotorRequest.PwmAmount > RequestedMotor.CurrentPwmAmount)
            {
                Accelerate(RequestedMotor, MotorRequest.PwmAmount);
            }
            else if (MotorRequest.PwmAmount < RequestedMotor.CurrentPwmAmount)
            {
                Decelerate(RequestedMotor, MotorRequest.PwmAmount);
            }
        }

        private void SetDutyAndDirection(IMotor Motor)
        {
            Console.WriteLine($"---- {Motor.Name}: {Motor.PwmChannelId}: {Motor.CurrentPwmAmount}");
            if (Motor.CurrentPwmAmount != 0) Motor.IsActive = true;
            else Motor.IsActive = false;
            gpio.SafeWritePin(Motor.DirectionPin, Motor.CurrentDirection);
            servoBoardController.SetChannelPWM(Motor.PwmChannelId, Motor.CurrentPwmAmount);
            if (Motor.CurrentPwmAmount == 0) Motor.IsActive = false;
        }

        private void GradualStop(IMotor Motor)
        {
            Decelerate(Motor, 0);
        }

        private void Decelerate(IMotor Motor, ushort TargetPwm)
        {
            if (TargetPwm >= Motor.CurrentPwmAmount) { return; }
            ushort stopAmount = (ushort)Math.Floor(Motor.PwmSoftCaps.StopPwm * servoBoardController.PwmMaxValue);
            if (TargetPwm < stopAmount) { TargetPwm = stopAmount; }
            ushort pwmChangeAmount = (ushort)Math.Floor(servoBoardController.PwmMaxValue * Motor.DutyCycleChangeStepPct);
            Console.WriteLine($"-- Decelerate: from={Motor.CurrentPwmAmount}, to={TargetPwm}, steps={pwmChangeAmount}");
            while (Motor.CurrentPwmAmount > TargetPwm)
            {
                Motor.CurrentPwmAmount -= pwmChangeAmount;
                if (Motor.CurrentPwmAmount < TargetPwm) { Motor.CurrentPwmAmount = TargetPwm; }
                if (Motor.CurrentPwmAmount == stopAmount) { Motor.CurrentPwmAmount = 0; };
                SetDutyAndDirection(Motor);
                Thread.Sleep(Motor.DutyCycleChangeIntervalMs);
            }
        }

        private void Accelerate(IMotor Motor, ushort TargetPwm)
        {
            ushort stopAmount = (ushort)Math.Floor(Motor.PwmSoftCaps.StopPwm * servoBoardController.PwmMaxValue);
            if (Motor.CurrentPwmAmount < stopAmount) { Motor.CurrentPwmAmount = stopAmount; }
            if (TargetPwm <= Motor.CurrentPwmAmount) { return; }
            ushort runAmount = (ushort)Math.Floor(Motor.PwmSoftCaps.RunPwm * servoBoardController.PwmMaxValue);
            if (TargetPwm > runAmount) { TargetPwm = runAmount; }
            ushort pwmChangeAmount = (ushort)Math.Floor(servoBoardController.PwmMaxValue * Motor.DutyCycleChangeStepPct);
            Console.WriteLine($"-- Accelerate: from={Motor.CurrentPwmAmount}, to={TargetPwm}, steps={pwmChangeAmount}");
            while (Motor.CurrentPwmAmount < TargetPwm)
            {
                Motor.CurrentPwmAmount += pwmChangeAmount;
                if (Motor.CurrentPwmAmount > TargetPwm) { Motor.CurrentPwmAmount = TargetPwm; }
                SetDutyAndDirection(Motor);
                Thread.Sleep(Motor.DutyCycleChangeIntervalMs);
            }
        }

        private void HardStop(IMotor Motor, EnumExecutionMode Mode)
        {
            Motor.CurrentPwmAmount = 0;
            SetDutyAndDirection(Motor);
        }
    }
}