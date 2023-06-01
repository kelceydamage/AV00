// See https://aka.ms/new-console-template for more information
using System.IO;
using System.Device.I2c;

namespace sensors_test
{
    public class Program
    {
        private static readonly int busId = 8;
        private static readonly int deviceAddress = 0x1c;

        public static void Main()
        {
            I2cConnectionSettings I2CConnectionSettings = new(busId, deviceAddress);
            I2cDevice I2CDevice = I2cDevice.Create(I2CConnectionSettings);
            Console.WriteLine("Hello, World!");
        }
    }
}