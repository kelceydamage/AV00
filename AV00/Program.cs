using AV00.Services;
using Transport.Relay;
using System.Configuration;
using Transport.Event;
using NetMQ;
using AV00.Controllers.MotorController;
using AV00.Drivers.IO;
using AV00.Drivers.Motors;
using AV00.Drivers.ExpansionBoards;
using Transport.Client;
using System.Device.Gpio;

namespace AV00
{
    using MotorEvent = Event<MotorCommandEventModel>;

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
        private static readonly int pwmFrequency = 20000;  // DFR0604 is capable of up to 48000hz frequency.
        private static readonly int GpioControllerId = 1;

        private static bool TestCallback(NetMQMessage MQMessage)
        {
            Console.WriteLine($"PROGRAM: [Received] receipt {MQMessage[3].ConvertToString()}");
            return true;
        }

        public static void Main()
        {
            Console.WriteLine("Starting");
            Console.WriteLine($"Pin High = {PinValue.High}, Pin Low = {PinValue.Low}");
            DeviceRegistryService deviceRegistry = new();
            ServiceRegistry.AddService(deviceRegistry);

            TransportRelay transportRelay = new(ConfigurationManager.ConnectionStrings, ConfigurationManager.AppSettings);
            ThreadStart transportRelayThreadDelegate = new(transportRelay.ForwardMessages);
            Thread transportRelayThread = new(transportRelayThreadDelegate);

            DFR0604 DFR0604 = new(boardBusId);
            EnumBoardStatus status = DFR0604.Init();
            Console.WriteLine($"STATUS: {status}, REASON: {DFR0604.ErrorMessage}");
            PWM pwmDriver = new(DFR0604);
            pwmDriver.SetPwmFrequency(pwmFrequency);
            Console.WriteLine($"STATUS: {DFR0604.LastOperationStatus}, REASON: {DFR0604.ErrorMessage}");
            PDSGBGearboxMotorController motorController = new(
                new GPIO(GpioControllerId),
                pwmDriver,
                new MDD10A39012(127, 1, "TurningMotor"),
                new MDD10A55072(112, 0, "DriveMotor")
            );
            Console.WriteLine($"STATUS: {DFR0604.LastOperationStatus}, REASON: {DFR0604.ErrorMessage}");
            DriveService driveService = new(motorController, ConfigurationManager.ConnectionStrings, ConfigurationManager.AppSettings);
            ThreadStart driveServiceThreadDelegate = new(driveService.Start);
            Thread driveServiceThread = new(driveServiceThreadDelegate);
            ServiceRegistry.AddService(driveService);

            TransportClient transportClient = new(ConfigurationManager.ConnectionStrings, ConfigurationManager.AppSettings);
            transportClient.RegisterServiceEventCallback("DriveService", TestCallback);
            Console.WriteLine($"Starting Transport Relay");
            transportRelayThread.Start();
            driveServiceThread.Start();

            
            MotorCommandEventModel eventModel = new("DriveService", EnumMotorCommands.Move, MotorDirection.Backwards, 0f);
            MotorEvent @event = new(eventModel);
            Console.WriteLine($"PROGRAM: [Pushing] TaskEvent {@event.Id}");
            transportClient.PushEvent(@event);

            /*
            MotorCommandEventModel eventModel = new("DriveService", EnumMotorCommands.Move, MotorDirection.Forwards, 30f);
            MotorEvent @event = new(eventModel);
            Console.WriteLine($"PROGRAM: [Pushing] TaskEvent {@event.Id}");
            transportClient.PushEvent(@event);
            Console.WriteLine($"STATUS1: {DFR0604.LastOperationStatus}, REASON: {DFR0604.ErrorMessage}");
            eventModel = new("DriveService", EnumMotorCommands.Move, MotorDirection.Forwards, 0f, EnumExecutionMode.Override);
            @event = new(eventModel);
            Console.WriteLine($"PROGRAM: [Pushing] TaskEvent {@event.Id}");
            transportClient.PushEvent(@event);
            Console.WriteLine($"STATUS2: {DFR0604.LastOperationStatus}, REASON: {DFR0604.ErrorMessage}");
            eventModel = new("DriveService", EnumMotorCommands.Move, MotorDirection.Forwards, 30f);
            @event = new(eventModel);
            Console.WriteLine($"PROGRAM: [Pushing] TaskEvent {@event.Id}");
            transportClient.PushEvent(@event);
            Console.WriteLine($"STATUS3: {DFR0604.LastOperationStatus}, REASON: {DFR0604.ErrorMessage}");
            Thread.Sleep(6000);

            eventModel = new("DriveService", EnumMotorCommands.Move, MotorDirection.Forwards, 0f, EnumExecutionMode.Override);
            @event = new(eventModel);
            Console.WriteLine($"PROGRAM: [Pushing] TaskEvent {@event.Id}");
            transportClient.PushEvent(@event);

            */


            var i = 0;
            while(!Console.KeyAvailable)
            {
                transportClient.ProcessPendingEvents();
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
