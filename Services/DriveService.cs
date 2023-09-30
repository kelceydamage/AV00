﻿using AV00.Controllers.MotorController;
using System.Collections.Specialized;
using System.Configuration;
using AV00.Communication;
using NetMQ;
using AV00.Shared;
using Transport.Messages;

namespace AV00.Services
{
    using MotorEvent = Event<MotorCommandData>;

    internal class DriveService : IService
    {
        public string ServiceName { get => "DriveService"; }
        private readonly TaskExecutorClient taskExecutorClient;
        private readonly IMotorController motorController;
        private readonly int updateFrequency = 1;
        private readonly int backoffFrequencyMs = 100;
        private readonly Dictionary<Guid, MotorEvent> activeTasks = new();
        private readonly Queue<MotorEvent> commandBuffer = new();
        private readonly Queue<MotorEvent> overrideBuffer = new();
        private readonly bool enableDebugLogging = false;

        public DriveService(IMotorController MotorController, ConnectionStringSettingsCollection Connections, NameValueCollection Settings)
        {
            taskExecutorClient = new(Connections, Settings);
            motorController = MotorController;
            updateFrequency = int.Parse(Settings["DriveServiceUpdateFrequency"] ?? throw new Exception());
            taskExecutorClient.RegisterServiceEventCallback(ServiceName, OnTaskEventCallback);
            enableDebugLogging = bool.Parse(Settings["RelayEnableDebugLogging"] ?? throw new Exception());
        }

        public void Start()
        {
            Console.WriteLine("Drive Service Started");
            while (true)
            {
                taskExecutorClient.ProcessPendingEvents();
                ExecutionControl();
                Thread.Sleep(updateFrequency);
            }
        }

        private bool OnTaskEventCallback(NetMQMessage WireMessage)
        {
            Console.WriteLine($"DRIVER-SERVICE: [Received] MotorEvent {WireMessage[3].ConvertToString()}");
            try
            {
                MotorEvent motorEvent = MotorEvent.Deserialize(WireMessage);
                if (motorEvent.Data.Mode == EnumExecutionMode.Override)
                {
                    overrideBuffer.Enqueue(motorEvent);
                    commandBuffer.Clear();
                }
                else
                {
                    commandBuffer.Enqueue(motorEvent);
                }
            } catch (Exception e)
            {
                Console.WriteLine($"DRIVER-SERVICE: [Error] Failed to deserialize MotorEvent {e.Message}");
                return false;
            }
            return true;
        }

        private void ExecutionControl()
        {
            if (overrideBuffer.Count != 0)
            {
                CancelAllCommands();
                RegisterActiveCommands(overrideBuffer);
                var task2 = Execute(overrideBuffer, true);
            }
            if (commandBuffer.Count != 0)
            {
                RegisterActiveCommands(commandBuffer);
                var task1 = Execute(commandBuffer);
            }
        }

        private void RegisterActiveCommands(Queue<MotorEvent> PendingCommandBuffer)
        {
            foreach (var command in PendingCommandBuffer)
            {
                activeTasks.Add(command.Id, command);
            }
        }

        // TODO: Make to motors global objects so that the reservation system can be used with threads/tasks.
        private async Task Execute(Queue<MotorEvent> Buffer, bool IsOverride = false)
        {
            await Task.Run(() =>
                {
                    foreach (var _ in Buffer)
                    {
                        MotorEvent command = Buffer.Dequeue();
                        LockableMotor activeMotor = motorController.GetMotorByCommand(command.Data.Command);
                        if (activeMotor.IsReserved && !IsOverride)
                        {
                            Console.WriteLine($"DRIVER-SERVICE: [Warning] Motor {activeMotor.Motor.Name} is reserved by {activeMotor.ReservationId}, requestee {command.Id}");
                        }
                        while (activeMotor.IsReserved && !IsOverride && !command.Data.CancellationToken.IsCancellationRequested)
                        {
                            Thread.Sleep(backoffFrequencyMs);
                        }
                        Console.WriteLine($"DRIVER-SERVICE: [MotorLock] {activeMotor.Motor.Name} MotorEvent {activeMotor.ReservationId} is active {activeMotor.IsReserved}");
                        if (!command.Data.CancellationToken.IsCancellationRequested)
                        {
                            lock (activeMotor)
                            {
                                activeMotor.IsReserved = true;
                                activeMotor.ReservationId = command.Id;
                                motorController.Run(command.Data);
                                activeMotor.ReservationId = Guid.Empty;
                                activeMotor.IsReserved = false;
                            }
                        }
                        activeTasks.Remove(command.Id);
                        var ExecutionState = EnumTaskEventProcessingState.Completed;
                        if (command.Data.CancellationToken.IsCancellationRequested)
                            ExecutionState = EnumTaskEventProcessingState.Cancelled;
                        IssueCommandReceipt(command, ExecutionState);
                    }
                }
            );
        }

        private void CancelAllCommands()
        {
            foreach (var command in activeTasks)
            {
                command.Value.Data.CancellationToken.IsCancellationRequested = true;
                Console.WriteLine($"DRIVER-SERVICE: [Cancel] MotorEvent {command.Value.Id}");
            }
        }

        private void IssueCommandReceipt(MotorEvent CurrentEvent, EnumTaskEventProcessingState ExecutionState)
        {
            Console.WriteLine($"DRIVER-SERVICE: [Issuing] EventReceipt <{ExecutionState}> for MotorEvent {CurrentEvent.Id}");
            taskExecutorClient.PublishReceipt(CurrentEvent.GenerateReceipt(ExecutionState));
        }
    }
}
