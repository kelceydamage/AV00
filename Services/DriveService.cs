using AV00.Controllers.MotorController;
using System.Collections.Specialized;
using System.Configuration;
using AV00.Communication;
using NetMQ;
using AV00.Shared;
using Transport.Messages;
using System.Threading;

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

        private bool OnTaskEventCallback(NetMQMessage WireMessage)
        {
            Console.WriteLine($"DRIVER-SERVICE: [Received] MotorEvent {WireMessage[3].ConvertToString()}");
            try
            {
                MotorEvent motorEvent = MotorEvent.Deserialize(WireMessage);
                if (motorEvent.Data.Mode == EnumExecutionMode.Override)
                {
                    Console.WriteLine($"Cancellation Token for source: {motorEvent.Data.Command}");
                    cancellationSources.TryGetValue(motorEvent.Data.Command, out CancellationTokenSource? source);
                    source?.Cancel();
                    Console.WriteLine($"Token state: {source?.Token.IsCancellationRequested}");
                    activeOverrides[motorEvent.Data.Command] = motorEvent.Data;
                    isOverrideInQueue = true;
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
                                //if (cancellationSources[queuetype].IsCancellationRequested)
                                //{
                                //    Console.WriteLine($"QUEUE-RUNNER: [New] creating new cancellation source");
                                //    cancellationSources[queuetype] = new();
                                //}
                                activeTasks[queuetype] = ProcessQueue(queue, queuetype, cancellationSources[queuetype].Token, isOverrideInQueue);
                            }
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine($"QUEUE-RUNNER: [Error] something broke - {e.Message}");
                        }
                        {
                            try
                            {
                                Task.WaitAll(activeTasks.Values.ToArray());
                                Console.WriteLine($"QUEUE-RUNNER: [Info] Finished queues");
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine($"QUEUE-RUNNER: [Error] something broke2 - {e.Message}");
                            }
                            Thread.Sleep(updateFrequency);
                        }
                    }
                }
            );
        }

        private async Task ProcessQueue(Queue<MotorCommandData> MotorCommandQueue, EnumMotorCommands CommandQueueType, CancellationToken Token, bool IsOverrideInQueue)
        {
            await Task.Run(() =>
                {
                    int NumberPendingOfMotorEvents = MotorCommandQueue.Count;
                    Console.WriteLine($"QUEUE-RUNNER: [Info] Executing queue - {CommandQueueType}[{NumberPendingOfMotorEvents}]");
                    //Token.ThrowIfCancellationRequested();
                    for (int motorEventIndex = 0; motorEventIndex < NumberPendingOfMotorEvents; motorEventIndex++)
                    {
                        Console.WriteLine("1");
                        MotorCommandData currentCommand = MotorCommandQueue.Dequeue();
                        var q = activeOverrides.TryGetValue(CommandQueueType, out MotorCommandData O);
                        if (q)
                        {
                            Console.WriteLine($"QUEUE-RUNNER: [Info] token={Token.IsCancellationRequested} id={currentCommand.CommandId} override?={O.CommandId}");
                        }
                        else
                        {
                            Console.WriteLine($"QUEUE-RUNNER: [Info] token={Token.IsCancellationRequested} id={currentCommand.CommandId}");
                        }
                        Console.WriteLine("2");
                        if (Token.IsCancellationRequested && currentCommand.CommandId != activeOverrides[CommandQueueType].CommandId)
                        {
                            //IssueCommandReceipt(currentCommand, EnumEventProcessingState.Rejected, "Override in queue");
                            continue;
                        }
                        else if (Token.IsCancellationRequested && currentCommand.CommandId == activeOverrides[CommandQueueType].CommandId)
                        {
                            Console.WriteLine($"QUEUE-RUNNER: creating new cancellationSource");
                            cancellationSources[CommandQueueType] = new();
                        }
                        try
                        {
                            Console.WriteLine($"QUEUE-RUNNER: [Info] Given token {Token.IsCancellationRequested} Active token {cancellationSources[CommandQueueType].Token.IsCancellationRequested}");
                            Console.WriteLine($"QUEUE-RUNNER: [Info] executing command");
                            // This is not working as intended
                            motorController.Run(currentCommand, cancellationSources[CommandQueueType].Token);
                            isOverrideInQueue = false;
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine($"DRIVER-SERVICE: [Error] Failed to run MotorEvent {currentCommand.CommandId} - {e.Message}");
                        }
                    }
                }
            );
        }

        private void IssueCommandReceipt(MotorEvent CurrentEvent, EnumEventProcessingState ExecutionState, string ReasonForExecutionState)
        {
            Console.WriteLine($"DRIVER-SERVICE: [Issuing] EventReceipt <{ExecutionState}> for MotorEvent {CurrentEvent.Id}");
            taskExecutorClient.PublishReceipt(CurrentEvent.GenerateReceipt(ExecutionState, ReasonForExecutionState));
        }
    }
}
