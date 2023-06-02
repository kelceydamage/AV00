using System.Device.I2c;

namespace sensors_test.Drivers.Sensors
{
    public class MPU9250 : I2CDriver
    {
        private readonly byte gyroscopeAddress = 0x43;
        private readonly byte accelerometerAddress = 0x3B;

        public MPU9250(I2cConnectionSettings Settings) : base(Settings) { }

        // x|y|z accel register data returned
        public short[] ReadAccelerometer()
        {
            return ReadSensor(accelerometerAddress);
        }

        // x|y|z gyro register data returned
        public short[] ReadGyroscope()
        {
            return ReadSensor(gyroscopeAddress);
        }

        public short[] ReadSensor(byte address)
        {
            Span<byte> sensorBuffer = new byte[6];
            I2CDevice.WriteByte(address);
            I2CDevice.Read(sensorBuffer);
            return MergeMSBAndLSB(sensorBuffer);
        }
    }
}
