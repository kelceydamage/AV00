using System.Device.I2c;
using static sensors_test.Drivers.ExpansionBoards.DFRIOExpansion;

namespace sensors_test.Drivers
{
    public class I2CDriver
    {
        protected readonly I2cDevice I2CDevice;
        public readonly I2cConnectionSettings I2CConnectionSettings;
        protected I2CDriver(I2cConnectionSettings Settings)
        {
            I2CConnectionSettings = Settings;
            I2CDevice = I2cDevice.Create(I2CConnectionSettings);
        }

        protected void WriteBytes(byte register, byte[] buffer)
        {
            I2CDevice.WriteByte(register);
            I2CDevice.Write(buffer);
        }

        protected void ReadBytes(byte register, byte[] buffer)
        {
            I2CDevice.WriteByte(register);
            I2CDevice.Read(buffer);
        }

        // Turn the MSB and LSB into a signed 16-bit value
        protected static short[] MergeMSBAndLSB(Span<byte> buffer)
        {
            short[] merged = new short[3];

            merged[0] = (short)((buffer[0] << 8) | buffer[1]);
            merged[1] = (short)((buffer[2] << 8) | buffer[3]);
            merged[2] = (short)((buffer[4] << 8) | buffer[5]);

            return merged;
        }
    }
}
