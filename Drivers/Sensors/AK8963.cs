using sensors_test.Drivers.IO;
using System.Collections.Generic;
using System.Device.I2c;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace sensors_test.Drivers.Sensors
{
    public class AK8963
    {
        enum MScale
        {
            MFS_14BITS = 0, // 0.6 mG per LSB
            MFS_16BITS      // 0.15 mG per LSB
        };
        private readonly byte magnetometerAddress = 0x03;
        private readonly byte magnetometerModeAddress = 0x06;
        private readonly byte AK8963st1Address = 0x02;
        private readonly byte AK8963ControlAddress = 0x0A;
        private readonly byte AK8963AxisSensitivityAdjustmentXAddress = 0x10;
        private readonly byte mScale = (byte)MScale.MFS_16BITS;
        private readonly I2cChannelWrapper i2CChannel;

        public AK8963(HardwareIODriver IODriver, byte I2cAddress)
        {
            i2CChannel = IODriver.CreateI2cChannelInstance(I2cAddress);
        }

        public short[] ReadMagnetometer()
        {
            short[] merged = new short[3];
            byte[] sensorBuffer = new byte[7];
            byte[] readByteBuffer = new byte[1];
            i2CChannel.ReadBytes(AK8963st1Address, readByteBuffer);
            byte readyBit = readByteBuffer[0];
            Console.WriteLine($"ReadyBit {readyBit}");
            Console.WriteLine($"ReadyBit {(readyBit & 0x01)}");
            if ((readyBit & 0x01) == 1)
            {
                i2CChannel.ReadBytes(magnetometerAddress, sensorBuffer);
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

        public void Initialize()
        {
           
            byte[] buffer = new byte[3];
            float[] fBuffer = new float[3];
            // Power down magnetometer  
            i2CChannel.WriteBytes(AK8963ControlAddress, new byte[] { 0x00 });
            Thread.Sleep(100);
            // Enter Fuse ROM access mode
            i2CChannel.WriteBytes(AK8963ControlAddress, new byte[] { 0x0F });
            Thread.Sleep(100);
            // Optional: Return x-axis sensitivity adjustment values, etc.
            i2CChannel.ReadBytes(AK8963AxisSensitivityAdjustmentXAddress, buffer);
            fBuffer[0] = (float)(buffer[0] - 128) / 256.0f + 1.0f;   // Return x-axis sensitivity adjustment values, etc.
            fBuffer[1] = (float)(buffer[1] - 128) / 256.0f + 1.0f;
            fBuffer[2] = (float)(buffer[2] - 128) / 256.0f + 1.0f;
            // Power down magnetometer 
            i2CChannel.WriteBytes(AK8963ControlAddress, new byte[] { 0x00 });
            // Configure the magnetometer for continuous read and highest resolution
            // set Mscale bit 4 to 1 (0) to enable 16 (14) bit resolution in CNTL register,
            // and enable continuous mode data acquisition Mmode (bits [3:0]), 0010 for 8 Hz and 0110 for 100 Hz sample rates
            // Set magnetometer data resolution and sample ODR
            i2CChannel.WriteBytes(AK8963ControlAddress, new byte[] { (byte)(mScale << 4 | magnetometerModeAddress) });
            Thread.Sleep(100);
        }
    }
}
