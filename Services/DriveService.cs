using AV00.Controllers.MotorController;
using System.Collections.Specialized;
using System.Configuration;
using AV00.Communication;
using NetMQ;
using AV00.Shared;
using Transport.Messages;
using System.Collections.ObjectModel;
using System.Collections.Generic;

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
        private readonly List<MotorEvent> commandBuffer = new();
        private readonly List<MotorEvent> overrideBuffer = new();

        public DriveService(IMotorController MotorController, ConnectionStringSettingsCollection Connections, NameValueCollection Settings)
        {
            taskExecutorClient = new(Connections, Settings);
            motorController = MotorController;
            updateFrequency = int.Parse(Settings["DriveServiceUpdateFrequency"] ?? throw new Exception());
            taskExecutorClient.RegisterServiceEventCallback(ServiceName, OnTaskEventCallback);
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
            Console.WriteLine($"@DRIVER-SERVICE: [Received] TaskEvent {WireMessage[3].ConvertToString()}");
            try
            {
                MotorEvent motorEvent = MotorEvent.Deserialize(WireMessage);
                if (motorEvent.Data.Mode == EnumExecutionMode.Override)
                {
                    overrideBuffer.Add(motorEvent);
                    commandBuffer.Clear();
                }
                else
                {
                    commandBuffer.Add(motorEvent);
                }
            } catch (Exception e)
            {
                Console.WriteLine($"@DRIVER-SERVICE: [Error] Failed to deserialize MotorEvent: {e.Message}");
                return false;
            }
            return true;
        }

        private void ExecutionControl()
        {
            if (overrideBuffer.Count != 0)
            {
                var task2 = Execute(overrideBuffer, true);
            }
            var task1 = Execute(commandBuffer);
        }

        private async Task Execute(List<MotorEvent> CommandBuffer, bool IsOverride = false)
        {
            await Task.Run(() =>
                {
                    if (IsOverride)
                    {
                        CancelAllCommands();
                    }
                    foreach (var command in CommandBuffer)
                    {
                        QueueableMotor activeMotor = motorController.GetMotorByCommand(command.Data.Command);
                        while (activeMotor.IsReserved && !IsOverride)
                        {
                            Console.WriteLine($"DRIVER-SERVICE: [Warning] Motor {activeMotor.Motor.Name} is reserved by {activeMotor.ReservationId}");
                            Thread.Sleep(backoffFrequencyMs);
                        }
                        activeTasks.Add(command.Id, command);
                        activeMotor.IsReserved = true;
                        activeMotor.ReservationId = command.Id;
                        motorController.Run(command.Data);
                        activeMotor.ReservationId = Guid.Empty;
                        activeMotor.IsReserved = false;
                        activeTasks.Remove(command.Id);
                        IssueCommandReceipt(command, EnumTaskEventProcessingState.Completed);
                    }
                }
            );
        }

        private void CancelAllCommands()
        {
            foreach (var command in activeTasks)
            {
                activeTasks.Remove(command.Key);
                command.Value.Data.CancellationToken.IsCancellationRequested = true;
                IssueCommandReceipt(command.Value, EnumTaskEventProcessingState.Cancelled);
            }
        }

        private void IssueCommandReceipt(MotorEvent CurrentEvent, EnumTaskEventProcessingState ExecutionState)
        {
            Console.WriteLine($"DRIVER-SERVICE: [Issuing] TaskEventReceipt for event: {CurrentEvent.Id}");
            taskExecutorClient.PublishReceipt(CurrentEvent.GenerateReceipt(ExecutionState));
        }
    }
}
