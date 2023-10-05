using AV00.Controllers.MotorController;
using System.Collections.Specialized;
using System.Configuration;
using AV00.Communication;
using NetMQ;
using Transport.Messages;
using AV00_Shared.FlowControl;

namespace AV00.Services
{
    using MotorEvent = Event<MotorCommandData>;

    internal class DriveService : IService
    {
        public string ServiceName { get => "DriveService"; }
        private readonly TaskExecutorClient taskExecutorClient;
        private readonly IMotorController motorController;
        private readonly int updateFrequency = 10;
        private readonly bool enableDebugLogging = false;
        private Dictionary<EnumMotorCommands, MotorCommandData> activeOverrides = new();
        private Dictionary<EnumMotorCommands, CancellationTokenSource> cancellationSources = new();

        public DriveService(IMotorController MotorController, ConnectionStringSettingsCollection Connections, NameValueCollection Settings)
        {
            taskExecutorClient = new(Connections, Settings);
            motorController = MotorController;
            updateFrequency = int.Parse(Settings["DriveServiceUpdateFrequency"] ?? throw new Exception());
            taskExecutorClient.RegisterServiceEventCallback(ServiceName, OnTaskEventCallback);
            enableDebugLogging = bool.Parse(Settings["RelayEnableDebugLogging"] ?? throw new Exception());
            InitializeCancellationTokenSources();
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

        private void InitializeCancellationTokenSources()
        {
            foreach (EnumMotorCommands command in Enum.GetValues(typeof(EnumMotorCommands)))
            {
                cancellationSources.Add(command, new());
            }
        }

        // This task has sole authority to cancel cancellation sources.
        private bool OnTaskEventCallback(NetMQMessage WireMessage)
        {
            Console.WriteLine($"DRIVER-SERVICE: [Received] MotorEvent {WireMessage[3].ConvertToString()}");
            try
            {
                MotorEvent motorEvent = MotorEvent.Deserialize(WireMessage);
                if (motorEvent.Data.Mode == EnumExecutionMode.Override)
                {
                    Console.WriteLine($"DRIVER-SERVICE: [Warning] Cancelling Token for source: {motorEvent.Data.Command}");
                    cancellationSources.TryGetValue(motorEvent.Data.Command, out CancellationTokenSource? source);
                    source?.Cancel();
                    activeOverrides[motorEvent.Data.Command] = motorEvent.Data;
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
                    Console.WriteLine($"QUEUE-RUNNER: [Info] QueueRunner started");
                    Dictionary<EnumMotorCommands, Task> activeTasks = new();
                    while (true)
                    {
                        try
                        {
                            foreach (var (queuetype, queue) in motorController.MotorCommandQueues)
                            {
                                activeTasks[queuetype] = ProcessQueue(queue, queuetype);
                                Task.WaitAll(activeTasks.Values.ToArray());
                            }
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine($"QUEUE-RUNNER: [Error] something broke - {e.Message}");
                        }
                        Thread.Sleep(updateFrequency);
                    }
                }
            );
        }

        // This task has sole authority to create new cancellation sources.
        private async Task ProcessQueue(Queue<MotorCommandData> MotorCommandQueue, EnumMotorCommands CommandQueueType)
        {
            await Task.Run(() =>
                {
                    int NumberPendingOfMotorEvents = MotorCommandQueue.Count;
                    Console.WriteLine($"*DEBUG* QUEUE-RUNNER: [Info] Executing queue - {CommandQueueType}[{NumberPendingOfMotorEvents}]");
                    for (int motorEventIndex = 0; motorEventIndex < NumberPendingOfMotorEvents; motorEventIndex++)
                    {
                        MotorCommandData currentCommand = MotorCommandQueue.Dequeue();
                        if (cancellationSources[CommandQueueType].Token.IsCancellationRequested)
                        {
                            if (currentCommand.CommandId != activeOverrides[CommandQueueType].CommandId)
                            {
                                IssueCommandReceipt(currentCommand, EnumEventProcessingState.Rejected, "Command cancelled prior to execution");
                                continue;
                            }
                            Console.WriteLine($"QUEUE-RUNNER: [Info] creating new cancellationSource");
                            cancellationSources[CommandQueueType] = new();
                        }
                        try
                        {
                            motorController.Run(currentCommand, cancellationSources[CommandQueueType].Token);
                            IssueCommandReceipt(currentCommand, EnumEventProcessingState.Completed, "Command executed");
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine($"DRIVER-SERVICE: [Error] Failed to run MotorEvent {currentCommand.CommandId} - {e.Message}");
                            IssueCommandReceipt(currentCommand, EnumEventProcessingState.Cancelled, "Command cancelled in progress");
                        }
                    }
                }
            );
        }

        private void IssueCommandReceipt(MotorCommandData CommandData, EnumEventProcessingState ExecutionState, string ReasonForExecutionState)
        {
            Event<MotorCommandData> @event = new(ServiceName, CommandData.CommandId, EnumEventType.Event);
            Console.WriteLine($"DRIVER-SERVICE: [Issuing] EventReceipt <{ExecutionState}> for MotorEvent {CommandData.CommandId}");
            taskExecutorClient.PublishReceipt(@event.GenerateReceipt(ExecutionState, ReasonForExecutionState));
        }
    }
}
