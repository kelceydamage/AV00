using System.Device.Gpio;
using System.Text.Json.Serialization;
using AV00.Shared;
using AV00.Drivers.Motors;
using AV00.Drivers.IO;
using System.Runtime.InteropServices;

namespace AV00.Controllers.MotorController
{
    public struct QueueableMotor
    {
        public readonly IMotor Motor { get => motor; }
        private readonly IMotor motor;
        public readonly Queue<MotorCommandData> MotorCommandQueue { get => motorCommandQueue; }
        private readonly Queue<MotorCommandData> motorCommandQueue;
        public bool IsReserved { get; set; } = false;
        public Guid ReservationId { get; set; }
        public bool IsActive { get; set; } = false;

        public QueueableMotor(IMotor Motor)
        {
            motor = Motor;
            motorCommandQueue = new Queue<MotorCommandData>();
        }
    }

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

    public class CancellationToken
    {
        public bool IsCancellationRequested
        { 
            get => isCancellationRequested;
            set => isCancellationRequested = value;
        }
        private bool isCancellationRequested = false;
    }

    [Serializable]
    public readonly struct MotorCommandData
    {
        public Guid CommandId { get => commandId; }
        private readonly Guid commandId;
        public PinValue Direction { get => direction; }
        private readonly PinValue direction;
        public ushort PwmAmount { get => pwmAmount; }
        private readonly ushort pwmAmount;
        public EnumMotorCommands Command { get => command; }
        private readonly EnumMotorCommands command;
        public EnumExecutionMode Mode { get => mode; }
        private readonly EnumExecutionMode mode;
        public CancellationToken CancellationToken { get => cancellationToken; }
        private readonly CancellationToken cancellationToken;

        [JsonConstructor]
        public MotorCommandData(EnumMotorCommands Command, PinValue Direction, ushort PwmAmount, Guid CommandId, EnumExecutionMode Mode = EnumExecutionMode.Blocking)
        {
            command = Command;
            direction = Direction;
            pwmAmount = PwmAmount;
            mode = Mode;
            commandId = CommandId;
            cancellationToken = new CancellationToken();
        }
    }

    internal class PDSGBGearboxMotorController: IMotorController
    {
        private readonly QueueableMotor turningMotor;
        private readonly QueueableMotor driveMotor;
        private readonly PWM servoBoardController;
        private readonly GPIO gpio;
        private readonly Dictionary<EnumMotorCommands, QueueableMotor> MotorRegistry = new();

        public PDSGBGearboxMotorController(GPIO Gpio, PWM ServoBoardController, IMotor TurningMotor, IMotor DriveMotor)
        {
            gpio = Gpio;
            turningMotor = new QueueableMotor(TurningMotor);
            driveMotor = new QueueableMotor(DriveMotor);
            servoBoardController = ServoBoardController;
            MotorRegistry.Add(EnumMotorCommands.Move, driveMotor);
            MotorRegistry.Add(EnumMotorCommands.Turn, turningMotor);
        }

        // TODO: Stop is not implemented, might remove entirely as a command.
        public ref QueueableMotor GetMotorByCommand(EnumMotorCommands MotorCommand)
        {
            return ref CollectionsMarshal.GetValueRefOrNullRef(MotorRegistry, MotorCommand);
        }

        public void Test()
        {
            using (StreamWriter outputFile = new(Path.Combine(Environment.CurrentDirectory, "pwm-log.txt"), true))
            {
                outputFile.WriteLine("New Test -----------------");
            }
            Console.WriteLine("New Test -----------------");

            Console.WriteLine($"{turningMotor.Motor.Name} Current PWM: {turningMotor.Motor.CurrentPwmAmount}");
            Console.WriteLine($"{driveMotor.Motor.Name} Current PWM: {driveMotor.Motor.CurrentPwmAmount}");
            Console.WriteLine($"{turningMotor.Motor.Name} - Test Complete -----------------");
            Console.WriteLine($"{driveMotor.Motor.Name} - Test Complete -----------------");
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
            Run(MotorRequest);
        }

        // Compatability API
        public void Turn(MotorCommandData MotorRequest)
        {
            Run(MotorRequest);
        }

        // Compatability API
        public void Stop(MotorCommandData MotorRequest) 
        {
            HardStop(MotorRequest);
        }

        public void Run(MotorCommandData MotorRequest)
        {
            IMotor RequestedMotor = MotorRegistry[MotorRequest.Command].Motor;
            WriteLogFile(RequestedMotor, MotorRequest);
            if (MotorRequest.Direction != RequestedMotor.CurrentDirection)
            {
                GradualStop(RequestedMotor, MotorRequest);
            }
            RequestedMotor.CurrentDirection = MotorRequest.Direction;
            if (MotorRequest.PwmAmount == RequestedMotor.CurrentPwmAmount) { return; }
            else if (MotorRequest.PwmAmount > RequestedMotor.CurrentPwmAmount)
            {
                Accelerate(RequestedMotor, MotorRequest);
            }
            else if (MotorRequest.PwmAmount < RequestedMotor.CurrentPwmAmount)
            {
                Decelerate(RequestedMotor, MotorRequest);
            }
        }

        private void SetDutyAndDirection(IMotor Motor)
        {
            Console.WriteLine($"---- {Motor.Name}: {Motor.PwmChannelId}: {Motor.CurrentPwmAmount}");
            //if (Motor.CurrentPwmAmount != 0) Motor.IsActive = true;
            //else Motor.IsActive = false;
            gpio.SafeWritePin(Motor.DirectionPin, Motor.CurrentDirection);
            servoBoardController.SetChannelPWM(Motor.PwmChannelId, Motor.CurrentPwmAmount);
            //if (Motor.CurrentPwmAmount == 0) Motor.IsActive = false;
        }

        private void GradualStop(IMotor Motor, MotorCommandData MotorRequest)
        {
            Decelerate(Motor, MotorRequest);
        }

        // A cancelled Decelerate will leave the motor running at the last set speed. This is allowed in order to facilite smoother transitions
        // between motor commands, and a quicker response time
        private void Decelerate(IMotor Motor, MotorCommandData MotorRequest)
        {
            ushort targetPwm = MotorRequest.PwmAmount;
            if (targetPwm >= Motor.CurrentPwmAmount) { return; }
            ushort stopAmount = (ushort)Math.Floor(Motor.PwmSoftCaps.StopPwm * servoBoardController.PwmMaxValue);
            if (targetPwm < stopAmount) { targetPwm = stopAmount; }
            ushort pwmChangeAmount = (ushort)Math.Floor(servoBoardController.PwmMaxValue * Motor.DutyCycleChangeStepPct);
            Console.WriteLine($"-- Decelerate: from={Motor.CurrentPwmAmount}, to={targetPwm}, steps={pwmChangeAmount}");
            while (Motor.CurrentPwmAmount > targetPwm)
            {
                if (MotorRequest.CancellationToken.IsCancellationRequested) { return; }
                Motor.CurrentPwmAmount -= pwmChangeAmount;
                if (Motor.CurrentPwmAmount < targetPwm) { Motor.CurrentPwmAmount = targetPwm; }
                if (Motor.CurrentPwmAmount == stopAmount) { Motor.CurrentPwmAmount = 0; };
                SetDutyAndDirection(Motor);
                Thread.Sleep(Motor.DutyCycleChangeIntervalMs);
            }
        }

        // A cancelled Accelerate will leave the motor running at the last set speed. This is allowed in order to facilite smoother transitions
        // between motor commands, and a quicker response time
        private void Accelerate(IMotor Motor, MotorCommandData MotorRequest)
        {
            ushort targetPwm = MotorRequest.PwmAmount;
            ushort stopAmount = (ushort)Math.Floor(Motor.PwmSoftCaps.StopPwm * servoBoardController.PwmMaxValue);
            if (Motor.CurrentPwmAmount < stopAmount) { Motor.CurrentPwmAmount = stopAmount; }
            if (targetPwm <= Motor.CurrentPwmAmount) { return; }
            ushort runAmount = (ushort)Math.Floor(Motor.PwmSoftCaps.RunPwm * servoBoardController.PwmMaxValue);
            if (targetPwm > runAmount) { targetPwm = runAmount; }
            ushort pwmChangeAmount = (ushort)Math.Floor(servoBoardController.PwmMaxValue * Motor.DutyCycleChangeStepPct);
            Console.WriteLine($"-- Accelerate: from={Motor.CurrentPwmAmount}, to={targetPwm}, steps={pwmChangeAmount}");
            while (Motor.CurrentPwmAmount < targetPwm)
            {
                if (MotorRequest.CancellationToken.IsCancellationRequested) { return; }
                Motor.CurrentPwmAmount += pwmChangeAmount;
                if (Motor.CurrentPwmAmount > targetPwm) { Motor.CurrentPwmAmount = targetPwm; }
                SetDutyAndDirection(Motor);
                Thread.Sleep(Motor.DutyCycleChangeIntervalMs);
            }
        }

        private void HardStop(MotorCommandData MotorRequest)
        {
            MotorRegistry[MotorRequest.Command].Motor.CurrentPwmAmount = 0;
            SetDutyAndDirection(MotorRegistry[MotorRequest.Command].Motor);
        }
    }
}