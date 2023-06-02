// See https://aka.ms/new-console-template for more information
using System.IO;
// https://github.com/dotnet/iot/blob/main/src/System.Device.Gpio/System/Device/I2c/I2cDevice.cs#L11
using System.Device.I2c;
using sensors_test.Drivers.Sensors;

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

    public class Program
    {
        private static readonly int busId = 8;
        private static readonly byte MPU9250Address = 0x68;
        // Device may be missing/broken on board
        private static readonly byte AK8963Address = 0x0C;
        private static readonly byte BMP280 = 0x77;

        public static void Main()
        {
            I2cConnectionSettings MPU9250Settings = new(busId, MPU9250Address);
            MPU9250 mpu9250 = new(MPU9250Settings);
            I2cConnectionSettings AK8963Settings = new(busId, AK8963Address);
            AK8963 ak8963 = new(AK8963Settings);
            short[] temp = mpu9250.ReadGyroscope();
            Console.WriteLine("Gyroscope:");
            DEBUG.DebugPrintResults(temp);
            temp = mpu9250.ReadAccelerometer();
            Console.WriteLine("Accelerometer:");
            DEBUG.DebugPrintResults(temp);
            temp = ak8963.ReadMagnetometer();
            DEBUG.DebugPrintResults(temp);
        }
    }
}