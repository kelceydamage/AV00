using AV00.Drivers.IO;
using System.Device.I2c;

namespace AV00.Drivers.ExpansionBoards
{
    internal class DFR0604 : IPwmGenerator
    {
        private enum EnumAnalogChannels : byte
        {
            A0 = 0x00,
            A1 = 0x01,
            A2 = 0x02,
            A3 = 0x03
        };
        public enum EnumBoardStatus : byte
        {
            StatusOk = 0x00,
            StatusError = 0x01,
            StatusErrorDeviceNotDetected = 0x02,
            StatusErrorSoftwareVersion = 0x03,
            StatusErrorParameter = 0x04,
            StatusErrorUnableToRead = 0x05,
            StatusErrorUnableToWrite = 0x06,
        }
        public enum EnumPwmChannelRegisters : byte
        {
            Pwm1 = 0x06,
            Pwm2 = 0x08,
            Pwm3 = 0x0a,
            Pwm4 = 0x0c
        };
        public enum EnumAdcChannelRegisters : byte
        {
            Adc1 = 0x0f,
            Adc2 = 0x11,
            Adc3 = 0x13,
            Adc4 = 0x15
        };
        public const byte AdcChannelCount = 4;
        private const byte secondaryAddressRegister = 0x00;
        private const byte pidRegister = 0x01;
        private const byte vidRegister = 0x02;
        private const byte pwmControlRegister = 0x03;
        private const byte pwmFrequencyRegister = 0x04;
        private const byte adcControlRegister = 0x0e;
        private const byte defaultPidRegister = 0xdf;
        private const byte defaultVidRegister = 0x10;
        private const byte enableByte = 0x01;
        private const byte disableByte = 0x00;
        const byte i2cAddress = 0x10;
        const int defaultChannelCount = 4;
        static readonly int minChannel = 0;
        private readonly int channelCount;
        private readonly int maxChannel;
        private readonly I2cChannel i2c;
        public string ErrorMessage = "";
        private byte address = i2cAddress;

        public byte BusId { get => busId; }
        private readonly byte busId;
        public int CurrentFrequency { get => currentFrequency; }
        private int currentFrequency = pwmMinFrequencyHz;
        public bool IsPwmEnabled { get => isPwmEnabled; }
        private bool isPwmEnabled = false;
        public EnumBoardStatus LastOperationStatus { get => lastOperationStatus; }
        private EnumBoardStatus lastOperationStatus = EnumBoardStatus.StatusOk;
        public string Name { get => name; set { } }
        private const string name = "DFR0604";
        public int PwmMaxFrequencyHz { get => pwmMaxFrequencyHz; }
        private const int pwmMaxFrequencyHz = 48000;
        public int PwmMinFrequencyHz { get => pwmMinFrequencyHz; }
        private const int pwmMinFrequencyHz = 500;
        public int PwmBitDepth { get => pwmBitDepth; }
        private const int pwmBitDepth = 8; // Default, max resolution is 16 bit.
        public int PwmChannelCount { get => pwmChannelCount; }
        private const int pwmChannelCount = 4;
        public float PwmMaxValue { get => pwmMaxValue; }
        private const int pwmMaxValue = 100;

        public DFR0604(byte I2cBus, int ChannelCount = 4)
        {
            busId = I2cBus;
            I2cConnectionSettings I2cSettings = new(I2cBus, i2cAddress);
            i2c = new I2cChannel(I2cSettings);
            if (ChannelCount < 1 || ChannelCount > defaultChannelCount)
            {
                throw new Exception("ChannelCount must be between 1 and 4");
            }
            channelCount = ChannelCount;
            maxChannel = channelCount - 1;
        }

        public void Reset()
        {
            throw new Exception("Reset() not yet implemented");
        }

        public EnumBoardStatus Init()
        {
            byte[] pidBuffer = new byte[1];
            ReadBytes(pidRegister, pidBuffer);

            byte[] vidBuffer = new byte[1];
            ReadBytes(vidRegister, vidBuffer);

            if (lastOperationStatus == EnumBoardStatus.StatusOk)
            {
                if (pidBuffer[0] != defaultPidRegister)
                {
                    lastOperationStatus = EnumBoardStatus.StatusErrorDeviceNotDetected;
                    ErrorMessage = $"PID: {pidBuffer[0]}, defaultPidRegister: {defaultPidRegister}";
                }
                else if (vidBuffer[0] != defaultVidRegister)
                {

                    lastOperationStatus = EnumBoardStatus.StatusErrorSoftwareVersion;
                    ErrorMessage = $"VID: {vidBuffer[0]}, defaultVidRegister: {defaultVidRegister}";
                }
                else
                {
                    SetPwmDisable();
                    SetChannelPwmAll((byte[])Enum.GetValues(typeof(EnumPwmChannelRegisters)), 0);
                    SetAdcDisable();
                }
            }
            return lastOperationStatus;
        }

        public void WriteBytes(byte register, byte[] buffer)
        {
            try
            {
                i2c.WriteBytes(register, buffer);
                lastOperationStatus = EnumBoardStatus.StatusOk;
            }
            catch (Exception)
            {
                lastOperationStatus = EnumBoardStatus.StatusErrorUnableToWrite;
            }
        }

        public void ReadBytes(byte register, byte[] buffer)
        {
            try
            {
                i2c.ReadBytes(register, buffer);
                lastOperationStatus = EnumBoardStatus.StatusOk;
            }
            catch (Exception)
            {
                lastOperationStatus = EnumBoardStatus.StatusErrorUnableToRead;
            }
        }

        public void SetAddress(byte Address)
        {
            if (address < 1 || address > 127)
            {
                lastOperationStatus = EnumBoardStatus.StatusErrorParameter;
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
            if (lastOperationStatus == EnumBoardStatus.StatusOk)
            {
                isPwmEnabled = false;
            }
            Thread.Sleep(10);
        }

        public void SetPwmEnable()
        {
            byte[] buffer = new byte[1];
            buffer[0] = enableByte;
            WriteBytes(pwmControlRegister, buffer);
            if (lastOperationStatus == EnumBoardStatus.StatusOk)
            {
                isPwmEnabled = true;
            }
            Thread.Sleep(10);
        }

        public void SetFrequency(int frequency)
        {
            if (frequency < 1 || frequency > 48000)
            {
                lastOperationStatus = EnumBoardStatus.StatusErrorParameter;
            }
            else
            {
                bool pwmPreviousFlag = isPwmEnabled;
                SetPwmDisable();
                byte[] buffer = new byte[2];
                buffer[0] = (byte)(frequency >> 8);
                buffer[1] = (byte)(frequency & 0xff);
                Console.WriteLine($"Setting PWM Frequencey buffer[0]: {buffer[0]}, buffer[1]: {buffer[1]}");
                WriteBytes(pwmFrequencyRegister, buffer);
                Thread.Sleep(10);
                if (pwmPreviousFlag)
                {
                    SetPwmEnable();
                }
                currentFrequency = frequency;
            }
        }

        private void ValidateChannelId(int ChannelId)
        {
            if (ChannelId < minChannel || ChannelId > maxChannel)
            {
                throw new Exception($"Channel must be between {minChannel} and {maxChannel} inclusive");
            }
        }

        // NOTE: PwmAmount is a percentage for this hardware device
        public void SetChannelPwm(int channelId, ushort PwmAmount)
        {
            ValidateChannelId(channelId);
            byte pwmAmount = (byte)PwmAmount;
            if (PwmAmount < 0 || PwmAmount > 100)
            {
                lastOperationStatus = EnumBoardStatus.StatusErrorParameter;
                Console.WriteLine($"PwmAmount is a percentage for this hardware device, please set to between 0 - 100. Attempted Value: {PwmAmount}");
            }
            else
            {
                byte[] buffer = new byte[2];
                buffer[0] = pwmAmount;
                buffer[1] = (byte)(pwmAmount * 10 % 10);
                WriteBytes((byte)channelId, buffer);
            }
        }

        public void SetChannelPwmAll(byte[] channelId, byte Duty)
        {
            Console.WriteLine("Setting PWM Duty Cycle From List");
            foreach (byte id in channelId)
            {
                SetChannelPwm(id, Duty);
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
            if (lastOperationStatus == EnumBoardStatus.StatusOk)
            {
                return buffer[0] << 8 | buffer[1];
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
    }
}
