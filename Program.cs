// See https://aka.ms/new-console-template for more information
using System.IO;
// https://github.com/dotnet/iot/blob/main/src/System.Device.Gpio/System/Device/I2c/I2cDevice.cs#L11
using System.Device.I2c;

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

    public class I2CSensor
    {
        protected readonly I2cDevice I2CDevice;
        public readonly I2cConnectionSettings I2CConnectionSettings;
        protected I2CSensor(I2cConnectionSettings Settings)
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

    public class AK8963: I2CSensor
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

    public class MPU9250: I2CSensor
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


    public class Program
    {
        private static readonly int busId = 8;
        private static readonly byte MPU9250Address = 0x68;
        private static readonly byte AK8963Address = 0x0C;

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
            Console.WriteLine("Magnetometer:");
            DEBUG.DebugPrintResults(temp);
        }
    }
}