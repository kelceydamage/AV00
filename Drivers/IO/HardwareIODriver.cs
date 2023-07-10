// Based on https://github.com/DFRobot/DFRobot_RaspberryPi_Expansion_Board/blob/master/DFRobot_RaspberryPi_Expansion_Board.py
using System.Device.I2c;
using System.Device.Gpio;
using System.Net;
using System;

namespace sensors_test.Drivers.IO
{
    public class HardwareIODriver
    {
        private enum AnalogChannels : byte
        {
            A0 = 0x00,
            A1 = 0x01,
            A2 = 0x02,
            A3 = 0x03
        };
        public enum BoardStatus : byte
        {
            StatusOk = 0x00,
            StatusError = 0x01,
            StatusErrorDeviceNotDetected = 0x02,
            StatusErrorSoftwareVersion = 0x03,
            StatusErrorParameter = 0x04,
            StatusErrorUnableToRead = 0x05,
            StatusErrorUnableToWrite = 0x06,
        }
        public enum PwmChannelRegisters : byte
        {
            Pwm1 = 0x06,
            Pwm2 = 0x08,
            Pwm3 = 0x0a,
            Pwm4 = 0x0c
        };
        public enum AdcChannelRegisters : byte
        {
            Adc1 = 0x0f,
            Adc2 = 0x11,
            Adc3 = 0x13,
            Adc4 = 0x15
        };
        public PinMode GPIOPinMode;
        public const byte PwmChannelCount = 4;
        public const byte AdcChannelCount = 4;
        private const byte secondaryAddressRegister = 0x00;
        private const byte pidRegister = 0x01;
        private const byte vidRegister = 0x02;
        private const byte pwmControlRegister = 0x03;
        private const byte pwmFrequencyRegister = 0x04;
        public readonly byte[] AllPwmRegisters = new byte[]
        {
            (byte)PwmChannelRegisters.Pwm1,
            (byte)PwmChannelRegisters.Pwm2,
            (byte)PwmChannelRegisters.Pwm3,
            (byte)PwmChannelRegisters.Pwm4
        };
        private const byte adcControlRegister = 0x0e;
        public readonly byte[] AllAdcRegisters = new byte[]
        {
            (byte)AdcChannelRegisters.Adc1,
            (byte)AdcChannelRegisters.Adc2,
            (byte)AdcChannelRegisters.Adc3,
            (byte)AdcChannelRegisters.Adc4
        };
        private const byte defaultPidRegister = 0xdf;
        private const byte defaultVidRegister = 0x10;
        private const byte enableByte = 0x01;
        private const byte disableByte = 0x00;
        private bool isPwmEnabled = false;
        private byte address;
        public byte BusId;
        public uint CurrentFrequency;
        private BoardStatus lastOperationStatus = BoardStatus.StatusOk;
        private I2cChannelWrapper boardI2cChannel;
        public BoardStatus LastOperationStatus
        {
            get
            {
                return lastOperationStatus;
            }
        }
        public string ErrorMessage = "";

        public HardwareIODriver(byte I2CBusId, byte Address)
        {
            BusId = I2CBusId;
            I2cConnectionSettings settings = new(I2CBusId, Address);
            boardI2cChannel = new I2cChannelWrapper(settings);
        }

        public I2cChannelWrapper CreateI2cChannelInstance(int I2cAddress)
        {
            I2cConnectionSettings Settings = new(boardI2cChannel.I2cConnectionSettings.BusId, I2cAddress);
            return new I2cChannelWrapper(Settings);
        }

        public BoardStatus Init()
        {
            byte[] pidBuffer = new byte[1];
            ReadBytes(pidRegister, pidBuffer);
            byte[] vidBuffer = new byte[1];
            ReadBytes(vidRegister, vidBuffer);
            
            if (lastOperationStatus == BoardStatus.StatusOk)
            {
                if (pidBuffer[0] != defaultPidRegister)
                {
                    lastOperationStatus = BoardStatus.StatusErrorDeviceNotDetected;
                    ErrorMessage = $"PID: {pidBuffer[0]}, defaultPidRegister: {defaultPidRegister}";
                }
                else if (vidBuffer[0] != defaultVidRegister)
                {

                    lastOperationStatus = BoardStatus.StatusErrorSoftwareVersion;
                    ErrorMessage = $"VID: {vidBuffer[0]}, defaultVidRegister: {defaultVidRegister}";
                }
                else
                {
                    SetPwmDisable();
                    SetPwmDutyCycle(AllPwmRegisters, 0);
                    SetAdcDisable();  
                }
            }
            return lastOperationStatus;
        }

        public void WriteBytes(byte register, byte[] buffer)
        {
            try
            {
                boardI2cChannel.WriteBytes(register, buffer);
                lastOperationStatus = BoardStatus.StatusOk;
            }
            catch (Exception)
            {
                lastOperationStatus = BoardStatus.StatusErrorUnableToWrite;
            }
        }

        public void ReadBytes(byte register, byte[] buffer)
        {
            try
            {
                boardI2cChannel.ReadBytes(register, buffer);
                lastOperationStatus = BoardStatus.StatusOk;
            }
            catch (Exception)
            {
                lastOperationStatus = BoardStatus.StatusErrorUnableToRead;
            }
        }

        public void SetAddress(byte Address)
        {
            if (address < 1 || address > 127)
            {
                lastOperationStatus = BoardStatus.StatusErrorParameter;
            }
            else
            {
                byte[] buffer = new byte[1];
                buffer[0] = Address;
                WriteBytes(secondaryAddressRegister, buffer);
            }
        }

        public void SetPwmDisable()
        {
            byte[] buffer = new byte[1];
            buffer[0] = disableByte;
            WriteBytes(pwmControlRegister, buffer);
            if (lastOperationStatus == BoardStatus.StatusOk)
            {
                isPwmEnabled = false;
            }
        }

        public void SetPwmEnable()
        {
            byte[] buffer = new byte[1];
            buffer[0] = enableByte;
            WriteBytes(pwmControlRegister, buffer);
            if (lastOperationStatus == BoardStatus.StatusOk)
            {
                isPwmEnabled = true;
            }
        }

        public void SetPwmFrequency(uint frequency)
        {
            if (frequency < 1 || frequency > 48000)
            {
                lastOperationStatus = BoardStatus.StatusErrorParameter;
            }
            else
            {
                bool pwmPreviousFlag = isPwmEnabled;
                SetPwmDisable();
                byte[] buffer = new byte[2];
                buffer[0] = (byte)(frequency >> 8);
                buffer[1] = (byte)(frequency & 0xff);
                WriteBytes(pwmFrequencyRegister, buffer);
                Thread.Sleep(100);
                if (pwmPreviousFlag)
                {
                    SetPwmEnable();
                }
                CurrentFrequency = frequency;
            }
        }

        public byte SetPwmDutyCycle(byte channelId, byte Duty)
        {
            if (Duty < 0 || Duty > 100)
            {
                lastOperationStatus = BoardStatus.StatusErrorParameter;
            }
            byte[] buffer = new byte[2];
            buffer[0] = Duty;
            buffer[1] = (byte)(Duty * 10 % 10);
            WriteBytes(channelId, buffer);
            return Duty;
        }

        public void SetPwmDutyCycle(byte[] channelId, byte Duty)
        {
            foreach (byte id in channelId)
            {
                SetPwmDutyCycle(id, Duty);
            }
        }

        public void SetAdcEnable()
        {
            byte[] buffer = new byte[1];
            buffer[0] = enableByte;
            WriteBytes(adcControlRegister, buffer);
        }

        public void SetAdcDisable()
        {
            byte[] buffer = new byte[1];
            buffer[0] = disableByte;
            WriteBytes(adcControlRegister, buffer);
        }

        public int GetAdcValue(byte channelId)
        {
            byte[] buffer = new byte[2];
            ReadBytes(channelId, buffer);
            if (lastOperationStatus == BoardStatus.StatusOk)
            {
                return (buffer[0] << 8) | buffer[1];
            }
            return 0;
        }

        public List<int> GetAdcValue(byte[] channelId)
        {
            List<int> results = new();
            foreach (byte id in channelId)
            {
                int result = GetAdcValue(id);
                results.Add(result);
            }
            return results;
        }

        public string[] DetectAddress()
        {
            byte[] buffer = new byte[1];
            byte oldAddress = address;
            for (byte i = 1; i < 127; i++)
            {
                I2cConnectionSettings _settings = new(BusId, i);
                boardI2cChannel = new I2cChannelWrapper(_settings);
                if (Init() == BoardStatus.StatusOk)
                {
                    buffer[0] = i;
                    Console.WriteLine($"Found Initial Address: {i}");
                }
            }
            string[] validAddresses = new string[buffer.Length];
            for (byte i = 0; i < buffer.Length; i++)
            {
                byte[] bytes = new byte[1];
                bytes[0] = buffer[i];
                validAddresses[i] = Convert.ToHexString(bytes);
                Console.WriteLine($"Found Address: {validAddresses[i]}");
            }
            address = oldAddress;
            I2cConnectionSettings settings = new(BusId, address);
            boardI2cChannel = new I2cChannelWrapper(settings);
            lastOperationStatus = BoardStatus.StatusOk;
            return validAddresses;
        }
    }

    public class I2cChannelWrapper
    {
        protected readonly I2cDevice I2cChannel;
        public readonly I2cConnectionSettings I2cConnectionSettings;
        public I2cChannelWrapper(I2cConnectionSettings Settings)
        {
            I2cConnectionSettings = Settings;
            I2cChannel = I2cDevice.Create(I2cConnectionSettings);
        }

        public void WriteBytes(byte register, byte[] buffer)
        {
            I2cChannel.WriteByte(register);
            I2cChannel.Write(buffer);
        }

        public void ReadBytes(byte register, byte[] buffer)
        {
            I2cChannel.WriteByte(register);
            I2cChannel.Read((Span<byte>)buffer);
        }

        // Turn the MSB and LSB into a signed 16-bit value
        public short[] MergeMSBAndLSB(Span<byte> buffer)
        {
            short[] merged = new short[3];

            merged[0] = (short)((buffer[0] << 8) | buffer[1]);
            merged[1] = (short)((buffer[2] << 8) | buffer[3]);
            merged[2] = (short)((buffer[4] << 8) | buffer[5]);

            return merged;
        }
    }
}
