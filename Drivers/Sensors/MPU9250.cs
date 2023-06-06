// Based on https://github.com/kriswiner/MPU9250/blob/master/STM32F401/MPU9250.h
using System.Device.I2c;

namespace sensors_test.Drivers.Sensors
{
    public class MPU9250 : I2CDeviceDriver
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

        public MPU9250(I2cConnectionSettings Settings) : base(Settings) { }

        // x|y|z accel register data returned
        public short[] ReadAccelerometer()
        {
            return MergeMSBAndLSB(ReadSensor(accelerometerAddress, accelBuffer));
        }

        // x|y|z gyro register data returned
        public short[] ReadGyroscope()
        {
            return MergeMSBAndLSB(ReadSensor(gyroscopeAddress, gyroBuffer));
        }

        // temperature register data returned
        public short ReadTemperature()
        {
            ReadSensor(temperatureAddress, tempBuffer);
            return (short)((tempBuffer[0] << 8) | tempBuffer[1]);
        }

        public Span<byte> ReadSensor(byte address, byte[] buffer)
        {
            Span<byte> sensorBuffer = buffer;
            I2CDevice.WriteByte(address);
            I2CDevice.Read(sensorBuffer);
            return sensorBuffer;
        }

        public void Reset()
        {             
            I2CDevice.WriteByte(powerManagementAddress);
            I2CDevice.WriteByte(0x80);
            Thread.Sleep(100);
        }

        public void Initialize()
        {
            // Wake up the MPU9250
            I2CDevice.WriteByte(powerManagementAddress);
            I2CDevice.WriteByte(0x00);
            Thread.Sleep(100);
            // Get stable time source
            I2CDevice.WriteByte(powerManagementAddress);
            I2CDevice.WriteByte(0x01);
            // Disable FSYNC
            I2CDevice.WriteByte(configAddress);
            I2CDevice.WriteByte(0x03);
            // Set sample rate = gyroscope output rate/(1 + SMPLRT_DIV)
            I2CDevice.WriteByte(sampleRateDividerAddress);
            I2CDevice.WriteByte(0x04);
            // Set gyro full scale range
            I2CDevice.WriteByte(gyroscopeConfigAddress);
            byte configRegister = I2CDevice.ReadByte();
            configRegister = (byte)(configRegister & ~0x02);
            configRegister = (byte)(configRegister & ~0x18);
            configRegister = (byte)(configRegister | gScale << 3);
            I2CDevice.WriteByte(gyroscopeConfigAddress);
            I2CDevice.WriteByte(configRegister);
            // Set accelerometer full-scale range configuration
            I2CDevice.WriteByte(accelerometerScaleConfigAddress);
            configRegister = I2CDevice.ReadByte();
            configRegister = (byte)(configRegister & ~0x18);
            configRegister = (byte)(configRegister | aScale << 3);
            I2CDevice.WriteByte(accelerometerScaleConfigAddress);
            I2CDevice.WriteByte(configRegister);
            // Set accelerometer sample rate configuration
            I2CDevice.WriteByte(accelerometerSampleConfigAddress);
            configRegister = I2CDevice.ReadByte();
            // Clear accel_fchoice_b(bit 3) and A_DLPFG(bits[2:0])  
            configRegister = (byte)(configRegister & ~0x0F);
            // Set accelerometer rate to 1 kHz and bandwidth to 41 Hz
            configRegister = (byte)(configRegister | 0x03); 
            I2CDevice.WriteByte(accelerometerSampleConfigAddress);
            I2CDevice.WriteByte(configRegister);
            // The accelerometer, gyro, and thermometer are set to 1 kHz sample rates, 
            // but all these rates are further reduced by a factor of 5 to 200 Hz because of the
            // SMPLRT_DIV setting
            //
            // Configure Interrupts and Bypass Enable
            // Set interrupt pin active high, push-pull, and clear on read of INT_STATUS,
            // enable I2C_BYPASS_EN so additional chips can join the I2C bus and all can be controlled
            // by the Xavier as master
            I2CDevice.WriteByte(interruptConfigAddress);
            I2CDevice.WriteByte(0x22);
            I2CDevice.WriteByte(interruptEnableAddress);
            I2CDevice.WriteByte(0x01);
        }
    }
}
