﻿// See https://aka.ms/new-console-template for more information
// https://github.com/dotnet/iot/blob/main/src/System.Device.Gpio/System/Device/I2c/I2cDevice.cs#L11
// https://developer.download.nvidia.com/assets/embedded/secure/jetson/xavier/docs/Xavier_TRM_DP09253002.pdf?VakqmUGBQ99wvpu9mQgUOnm_mwq8MupZBel1lXFho98PGkC6gCjYKa58bYSGYUZZYHMpkF-PPS9WNeV_KmsmQxUQX84dYaEQavkufeAuom2ZpN2P8oQ3I1gSljfF-7rzC1sxKAXvgDnQlzdmma_jvCdqtjyKvye5MFmhFtSSG63Huw==&t=eyJscyI6ImdzZW8iLCJsc2QiOiJodHRwczovL3d3dy5nb29nbGUuY29tLyJ9
// https://github.com/NVIDIA/jetson-gpio/blob/6cab53dc80f8f5ecd6257d90dc7c0c51cb5348a7/lib/python/Jetson/GPIO/gpio_pin_data.py#L321
// https://forums.developer.nvidia.com/t/gpio-numbers-and-sysfs-names-changed-in-jetpack-5-linux-5-10/218580/7
// https://forums.developer.nvidia.com/t/gpio-numbers-and-sysfs-names-changed-in-jetpack-5-linux-5-10/218580/3
// https://jetsonhacks.com/nvidia-jetson-xavier-nx-gpio-header-pinout/
using sensors_test.Services;
using sensors_test.Drivers.IO;
using sensors_test.Drivers.Motors;
using sensors_test.Controllers.MotorController;
using sensors_test.Drivers.ExpansionBoards;
using Transport.Relay;
using System.Configuration;
using Transport.Client;
using NetMQ;
using Transport.Messages;
using System.ServiceModel.Channels;

namespace sensors_test
{
    using DataDict = Dictionary<string, object>;
    using MQMessageBuffer = List<(NetMQMessage, bool?)>;

    public static class DEBUG
    {
        public static void DebugPrintResults(short[] results)
        {
            Console.WriteLine($"Read int X: {results[0]}");
            Console.WriteLine($"Read int Y: {results[1]}");
            Console.WriteLine($"Read int Z: {results[2]}");
        }
    }

    // Digital Port: IO expansion board offers 28 groups (D0-D27) of digital ports that are led out via Raspberry Pi ports GPIO0~GPIO27 (BCM codes)
    // Requires libgpiod-dev (Could not get to work on JP4)
    // sudo apt install -y libgpiod-dev
    // gpiochip1 [tegra-gpio-aon] (40 lines)
    // Gpiod uses line numbers to access gpio. The line numbers can be found with `gpioinfo` and cross referenced with the nvidia 
    // pinmux spreadsheet.
    // The pinmux spreadsheet can be found here: https://developer.nvidia.com/jetson-nano-pinmux-datasheet
    // Notes for personal use, these are free GPIO pins.
    // gpiochip1 127 -- BCM 17 -- J41 Pin 11
    // gpiochip1 112 -- BCM 18 -- J41 Pin 12


    // PWM2 on MD10A is driver motor, PWM1 is turning motor

    public class Program
    {
        private static readonly byte boardBusId = 8;
        // private static readonly byte MPU9250Address = 0x68;
        // Device may be missing/broken on board
        // private static readonly byte AK8963Address = 0x0C;
        // private static readonly byte BMP280 = 0x77;
        private static readonly int pwmFrequency = 1000;
        private static readonly int GpioControllerId = 1;

        private static bool TestCallback(NetMQMessage message)
        {
            Console.WriteLine($"Test Callback: {message}");
            return true;
        }

        public static void Main()
        {
            Console.WriteLine("Starting");
            DeviceRegistryService DeviceRegistry = new();
            ServiceRegistry.AddService(DeviceRegistry);

            ServiceBusRelay TransportRelay = new(ConfigurationManager.ConnectionStrings, ConfigurationManager.AppSettings);
            ThreadStart transportRelayThreadDelegate = new(TransportRelay.ForwardMessages);
            Thread transportRelayThread = new(transportRelayThreadDelegate);

            ServiceBusClient serviceBusClient = new(ConfigurationManager.ConnectionStrings);
            serviceBusClient.RegisterServiceEventCallback("DriveService", TestCallback);
            Console.WriteLine($"Starting Transport Relay");
            transportRelayThread.Start();

            DataDict myData = new() { { "message", "Hello World" } };
            TaskEvent myTask = new("DriveService", myData);
            serviceBusClient.PushTask(myTask);

            serviceBusClient.ProcessPendingEvents();

            Console.WriteLine("Press any key to exit");
            Console.ReadKey();
            Environment.Exit(0);
            //GPIO Gpio = new(GpioControllerId);
            //PCA9685 PwmController = new(boardBusId);
            //PWM ServoBoard = new(PwmController);
            //ServoBoard.SetPwmFrequency(pwmFrequency);
            //IMotor DriveMotor = new MDD10A55072 (112, 8, "DriveMotor");
            //IMotor TurningMotor = new MDD10A39012(127, 9, "TurningMotor");

            //Console.WriteLine($"Add Drive Motor To Registry");
            //DeviceRegistry.AddDevice(DriveMotor);
            //Console.WriteLine($"Add Turn Motor To Registry");
            //DeviceRegistry.AddDevice(TurningMotor);

            //PDSGBGearboxMotorController motorController = new(Gpio, ServoBoard, TurningMotor, DriveMotor);
            //Console.WriteLine($"Run Test:");
            //motorController.Test();


            // ---
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