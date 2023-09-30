using AV00.Controllers.MotorController;
using System.Collections.Specialized;
using System.Configuration;
using AV00.Communication;
using NetMQ;
using AV00.Shared;
using Transport.Messages;
using System.Collections.Generic;
using MassTransit.RabbitMqTransport;

namespace AV00.Services
{
    using MotorEvent = Event<MotorCommandData>;

    internal class DriveService: IService
    {
        public string ServiceName { get => "DriveService"; }
        private readonly TaskExecutorClient taskExecutorClient;
        private readonly IMotorController motorController;
        private readonly int updateFrequency = 1;
        private Dictionary<Guid, MotorEvent> activeTasks = new();

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
            while(true)
            {
                taskExecutorClient.ProcessPendingEvents();
                Thread.Sleep(updateFrequency);
            }
        }

        private bool OnTaskEventCallback(NetMQMessage WireMessage)
        {
            Console.WriteLine($"@DRIVER-SERVICE: [Received] TaskEvent {WireMessage[3].ConvertToString()}");
            try
            {
                MotorEvent motorEvent = MotorEvent.Deserialize(WireMessage);
                ExecutionControl(motorEvent);
            } catch (Exception e)
            {
                Console.WriteLine($"@DRIVER-SERVICE: [Error] Failed to deserialize MotorEvent: {e.Message}");
                return false;
            }
            return true;
        }

        // Likely a faster way to do this.
        private void CancelAllCommands(QueueableMotor ActiveMotor, MotorEvent MotorEvent)
        {
            while (ActiveMotor.MotorCommandQueue.Count > 0)
            {
                MotorCommandData command = ActiveMotor.MotorCommandQueue.Dequeue();
                Console.WriteLine($"Cancelling: {command.CommandId}");
                command.CancellationToken.IsCancellationRequested = true;
                Console.WriteLine($"DRIVER-SERVICE: [Issuing] TaskEventReceipt for event: {command.CommandId}");
                taskExecutorClient.PublishReceipt(MotorEvent.GenerateReceipt(EnumTaskEventProcessingState.Cancelled));
            }
            // HACK
            foreach (var motorEvent in activeTasks)
            {
                Console.WriteLine($"Cancelling: {motorEvent.Key}");
                motorEvent.Value.Data.CancellationToken.IsCancellationRequested = true;
                activeTasks.Remove(motorEvent.Key);
            }

            ActiveMotor.IsReserved = false;
            ActiveMotor.ReservationId = Guid.Empty;
        }

        private async Task CompleteTask(QueueableMotor ActiveMotor, MotorEvent MotorEvent)
        {
            await Task.Run(() =>
                {
                    if (ActiveMotor.ReservationId == MotorEvent.Id)
                    {
                        ActiveMotor.IsReserved = false;
                        ActiveMotor.ReservationId = Guid.Empty;
                    }
                    Console.WriteLine($"DRIVER-SERVICE: [Issuing] TaskEventReceipt for event: {MotorEvent.Id}");
                    taskExecutorClient.PublishReceipt(MotorEvent.GenerateReceipt(EnumTaskEventProcessingState.Completed));
                }
            );
        }

        // Execution control goals:
        // If blocking, do not allow motor to be used until lock has expired
        // If non-blocking, queue or blend the new command with the current command
        // If override, stop the current command and run the new command

        // TODO: This is a mess. Refactor this to be more readable and maintainable
        private void ExecutionControl(MotorEvent MotorEvent)
        {
            QueueableMotor activeMotor = motorController.GetMotorByCommand(MotorEvent.Data.Command);
            activeTasks.Add(MotorEvent.Id, MotorEvent);
            if (MotorEvent.Data.Mode == EnumExecutionMode.Override)
            {
                Console.WriteLine($"DRIVER-SERVICE: [Execute Override] for event: {MotorEvent.Id}");
                CancelAllCommands(activeMotor, MotorEvent);
                activeMotor.IsReserved = true;
                activeMotor.ReservationId = MotorEvent.Id;
                var task = ExecuteOverride(MotorEvent.Data);
                task.ContinueWith(t => CompleteTask(activeMotor, MotorEvent));
            }
            else if (MotorEvent.Data.Mode == EnumExecutionMode.Blocking)
            {
                Console.WriteLine($"DRIVER-SERVICE: [Execute] for event: {MotorEvent.Id}");
                activeMotor.MotorCommandQueue.Enqueue(MotorEvent.Data);
                var task = Execute(activeMotor);
                //task.ContinueWith(t => CompleteTask(activeMotor, MotorEvent));
            }
            else if (MotorEvent.Data.Mode == EnumExecutionMode.NonBlocking)
            {
                Console.WriteLine("DriveService does not support execution of non-blocking commands... Skipping");
            }
        }

        private async Task ExecuteOverride(MotorCommandData MotorCommand)
        {
            await Task.Run(() =>
                {
                    motorController.Run(MotorCommand);
                }
            );
        }

        private async Task Execute(QueueableMotor ActiveMotor)
        {
            await Task.Run(() =>
                {
                    while (ActiveMotor.MotorCommandQueue.Count > 0)
                    {
                        if (!ActiveMotor.IsReserved)
                        {
                            MotorCommandData motorCommand = ActiveMotor.MotorCommandQueue.Dequeue();
                            ActiveMotor.IsReserved = true;
                            ActiveMotor.ReservationId = motorCommand.CommandId;
                            motorController.Run(motorCommand);
                            ActiveMotor.IsReserved = false;
                            ActiveMotor.ReservationId = Guid.Empty;
                            activeTasks.Remove(motorCommand.CommandId);
                        }
                        else
                        {
                            Thread.Sleep(100);
                        }
                        Console.WriteLine($"QUEUE COUNT: {ActiveMotor.MotorCommandQueue.Count}");
                    }
                }
            );
        }
    }
}
