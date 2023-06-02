using System.Device.I2c;

namespace sensors_test.Drivers.Sensors
{
    public class AK8963 : I2CDriver
    {
        private readonly byte magnetometerAddress = 0x03;
        private readonly byte AK8963st1Address = 0x02;
        public AK8963(I2cConnectionSettings Settings) : base(Settings) { }

        public short[] ReadMagnetometer()
        {
            short[] merged = new short[3];
            Span<byte> sensorBuffer = new byte[7];
            I2CDevice.WriteByte(AK8963st1Address);
            byte readyBit = I2CDevice.ReadByte();
            Console.WriteLine($"ReadyBit {readyBit}");
            Console.WriteLine($"ReadyBit {(readyBit & 0x01)}");
            if ((readyBit & 0x01) == 1)
            {
                I2CDevice.WriteByte(magnetometerAddress);
                I2CDevice.Read(sensorBuffer);
                uint st2Register = sensorBuffer[6];
                Console.WriteLine($"st2Register {st2Register}");
                Console.WriteLine($"st2Register {(st2Register & 0x08)}");
                if ((st2Register & 0x08) != 1)
                {
                    // Turn the MSB and LSB into a signed 16-bit value
                    merged[0] = (short)((sensorBuffer[1] << 8) | sensorBuffer[0]);
                    // Data stored as little Endian
                    merged[1] = (short)((sensorBuffer[3] << 8) | sensorBuffer[2]);
                    merged[2] = (short)((sensorBuffer[5] << 8) | sensorBuffer[4]);
                }
            }
            return merged;
        }
    }
}
