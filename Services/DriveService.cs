using AV00.Controllers.MotorController;
using System.Collections.Specialized;
using System.Configuration;
using AV00.Communication;
using NetMQ;
using AV00.Shared;
using AV00.Drivers.Motors;

namespace AV00.Services
{
    internal class DriveService: IService
    {
        public string ServiceName { get => "DriveService"; }
        private readonly TaskExecutorClient taskExecutorClient;
        private readonly IMotorController motorController;
        private readonly int updateFrequency = 1;
        private readonly ushort CommandBackoffMs = 10;
        private readonly Dictionary<Guid, TaskEvent> TasksInProgress = new();

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
            Console.WriteLine($"@DRIVER-SERVICE: [Received] TaskEvent {WireMessage[3]}");
            Console.WriteLine($"@DRIVER-SERVICE: [Received] TaskEvent {WireMessage[3].ConvertToString()}");
            TaskEvent taskEvent = new();
            taskEvent.Deserialize(WireMessage);
            Console.WriteLine($"DRIVER-SERVICE: [Received] TaskEvent {taskEvent.ServiceName}");
            Console.WriteLine($"DRIVER-SERVICE: [Received] TaskEvent {taskEvent.Type}");
            Console.WriteLine($"DRIVER-SERVICE: [Received] TaskEvent {taskEvent.Id}");
            Console.WriteLine($"DRIVER-SERVICE: [Received] TaskEvent {taskEvent.Data.Command}");
            Console.WriteLine($"DRIVER-SERVICE: [Received] TaskEvent {taskEvent.Data.Direction}");
            Console.WriteLine($"DRIVER-SERVICE: [Received] TaskEvent {taskEvent.Data.PwmAmount}");

            ExecutionControl(taskEvent);


            return true;
        }

        private void CancelAllTasks()
        {
            foreach (var task in TasksInProgress)
            {
                task.Value.Data.CancellationToken.IsCancellationRequested = true;
                Console.WriteLine($"DRIVER-SERVICE: [Issuing] TaskEventReceipt for event: {task.Value.Id}");
                taskExecutorClient.PublishReceipt(task.Value.GenerateReceipt(EnumTaskEventProcessingState.Cancelled));
            }
            TasksInProgress.Clear();
        }

        private async Task CompleteTask(Guid TaskEventId)
        {
            await Task.Run(() =>
                {
                    TasksInProgress.TryGetValue(TaskEventId, out TaskEvent? taskEvent);
                    if (taskEvent != null)
                    {
                        Console.WriteLine($"DRIVER-SERVICE: [Issuing] TaskEventReceipt for event: {taskEvent.Id}");
                        taskExecutorClient.PublishReceipt(taskEvent.GenerateReceipt(EnumTaskEventProcessingState.Completed));
                        TasksInProgress.Remove(TaskEventId);
                    }
                }
            );
        }

        // Execution control goals:
        // If blocking, do not allow motor to be used until lock has expired
        // If non-blocking, queue or blend the new command with the current command
        // If override, stop the current command and run the new command

        // TODO: This is a mess. Refactor this to be more readable and maintainable
        private void ExecutionControl(TaskEvent CurrentTask)
        {
            IMotor activeMotor = motorController.GetMotorByCommand(CurrentTask.Data.Command);
            if (CurrentTask.Data.Mode == EnumExecutionMode.Override)
            {
                CancelAllTasks();
                TasksInProgress.Add(CurrentTask.Id, CurrentTask);
                activeMotor.IsReserved = true;
                var task = Execute(CurrentTask.Data);
                activeMotor.IsReserved = false;
                task.ContinueWith(t => CompleteTask(CurrentTask.Id));
            }
            else if (activeMotor.IsReserved)
            {
                while (activeMotor.IsReserved)
                {
                    Thread.Sleep(CommandBackoffMs);
                }
            }

            if (CurrentTask.Data.Mode == EnumExecutionMode.Blocking)
            {
                TasksInProgress.Add(CurrentTask.Id, CurrentTask);
                activeMotor.IsReserved = true;
                var task = Execute(CurrentTask.Data);
                activeMotor.IsReserved = false;
                task.ContinueWith(t => CompleteTask(CurrentTask.Id));
            }
            else if (CurrentTask.Data.Mode == EnumExecutionMode.NonBlocking)
            {
                TasksInProgress.Add(CurrentTask.Id, CurrentTask);
                var task = Execute(CurrentTask.Data);
                task.ContinueWith(t => CompleteTask(CurrentTask.Id));
            }
        }

        private async Task Execute(MotorCommandData MotorRequest)
        {
            await Task.Run(() =>
                {
                    motorController.Run(MotorRequest);
                }
            );
        }
    }
}
