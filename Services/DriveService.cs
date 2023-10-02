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
        private readonly int updateFrequency = 10;
        private readonly int backoffFrequencyMs = 10;
        private readonly bool enableDebugLogging = false;
        private bool isOverrideInQueue = false;
        private Dictionary<EnumMotorCommands, bool> activeOverrides = new();

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
            var _ = QueueRunner();
            while (true)
            {
                taskExecutorClient.ProcessPendingEvents();
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
                    activeOverrides[motorEvent.Data.Command] = true;
                }
                motorController.MotorCommandQueues[motorEvent.Data.Command].Enqueue(motorEvent.Data);
            }
            catch (Exception e)
            {
                Console.WriteLine($"DRIVER-SERVICE: [Error] Failed to deserialize MotorEvent {e.Message}");
                return false;
            }
            return true;
        }

        private async Task QueueRunner()
        {
            await Task.Run(() =>
                {
                    while (true)
                    {
                        Task.WaitAll(motorController.MotorCommandQueues.Values.Select((queue, queueType) => ProcessQueue(queue, (EnumMotorCommands)queueType)).ToArray());
                        Thread.Sleep(updateFrequency);
                    }
                }
            );
        }

        private async Task ProcessQueue(Queue<MotorCommandData> MotorCommandQueue, EnumMotorCommands CommandQueueType)
        {
            await Task.Run(() =>
                {
                    int NumberPendingOfMotorEvents = MotorCommandQueue.Count;
                    for (int motorEventIndex = 0; motorEventIndex < NumberPendingOfMotorEvents; motorEventIndex++)
                    {
                        MotorCommandData currentCommand = MotorCommandQueue.Dequeue();
                        if (activeOverrides[CommandQueueType] && currentCommand.Mode != EnumExecutionMode.Override)
                        {
                            //IssueCommandReceipt(currentCommand, EnumEventProcessingState.Rejected, "Override in queue");
                            continue;
                        }
                        Execute(currentCommand);
                    }
                    activeOverrides[CommandQueueType] = false;
                }
            );
        }

        private void Execute(MotorCommandData CurrentCommand)
        {
            motorController.Run(CurrentCommand);
        }

        private void IssueCommandReceipt(MotorEvent CurrentEvent, EnumEventProcessingState ExecutionState, string ReasonForExecutionState)
        {
            Console.WriteLine($"DRIVER-SERVICE: [Issuing] EventReceipt <{ExecutionState}> for MotorEvent {CurrentEvent.Id}");
            taskExecutorClient.PublishReceipt(CurrentEvent.GenerateReceipt(ExecutionState, ReasonForExecutionState));
        }
    }
}
