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
                var task2 = Execute(overrideBuffer, motorController, true);
            }
            if (commandBuffer.Count != 0)
            {
                var task1 = Execute(commandBuffer, motorController);
            }
        }

        // TODO: Make to motors global objects so that the reservation system can be used with threads/tasks.
        private async Task Execute(Queue<MotorEvent> Buffer, IMotorController SharedMotorController, bool IsOverride = false)
        {
            await Task.Run(() =>
                {
                    Console.WriteLine($"#####----- New Batch");
                    if (IsOverride)
                    {
                        Console.WriteLine($"**** Run Override");
                        CancelAllCommands();
                    }
                    foreach (var _ in Buffer)
                    {
                        MotorEvent command = Buffer.Dequeue();
                        QueueableMotor activeMotor = SharedMotorController.GetMotorByCommand(command.Data.Command);
                        Console.WriteLine($"**** MotorLock {activeMotor.Motor.Name} - {activeMotor.ReservationId} - {activeMotor.IsReserved}");
                        while (activeMotor.IsReserved && !IsOverride)
                        {
                            Console.WriteLine($"DRIVER-SERVICE: [Warning] Motor {activeMotor.Motor.Name} is reserved by {activeMotor.ReservationId}");
                            Thread.Sleep(backoffFrequencyMs);
                        }
                        activeTasks.Add(command.Id, command);
                        activeMotor.IsReserved = true;
                        activeMotor.ReservationId = command.Id;
                        Console.WriteLine($" ------- Set lock {activeMotor.ReservationId} - {activeMotor.IsReserved}");
                        motorController.Run(command.Data);
                        Console.WriteLine($" ------- Unset lock {activeMotor.ReservationId} - {activeMotor.IsReserved}");
                        activeMotor.ReservationId = Guid.Empty;
                        activeMotor.IsReserved = false;
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
                activeTasks.Remove(command.Key);
                command.Value.Data.CancellationToken.IsCancellationRequested = true;
                //IssueCommandReceipt(command.Value, EnumTaskEventProcessingState.Cancelled);
            }
        }

        private void IssueCommandReceipt(MotorEvent CurrentEvent, EnumTaskEventProcessingState ExecutionState)
        {
            Console.WriteLine($"DRIVER-SERVICE: [Issuing] TaskEventReceipt for event: {CurrentEvent.Id} ES-{ExecutionState}");
            taskExecutorClient.PublishReceipt(CurrentEvent.GenerateReceipt(ExecutionState));
        }
    }
}
