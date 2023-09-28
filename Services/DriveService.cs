using sensors_test.Controllers.MotorController;
using System.Collections.Specialized;
using System.Configuration;
using System.Device.Gpio;
using Transport.Client;
using AV00.Communication;
using Transport.Messages;

namespace sensors_test.Services
{
    internal class DriveService: IService
    {
        public string ServiceName { get => "DriveService"; }
        private readonly TaskExecutorClient taskExecutorClient;
        private readonly IMotorController? motorController;
        private readonly int updateFrequency = 1;

        public DriveService(IMotorController? MotorController, ConnectionStringSettingsCollection Connections, NameValueCollection Settings)
        {
            taskExecutorClient = new(Connections);
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

        private bool OnTaskEventCallback(TransportMessage WireMessage)
        {
            TaskEvent taskEvent = new();
            taskEvent.Deserialize(WireMessage);
            Console.WriteLine($"DRIVER-SERVICE: [Received] TaskEvent {taskEvent.ServiceName}");
            Console.WriteLine($"DRIVER-SERVICE: [Received] TaskEvent {taskEvent.Type}");
            Console.WriteLine($"DRIVER-SERVICE: [Received] TaskEvent {taskEvent.Id}");
            Console.WriteLine($"DRIVER-SERVICE: [Received] TaskEvent {taskEvent.Data}");

            switch(taskEvent.Data.Command)
            {
                case "move":
                    motorController?.Move(taskEvent.Data.Direction,taskEvent.Data.PwmAmount);
                    break;
                case "turn":
                    motorController?.Turn(taskEvent.Data.Direction, taskEvent.Data.PwmAmount);
                    break;
                case "stop":
                    motorController?.Stop();
                    break;
                default:
                    break;
            }

            Console.WriteLine($"DRIVER-SERVICE: [Executing] TaskEvent {taskEvent.Data}");
            motorController?.Test();

            Console.WriteLine($"DRIVER-SERVICE: [Issuing] TaskEventReceipt for event: {taskEvent.Id}");
            taskExecutorClient.PublishReceipt(taskEvent);
            return true;
        }
    }
}
