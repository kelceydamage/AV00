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
        private Dictionary<EnumMotorCommands, MotorCommandData> activeOverrides = new();
        private Dictionary<EnumMotorCommands, CancellationTokenSource> cancellationSources = new();

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

        private void InitializeCancellationTokenSources()
        {
            foreach (EnumMotorCommands command in Enum.GetValues(typeof(EnumMotorCommands)))
            {
                cancellationSources.Add(command, new());
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
                    Console.WriteLine($"Sending Cancellation Token for source: {motorEvent.Data.Command}");
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
                    Dictionary<EnumMotorCommands, Task> ActiveTasks = new();
                    while (true)
                    {
                        foreach (var (queuetype, queue) in motorController.MotorCommandQueues)
                        {
                            ActiveTasks.Add(queuetype, ProcessQueue(queue, queuetype, cancellationSources[queuetype].Token));
                        }
                        Task.WaitAll(ActiveTasks.Values.ToArray());
                        
                        Thread.Sleep(updateFrequency);
                        foreach (var (name, source) in cancellationSources)
                        {
                            if (source.Token.IsCancellationRequested)
                            {
                                Console.WriteLine($"DRIVER-SERVICE: [Info] QueueRunner cancelled");
                                cancellationSources[name] = new();
                            }
                        }
                    }
                }
            );
        }

        private async Task ProcessQueue(Queue<MotorCommandData> MotorCommandQueue, EnumMotorCommands CommandQueueType, CancellationToken Token)
        {
            await Task.Run(() =>
                {
                    Token.ThrowIfCancellationRequested();
                    int NumberPendingOfMotorEvents = MotorCommandQueue.Count;
                    for (int motorEventIndex = 0; motorEventIndex < NumberPendingOfMotorEvents; motorEventIndex++)
                    {
                        MotorCommandData currentCommand = MotorCommandQueue.Dequeue();
                        if (Token.IsCancellationRequested && currentCommand.CommandId != activeOverrides[CommandQueueType].CommandId)
                        {
                            //IssueCommandReceipt(currentCommand, EnumEventProcessingState.Rejected, "Override in queue");
                            continue;
                        }
                        else if (Token.IsCancellationRequested && currentCommand.CommandId == activeOverrides[CommandQueueType].CommandId)
                        {
                            cancellationSources[CommandQueueType] = new();
                        }
                        try
                        {
                            motorController.Run(currentCommand, cancellationSources[CommandQueueType].Token);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine($"DRIVER-SERVICE: [Error] Failed to run MotorEvent {e.Message}");
                        }
                    }
                }, Token
            );
        }

        private void IssueCommandReceipt(MotorEvent CurrentEvent, EnumEventProcessingState ExecutionState, string ReasonForExecutionState)
        {
            Console.WriteLine($"DRIVER-SERVICE: [Issuing] EventReceipt <{ExecutionState}> for MotorEvent {CurrentEvent.Id}");
            taskExecutorClient.PublishReceipt(CurrentEvent.GenerateReceipt(ExecutionState, ReasonForExecutionState));
        }
    }
}
