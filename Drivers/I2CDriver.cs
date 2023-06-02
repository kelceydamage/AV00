using System.Device.I2c;

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
