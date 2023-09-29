﻿using System.Device.Gpio;
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

    internal class PDSGBGearboxMotorController: IMotorController
    {
        private readonly IMotor turningMotor;
        private readonly IMotor driveMotor;
        private readonly PWM servoBoardController;
        private readonly GPIO gpio;

        public PDSGBGearboxMotorController(GPIO Gpio, PWM ServoBoardController, IMotor TurningMotor, IMotor DriveMotor)
        {
            gpio = Gpio;
            turningMotor = TurningMotor;
            driveMotor = DriveMotor;
            servoBoardController = ServoBoardController;
        }

        public void Test()
        {
            using (StreamWriter outputFile = new(Path.Combine(Environment.CurrentDirectory, "pwm-log.txt"), true))
            {
                outputFile.WriteLine("New Test -----------------");
            }
            Console.WriteLine("New Test -----------------");
            Console.WriteLine("-- Move Forwads");
            Move(MotorDirection.Forwards, 1024, EnumExecutionMode.Blocking);
            Thread.Sleep(1000);
            Console.WriteLine("-- Move Backwards");
            Move(MotorDirection.Backwards, 1024, EnumExecutionMode.Blocking);
            Thread.Sleep(1000);
            Move(MotorDirection.Backwards, 0, EnumExecutionMode.Blocking);
            Console.WriteLine("-- Rotate Left");
            Turn(MotorDirection.Left, 1024, EnumExecutionMode.Blocking);
            Thread.Sleep(1000);
            Console.WriteLine("-- Rotate Right");
            Turn(MotorDirection.Right, 1024, EnumExecutionMode.Blocking);
            Thread.Sleep(1000);
            Turn(MotorDirection.Right, 0, EnumExecutionMode.Blocking);
            Console.WriteLine("-- All Stop");
            Stop(EnumExecutionMode.Blocking);

            Console.WriteLine($"{turningMotor.Name} Current PWM: {turningMotor.CurrentPwmAmount}");
            Console.WriteLine($"{driveMotor.Name} Current PWM: {driveMotor.CurrentPwmAmount}");
            Console.WriteLine($"{turningMotor.Name} - Test Complete -----------------");
            Console.WriteLine($"{driveMotor.Name} - Test Complete -----------------");
        }

        public void Move(PinValue Direction, ushort PwmAmount, EnumExecutionMode Mode)
        {
            RunMotor(driveMotor, Direction, PwmAmount, Mode);
        }

        public void Turn(PinValue Direction, ushort PwmAmount, EnumExecutionMode Mode)
        {
            RunMotor(turningMotor, Direction, PwmAmount, Mode);
        }

        public void Stop(EnumExecutionMode Mode)
        {
            HardStop(turningMotor, Mode);
            HardStop(driveMotor, Mode);
        }

        // Execution control goals:
        // If blocking, do not allow motor to be used until lock has expired
        // If non-blocking, queue or blend the new command with the current command
        // If override, stop the current command and run the new command
        private void RunMotor(IMotor Motor, PinValue Direction, ushort PwmAmount, EnumExecutionMode Mode)
        {
            using (StreamWriter outputFile = new(Path.Combine(Environment.CurrentDirectory, "pwm-log.txt"), true))
            {
                outputFile.WriteLine($"-- Run Motor: {Motor.Name}-{Motor.PwmChannelId}, Direction: {Direction}, Speed: {PwmAmount}");
            }
            Console.WriteLine($"-- Run Motor: {Motor.Name}-{Motor.PwmChannelId}, Direction: {Direction}, Speed: {PwmAmount}");
            if (Direction != Motor.CurrentDirection)
            {
                GradualStop(Motor);
            }
            Motor.CurrentDirection = Direction;
            if (PwmAmount == Motor.CurrentPwmAmount) { return; }
            else if (PwmAmount > Motor.CurrentPwmAmount)
            {
                Accelerate(Motor, PwmAmount);
            }
            else if (PwmAmount < Motor.CurrentPwmAmount)
            {
                Decelerate(Motor, PwmAmount);
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