// See https://aka.ms/new-console-template for more information
using System.IO;
// https://github.com/dotnet/iot/blob/main/src/System.Device.Gpio/System/Device/I2c/I2cDevice.cs#L11
// https://developer.download.nvidia.com/assets/embedded/secure/jetson/xavier/docs/Xavier_TRM_DP09253002.pdf?VakqmUGBQ99wvpu9mQgUOnm_mwq8MupZBel1lXFho98PGkC6gCjYKa58bYSGYUZZYHMpkF-PPS9WNeV_KmsmQxUQX84dYaEQavkufeAuom2ZpN2P8oQ3I1gSljfF-7rzC1sxKAXvgDnQlzdmma_jvCdqtjyKvye5MFmhFtSSG63Huw==&t=eyJscyI6ImdzZW8iLCJsc2QiOiJodHRwczovL3d3dy5nb29nbGUuY29tLyJ9
// https://github.com/NVIDIA/jetson-gpio/blob/6cab53dc80f8f5ecd6257d90dc7c0c51cb5348a7/lib/python/Jetson/GPIO/gpio_pin_data.py#L321
using System.Device.I2c;
using sensors_test.Drivers.Sensors;
using sensors_test.Services;
using sensors_test.Drivers.IO;
using sensors_test.Drivers;
using sensors_test.Drivers.Motors;
using sensors_test.Controllers.MotorController;
using System.Device.Gpio;
using System.Device.Gpio.Drivers;

namespace sensors_test
{
    public static class DEBUG
    {
        public static void DebugPrintResults(short[] results)
        {
            Console.WriteLine($"Read int X: {results[0]}");
            Console.WriteLine($"Read int Y: {results[1]}");
            Console.WriteLine($"Read int Z: {results[2]}");
        }
    }


    // Requires libgpiod-dev
    // sudo apt install -y libgpiod-dev
    // gpiochip1 [tegra-gpio-aon] (40 lines)
    // gpiochip0: GPIOs 288-511, parent: platform/2200000.gpio, tegra-gpio:
    // gpiochip1: GPIOs 248-287, parent: platform/c2f0000.gpio, tegra-gpio-aon
    // 17 = (chip0) 288 +  
    // 17 = (chip1) 248 + 17 = 264
    // GPIO_AON SYS, AO, AO_HV, (All on VDD_RTC) AA, BB, CC, DD, EE
    public class Program
    {
        private static readonly byte boardBusId = 8;
        private static readonly byte boardAddress = 0x10;
        private static readonly byte MPU9250Address = 0x68;
        // Device may be missing/broken on board
        private static readonly byte AK8963Address = 0x0C;
        private static readonly byte BMP280 = 0x77;
        private static readonly uint pwmFrequency = 18000;

        public static void Main()
        {
            DeviceRegistryService DeviceRegistry = new();
            ServiceRegistry.AddService(DeviceRegistry);

            HardwareIODriver BoardIO = new(boardBusId, boardAddress);
            BoardIO.Init();

            Console.WriteLine($"Init Board Status: {BoardIO.LastOperationStatus}");


            BoardIO.SetPwmEnable();
            Console.WriteLine($"Set PWM Enable Board Status: {BoardIO.LastOperationStatus}");
            BoardIO.SetPwmFrequency(pwmFrequency);
            Console.WriteLine($"Set PWM Frequencey Board Status: {BoardIO.LastOperationStatus}");

            Console.WriteLine($"Register Drive Motor");
            IMotorDriver DriveMotor = new MDD10A(BoardIO, 265, HardwareIODriver.PwmChannelRegisters.Pwm2);
            Console.WriteLine($"Add Drive Motor To Registry");
            DeviceRegistry.AddDevice(DriveMotor);
            Console.WriteLine($"Register Turn Motor");
            IMotorDriver TurningMotor = new MDD10A(BoardIO, 264, HardwareIODriver.PwmChannelRegisters.Pwm1);
            Console.WriteLine($"Add Turn Motor To Registry");
            DeviceRegistry.AddDevice(TurningMotor);

            PDSGBGearboxMotorController motorController = new(DriveMotor, TurningMotor);
            Console.WriteLine($"Run Test:");
            motorController.Test();

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