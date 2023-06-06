// See https://aka.ms/new-console-template for more information
using System.IO;
// https://github.com/dotnet/iot/blob/main/src/System.Device.Gpio/System/Device/I2c/I2cDevice.cs#L11
using System.Device.I2c;
using sensors_test.Drivers.Sensors;
using sensors_test.Services;
using sensors_test.Drivers.IO;
using sensors_test.Drivers;
using sensors_test.Drivers.Motors;
using sensors_test.Controllers.MotorController;

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

            IMotorDriver DriveMotor = new MDD10A(BoardIO, 18, HardwareIODriver.PwmChannelRegisters.Pwm1);
            DeviceRegistry.AddDevice(DriveMotor);
            IMotorDriver TurningMotor = new MDD10A(BoardIO, 17, HardwareIODriver.PwmChannelRegisters.Pwm2);
            DeviceRegistry.AddDevice(TurningMotor);

            PDSGBGearboxMotorController motorController = new(DriveMotor, TurningMotor);
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