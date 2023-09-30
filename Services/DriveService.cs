using AV00.Controllers.MotorController;
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
            Console.WriteLine($"@DRIVER-SERVICE: [Received] TaskEvent {WireMessage[3].ConvertToString()}");
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
                Console.WriteLine($"@DRIVER-SERVICE: [Error] Failed to deserialize MotorEvent: {e.Message}");
                return false;
            }
            return true;
        }

        private void ExecutionControl()
        {
            if (overrideBuffer.Count != 0)
            {
                CancelAllCommands();
                Console.WriteLine($"**** Run Overrides");
                var task2 = Execute(overrideBuffer, true);
            }
            if (commandBuffer.Count != 0)
            {
                var task1 = Execute(commandBuffer);
            }
        }

        // TODO: Make to motors global objects so that the reservation system can be used with threads/tasks.
        private async Task Execute(Queue<MotorEvent> Buffer, bool IsOverride = false)
        {
            await Task.Run(() =>
                {
                    if (IsOverride) CancelAllCommands();
                    foreach (var _ in Buffer)
                    {
                        MotorEvent command = Buffer.Dequeue();
                        QueueableMotor activeMotor = motorController.GetMotorByCommand(command.Data.Command);
                        Console.WriteLine($"**** MotorLock {activeMotor.Motor.Name} - {activeMotor.ReservationId} - {activeMotor.IsReserved}");
                        while (activeMotor.IsReserved && !IsOverride)
                        {
                            Console.WriteLine($"DRIVER-SERVICE: [Warning] Motor {activeMotor.Motor.Name} is reserved by {activeMotor.ReservationId}, requestee {command.Id}");
                            Thread.Sleep(backoffFrequencyMs);
                        }
                        //lock (activeMotor)
                        //{
                        activeMotor.IsReserved = true;
                        activeMotor.ReservationId = command.Id;
                        Console.WriteLine($" ------- Set lock {activeMotor.ReservationId} - {activeMotor.IsReserved}, requestee {command.Id}");
                        motorController.Run(command.Data);
                        Console.WriteLine($" ------- Unset lock {activeMotor.ReservationId} - {activeMotor.IsReserved}, requestee {command.Id}");
                        activeTasks.Remove(command.Id);
                        activeMotor.ReservationId = Guid.Empty;
                        activeMotor.IsReserved = false;
                        //}
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
            Console.WriteLine($"CANCEL Active Tasks: {activeTasks.Count}");
            foreach (var command in activeTasks)
            {
                command.Value.Data.CancellationToken.IsCancellationRequested = true;
                Console.WriteLine($"CANCEL: {command.Value.Id} - {command.Value.Data.CancellationToken.IsCancellationRequested}");
            }
        }

        private void IssueCommandReceipt(MotorEvent CurrentEvent, EnumTaskEventProcessingState ExecutionState)
        {
            Console.WriteLine($"DRIVER-SERVICE: [Issuing] TaskEventReceipt for event: {CurrentEvent.Id} ES-{ExecutionState}");
            taskExecutorClient.PublishReceipt(CurrentEvent.GenerateReceipt(ExecutionState));
        }
    }
}
