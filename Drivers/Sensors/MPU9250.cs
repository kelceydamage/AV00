// Based on https://github.com/kriswiner/MPU9250/blob/master/STM32F401/MPU9250.h
using AV00.Drivers.IO;
using System.Device.I2c;

namespace AV00.Drivers.Sensors
{
    public class MPU9250
    {
        private enum AScale
        {
            AFS_2G = 0,
            AFS_4G,
            AFS_8G,
            AFS_16G
        };
        private enum GScale
        {
            GFS_250DPS = 0,
            GFS_500DPS,
            GFS_1000DPS,
            GFS_2000DPS
        };
        private readonly byte gyroscopeAddress = 0x43;
        private readonly byte accelerometerAddress = 0x3B;
        private readonly byte temperatureAddress = 0x41;
        private readonly byte powerManagementAddress = 0x6B;
        private readonly byte configAddress = 0x1A;
        private readonly byte gyroscopeConfigAddress = 0x1B;
        private readonly byte accelerometerScaleConfigAddress = 0x1C;
        private readonly byte accelerometerSampleConfigAddress = 0x1C;
        private readonly byte sampleRateDividerAddress = 0x19;
        private readonly byte interruptConfigAddress = 0x37;
        private readonly byte interruptEnableAddress = 0x38;
        private readonly byte[] gyroBuffer = new byte[6];
        private readonly byte[] accelBuffer = new byte[6];
        private readonly byte[] tempBuffer = new byte[2];
        // Select gyroscope scale
        private readonly byte gScale = (byte)GScale.GFS_250DPS;
        // Select accelerometer scale
        private readonly byte aScale = (byte)AScale.AFS_2G;
        private readonly I2cChannel i2CChannel;

        public MPU9250(int BusId, byte I2cAddress)
        {
            i2CChannel = new(new I2cConnectionSettings(BusId, I2cAddress));
        }

        // x|y|z accel register data returned
        public short[] ReadAccelerometer()
        {
            return I2cChannel.MergeMSBAndLSB(ReadSensor(accelerometerAddress, accelBuffer));
        }

        // x|y|z gyro register data returned
        public short[] ReadGyroscope()
        {
            return I2cChannel.MergeMSBAndLSB(ReadSensor(gyroscopeAddress, gyroBuffer));
        }

        // temperature register data returned
        public short ReadTemperature()
        {
            ReadSensor(temperatureAddress, tempBuffer);
            return (short)((tempBuffer[0] << 8) | tempBuffer[1]);
        }

        public Span<byte> ReadSensor(byte address, byte[] buffer)
        {
            i2CChannel.ReadBytes(address, buffer);
            return (Span<byte>)buffer;
        }

        public void Reset()
        {
            i2CChannel.WriteBytes(powerManagementAddress, new byte[] { 0x80 });
            Thread.Sleep(100);
        }

        public void Initialize()
        {
            // Wake up the MPU9250
            Reset();
            Thread.Sleep(100);
            // Get stable time source
            i2CChannel.WriteBytes(powerManagementAddress, new byte[] { 0x01 });
            // Disable FSYNC
            i2CChannel.WriteBytes(configAddress, new byte[] { 0x03 });
            // Set sample rate = gyroscope output rate/(1 + SMPLRT_DIV)
            i2CChannel.WriteBytes(sampleRateDividerAddress, new byte[] { 0x04 });
            // Set gyro full scale range
            byte[] readByteBuffer = new byte[1];
            i2CChannel.ReadBytes(gyroscopeConfigAddress, readByteBuffer);
            byte configRegister = readByteBuffer[0];
            configRegister = (byte)(configRegister & ~0x02);
            configRegister = (byte)(configRegister & ~0x18);
            configRegister = (byte)(configRegister | gScale << 3);
            i2CChannel.WriteBytes(gyroscopeConfigAddress, new byte[] { configRegister });
            // Set accelerometer full-scale range configuration
            i2CChannel.ReadBytes(accelerometerScaleConfigAddress, readByteBuffer);
            configRegister = readByteBuffer[0];
            configRegister = (byte)(configRegister & ~0x18);
            configRegister = (byte)(configRegister | aScale << 3);
            i2CChannel.WriteBytes(accelerometerScaleConfigAddress, new byte[] { configRegister });
            // Set accelerometer sample rate configuration
            i2CChannel.ReadBytes(accelerometerSampleConfigAddress, readByteBuffer);
            configRegister = readByteBuffer[0];
            // Clear accel_fchoice_b(bit 3) and A_DLPFG(bits[2:0])  
            configRegister = (byte)(configRegister & ~0x0F);
            // Set accelerometer rate to 1 kHz and bandwidth to 41 Hz
            configRegister = (byte)(configRegister | 0x03); 
            i2CChannel.WriteBytes(accelerometerSampleConfigAddress, new byte[] { configRegister });
            // The accelerometer, gyro, and thermometer are set to 1 kHz sample rates, 
            // but all these rates are further reduced by a factor of 5 to 200 Hz because of the
            // SMPLRT_DIV setting
            //
            // Configure Interrupts and Bypass Enable
            // Set interrupt pin active high, push-pull, and clear on read of INT_STATUS,
            // enable I2C_BYPASS_EN so additional chips can join the I2C bus and all can be controlled
            // by the Xavier as master
            i2CChannel.WriteBytes(interruptConfigAddress, new byte[] { 0x22 });
            i2CChannel.WriteBytes(interruptEnableAddress, new byte[] { 0x01 });
        }
    }
}
