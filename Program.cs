using sensors_test.Services;
using Transport.Relay;
using System.Configuration;
using Transport.Client;
using NetMQ;
using Transport.Messages;
using sensors_test.Controllers.MotorController;
using sensors_test.Drivers.IO;
using sensors_test.Drivers.Motors;
using sensors_test.Drivers.ExpansionBoards;

namespace sensors_test
{
    using DataDict = Dictionary<string, object>;

    public static class DEBUG
    {
        public static void DebugPrintResults(short[] results)
        {
            Console.WriteLine($"Read int X: {results[0]}");
            Console.WriteLine($"Read int Y: {results[1]}");
            Console.WriteLine($"Read int Z: {results[2]}");
        }
    }

    public class Program
    {
        private static readonly byte boardBusId = 8;
        // private static readonly byte MPU9250Address = 0x68;
        // Device may be missing/broken on board
        // private static readonly byte AK8963Address = 0x0C;
        // private static readonly byte BMP280 = 0x77;
        private static readonly int pwmFrequency = 1000;
        private static readonly int GpioControllerId = 1;

        private static bool TestCallback(NetMQMessage MQMessage)
        {
            Console.WriteLine($"PROGRAM: [Received] receipt {MQMessage[2].ConvertToString()} status={Enum.Parse(typeof(EnumTaskEventProcessingState), MQMessage[3].ConvertToString())}");
            return true;
        }

        public static void Main()
        {
            Console.WriteLine("Starting");
            DeviceRegistryService deviceRegistry = new();
            ServiceRegistry.AddService(deviceRegistry);

            ServiceBusRelay transportRelay = new(ConfigurationManager.ConnectionStrings, ConfigurationManager.AppSettings);
            ThreadStart transportRelayThreadDelegate = new(transportRelay.ForwardMessages);
            Thread transportRelayThread = new(transportRelayThreadDelegate);

            PWM pwmDriver = new(new PCA9685(boardBusId));
            pwmDriver.SetPwmFrequency(pwmFrequency);
            PDSGBGearboxMotorController motorController = new(
                new GPIO(GpioControllerId),
                pwmDriver,
                new MDD10A55072(112, 8, "DriveMotor"),
                new MDD10A39012(127, 9, "TurningMotor")
            );
            DriveService driveService = new(motorController, ConfigurationManager.ConnectionStrings, ConfigurationManager.AppSettings);
            ThreadStart driveServiceThreadDelegate = new(driveService.Start);
            Thread driveServiceThread = new(driveServiceThreadDelegate);
            ServiceRegistry.AddService(driveService);

            ServiceBusClient serviceBusClient = new(ConfigurationManager.ConnectionStrings);
            serviceBusClient.RegisterServiceEventCallback("DriveService", TestCallback);
            Console.WriteLine($"Starting Transport Relay");
            transportRelayThread.Start();
            driveServiceThread.Start();

            DataDict myData = new() { { "message", "Hello World" } };
            TaskEvent myTask = new("DriveService", myData);
            Console.WriteLine($"PROGRAM: [Pushing] TaskEvent {myTask.Id}");
            serviceBusClient.PushTask(myTask);

            var i = 0;
            while(!Console.KeyAvailable)
            {
                serviceBusClient.ProcessPendingEvents();
                Thread.Sleep(500);

                var previousCursorY = Console.GetCursorPosition().Top;
                Console.SetCursorPosition(0, Console.WindowTop + Console.WindowHeight - 1);
                Console.Write($"* program loop: {i}");
                Console.SetCursorPosition(0, previousCursorY);
                i++;
            }
            Environment.Exit(0);
            /*
            I2cConnectionSettings MPU9250Settings = new(busId, MPU9250Address);
            MPU9250 mpu9250 = new(MPU9250Settings);
            I2cConnectionSettings AK8963Settings = new(busId, AK8963Address);
            AK8963 ak8963 = new(AK8963Settings);
            mpu9250.Initialize();
            short[] temp = mpu9250.ReadGyroscope();
            Console.WriteLine("Gyroscope:");
            DEBUG.DebugPrintResults(temp);
            temp = mpu9250.ReadAccelerometer();
            Console.WriteLine("Accelerometer:");
            DEBUG.DebugPrintResults(temp);
            short temp_s = mpu9250.ReadTemperature();
            Console.WriteLine($"Temperature: {temp_s}");
            temp = ak8963.ReadMagnetometer();
            DEBUG.DebugPrintResults(temp);
            */
        }
    }
}