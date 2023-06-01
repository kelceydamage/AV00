// See https://aka.ms/new-console-template for more information
using System.IO;
// https://github.com/dotnet/iot/blob/main/src/System.Device.Gpio/System/Device/I2c/I2cDevice.cs#L11
using System.Device.I2c;

namespace sensors_test
{
    public class Program
    {
        private static readonly int busId = 8;
        private static readonly int deviceAddress = 0x53;

        public static void Main()
        {
            I2cConnectionSettings I2CConnectionSettings = new(busId, deviceAddress);
            I2cDevice I2CDevice = I2cDevice.Create(I2CConnectionSettings);
            Console.WriteLine($"Test: {I2CDevice.ConnectionSettings.DeviceAddress}, {I2CDevice.ConnectionSettings.BusId}");
            byte _junk = I2CDevice.ReadByte();
            Console.WriteLine($"Junk Byte: {_junk}");
        }
    }
}