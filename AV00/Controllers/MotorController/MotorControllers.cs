﻿using System.Device.Gpio;
using AV00.Drivers.Motors;
using AV00.Drivers.IO;

namespace AV00.Controllers.MotorController
{
    public class LockableMotor
    {
        public IMotor Motor { get => motor; }
        private readonly IMotor motor;
        public bool IsReserved { get; set; } = false;
        public Guid ReservationId { get; set; }
        public bool IsActive { get; set; } = false;

        public LockableMotor(IMotor Motor)
        {
            motor = Motor;
        }
    }

    public enum EnumMotorDirection
    {
        Low,
        High,
    }

    public class MotorDirection
    {
        private static readonly EnumMotorDirection forwards = EnumMotorDirection.High;
        private static readonly EnumMotorDirection backwards = EnumMotorDirection.Low;
        private static readonly EnumMotorDirection left = EnumMotorDirection.High;
        private static readonly EnumMotorDirection right = EnumMotorDirection.Low;
        public static EnumMotorDirection Forwards { get => forwards; }
        public static EnumMotorDirection Backwards { get => backwards; }
        public static EnumMotorDirection Left { get => left; }
        public static EnumMotorDirection Right { get => right; }
        private static Dictionary<EnumMotorDirection, PinValue> pinValues = new()
        {
            { EnumMotorDirection.High, PinValue.High },
            { EnumMotorDirection.Low, PinValue.Low }
        };

        public static PinValue PinValueFromDirection(EnumMotorDirection MotorDirection)
        {
            return pinValues[MotorDirection];
        }
    }

    public enum EnumMotorCommands
    {
        Move,
        Turn,
    }

    internal class PDSGBGearboxMotorController: IMotorController
    {
        private readonly LockableMotor turningMotor;
        private readonly LockableMotor driveMotor;
        private readonly PWM servoBoardController;
        private readonly GPIO gpio;
        private readonly Dictionary<EnumMotorCommands, LockableMotor> motorRegistry = new();
        public Dictionary<EnumMotorCommands, Queue<MotorCommandEventModel>> MotorCommandQueues { get => motorCommandQueues; }
        private readonly Dictionary<EnumMotorCommands, Queue<MotorCommandEventModel>> motorCommandQueues = new();

        public PDSGBGearboxMotorController(GPIO Gpio, PWM ServoBoardController, IMotor TurningMotor, IMotor DriveMotor)
        {
            gpio = Gpio;
            turningMotor = new LockableMotor(TurningMotor);
            driveMotor = new LockableMotor(DriveMotor);
            servoBoardController = ServoBoardController;
            motorRegistry.Add(EnumMotorCommands.Move, driveMotor);
            motorRegistry.Add(EnumMotorCommands.Turn, turningMotor);
            InitializeCommandQueues();
            servoBoardController.Init();
        }

        // TODO: Stop is not implemented, might remove entirely as a command.
        public LockableMotor GetMotorByCommand(EnumMotorCommands MotorCommand)
        {
            return motorRegistry[MotorCommand];
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

        private void InitializeCommandQueues()
        {
            foreach (EnumMotorCommands command in Enum.GetValues(typeof(EnumMotorCommands)))
            {
                Console.WriteLine($"InitializeCommandQueues {command}");
                motorCommandQueues.Add(command, new Queue<MotorCommandEventModel>());
            }
        }

        private static void WriteLogFile(IMotor RequestedMotor, MotorCommandEventModel MotorRequest)
        {
            using (StreamWriter outputFile = new(Path.Combine(Environment.CurrentDirectory, "pwm-log.txt"), true))
            {
                outputFile.WriteLine($"*DEBUG* MOTOR-CONTROLLER [RunMotor]: {RequestedMotor.Name} Channel {RequestedMotor.PwmChannelId} Direction {MotorRequest.Direction} Speed: {MotorRequest.PwmAmount}");
            }
            Console.WriteLine($"*DEBUG* MOTOR-CONTROLLER [RunMotor]: {RequestedMotor.Name} Channel {RequestedMotor.PwmChannelId} Direction {MotorRequest.Direction} Speed: {MotorRequest.PwmAmount}");
        }

        // Compatability API
        public void Move(MotorCommandEventModel MotorRequest, CancellationToken Token)
        {
            Run(MotorRequest, Token);
        }

        // Compatability API
        public void Turn(MotorCommandEventModel MotorRequest, CancellationToken Token)
        {
            Run(MotorRequest, Token);
        }

        // Compatability API
        public void Stop(MotorCommandEventModel MotorRequest, CancellationToken Token) 
        {
            HardStop(MotorRequest);
        }

        public void Run(MotorCommandEventModel MotorRequest, CancellationToken Token)
        {
            Token.ThrowIfCancellationRequested();
            IMotor RequestedMotor = motorRegistry[MotorRequest.Command].Motor;
            WriteLogFile(RequestedMotor, MotorRequest);
            if (MotorDirection.PinValueFromDirection(MotorRequest.Direction) != RequestedMotor.CurrentDirection)
            {
                GradualStop(RequestedMotor, MotorRequest, Token);
            }
            RequestedMotor.CurrentDirection = MotorDirection.PinValueFromDirection(MotorRequest.Direction);
            if (MotorRequest.PwmAmount == RequestedMotor.CurrentPwmAmount) { return; }
            else if (MotorRequest.PwmAmount > RequestedMotor.CurrentPwmAmount)
            {
                Accelerate(RequestedMotor, MotorRequest, Token);
            }
            else if (MotorRequest.PwmAmount < RequestedMotor.CurrentPwmAmount)
            {
                Decelerate(RequestedMotor, MotorRequest, Token);
            }
        }

        private void SetDutyAndDirection(IMotor Motor)
        {
            gpio.SafeWritePin(Motor.DirectionPin, Motor.CurrentDirection);
            servoBoardController.SetChannelPWM(Motor.PwmChannelId, Motor.CurrentPwmAmount);
        }

        private void GradualStop(IMotor Motor, MotorCommandEventModel MotorRequest, CancellationToken Token)
        {
            Decelerate(Motor, MotorRequest, Token);
        }

        // A cancelled Decelerate will leave the motor running at the last set speed. This is allowed in order to facilite smoother transitions
        // between motor commands, and a quicker response time
        private void Decelerate(IMotor Motor, MotorCommandEventModel MotorRequest, CancellationToken Token)
        {
            float targetPwm = MotorRequest.PwmAmount;
            if (targetPwm >= Motor.CurrentPwmAmount) { return; }
            float stopAmount = (float)Math.Floor(Motor.PwmSoftCaps.StopPwm * servoBoardController.PwmMaxPercent);
            if (targetPwm < stopAmount) { targetPwm = stopAmount; }
            float pwmChangeAmount = (float)Math.Floor(servoBoardController.PwmMaxPercent * Motor.DutyCycleChangeStepPct);
            Console.WriteLine($"*DEBUG* MOTOR-CONTROLLER [Decelerate] Current speed {Motor.CurrentPwmAmount} Target speed {targetPwm} Stepping {pwmChangeAmount}");
            while (Motor.CurrentPwmAmount > targetPwm)
            {
                Token.ThrowIfCancellationRequested();
                Motor.CurrentPwmAmount -= pwmChangeAmount;
                if (Motor.CurrentPwmAmount < targetPwm) { Motor.CurrentPwmAmount = targetPwm; }
                if (Motor.CurrentPwmAmount == stopAmount) { Motor.CurrentPwmAmount = 0f; };
                SetDutyAndDirection(Motor);
                Thread.Sleep(Motor.DutyCycleChangeIntervalMs);
            }
        }

        // A cancelled Accelerate will leave the motor running at the last set speed. This is allowed in order to facilite smoother transitions
        // between motor commands, and a quicker response time
        private void Accelerate(IMotor Motor, MotorCommandEventModel MotorRequest, CancellationToken Token)
        {
            float targetPwm = MotorRequest.PwmAmount;
            float stopAmount = (float)Math.Floor(Motor.PwmSoftCaps.StopPwm * servoBoardController.PwmMaxPercent);
            if (Motor.CurrentPwmAmount < stopAmount) { Motor.CurrentPwmAmount = stopAmount; }
            if (targetPwm <= Motor.CurrentPwmAmount) { return; }
            float runAmount = (float)Math.Floor(Motor.PwmSoftCaps.RunPwm * servoBoardController.PwmMaxPercent);
            Console.WriteLine($"Target= {targetPwm}, softCap= {runAmount}, PwmSoftCaps.RunPw={Motor.PwmSoftCaps.RunPwm}, PwmMaxPercent={servoBoardController.PwmMaxPercent}");
            if (targetPwm > runAmount) { targetPwm = runAmount; }
            float pwmChangeAmount = (float)Math.Floor(servoBoardController.PwmMaxPercent * Motor.DutyCycleChangeStepPct);
            Console.WriteLine($"*DEBUG* MOTOR-CONTROLLER [Accelerate] Current speed {Motor.CurrentPwmAmount} Target speed {targetPwm} Stepping {pwmChangeAmount}");
            while (Motor.CurrentPwmAmount < targetPwm)
            {
                Token.ThrowIfCancellationRequested();
                Motor.CurrentPwmAmount += pwmChangeAmount;
                if (Motor.CurrentPwmAmount > targetPwm) { Motor.CurrentPwmAmount = targetPwm; }
                SetDutyAndDirection(Motor);
                Thread.Sleep(Motor.DutyCycleChangeIntervalMs);
            }
        }

        private void HardStop(MotorCommandEventModel MotorRequest)
        {
            motorRegistry[MotorRequest.Command].Motor.CurrentPwmAmount = 0f;
            SetDutyAndDirection(motorRegistry[MotorRequest.Command].Motor);
        }
    }
}