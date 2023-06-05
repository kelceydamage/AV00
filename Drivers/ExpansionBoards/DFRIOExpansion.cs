// Based on https://github.com/DFRobot/DFRobot_RaspberryPi_Expansion_Board/blob/master/DFRobot_RaspberryPi_Expansion_Board.py
using System.Device.I2c;
//using sensors_test.Drivers;

namespace sensors_test.Drivers.ExpansionBoards
{
    public class DFRIOExpansion : I2CDriver
    {
        private enum AnalogChannels
        {
            A0 = 0x00,
            A1 = 0x01,
            A2 = 0x02,
            A3 = 0x03
        };
        public enum BoardStatus
        {
            StatusOk = 0x00,
            StatusError = 0x01,
            StatusErrorDeviceNotDetected = 0x02,
            StatusErrorSoftwareVersion = 0x03,
            StatusErrorParameter = 0x04,
        }
        public const byte PwmChannelCount = 4;
        public const byte AdcChannelCount = 4;
        private const byte secondaryAddressRegister = 0x00;
        private const byte pidRegister = 0x01;
        private const byte vidRegister = 0x02;
        private const byte pwmControlRegister = 0x03;
        private const byte pwmFrequencyRegister = 0x04;
        public const byte PwmDuty1Register = 0x06;
        public const byte PwmDuty2Register = 0x08;
        public const byte PwmDuty3Register = 0x0a;
        public const byte PwmDuty4Register = 0x0c;
        public readonly byte[] AllPwmRegisters = new byte[]
        {
            PwmDuty1Register,
            PwmDuty2Register,
            PwmDuty3Register,
            PwmDuty4Register
        };
        private const byte adcControlRegister = 0x0e;
        public const byte AdcValue1Register = 0x0f;
        public const byte AdcValue2Register = 0x11;
        public const byte AdcValue3Register = 0x13;
        public const byte AdcValue4Register = 0x15;
        public readonly byte[] AllAdcRegisters = new byte[]
        {
            AdcValue1Register,
            AdcValue2Register,
            AdcValue3Register,
            AdcValue4Register
        };
        private const byte defaultPidRegister = 0xdf;
        private const byte defaultVidRegister = 0x10;
        private const byte enableByte = 0x01;
        private const byte disableByte = 0x00;
        private bool isPwmEnabled = false;
        private byte address;
        private BoardStatus lastOperationStatus = BoardStatus.StatusOk;
        public BoardStatus LastOperationStatus { 
            get
            {
                return lastOperationStatus;
            }
        }

        // Bus 8, Address 0x10
        public DFRIOExpansion(I2cConnectionSettings Settings) : base(Settings)
        {
            isPwmEnabled = false;
        }

        public BoardStatus Start()
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
                }
                else if (vidBuffer[0] != defaultVidRegister)
                {
                    lastOperationStatus = BoardStatus.StatusErrorSoftwareVersion;
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

        public new void WriteBytes(byte register, byte[] buffer)
        {
            try
            {
                WriteBytes(register, buffer);
                lastOperationStatus = BoardStatus.StatusOk;
            }
            catch (Exception)
            {
                lastOperationStatus = BoardStatus.StatusErrorDeviceNotDetected;
            }
        }

        public new void ReadBytes(byte register, byte[] buffer)
        {
            try
            {
                ReadBytes(register, buffer);
                lastOperationStatus = BoardStatus.StatusOk;
            }
            catch (Exception)
            {
                lastOperationStatus = BoardStatus.StatusErrorDeviceNotDetected;
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
            if (frequency < 1 || frequency > 1000)
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
            }
        }

        public void SetPwmDutyCycle(byte channelId, short Duty)
        {
            if (Duty < 0 || Duty > 100)
            {
                lastOperationStatus = BoardStatus.StatusErrorParameter;
            }
            byte[] buffer = new byte[2];
            buffer[0] = (byte)Duty;
            buffer[1] = (byte)(Duty * 10 % 10);
            WriteBytes(channelId, buffer);
        }

        public void SetPwmDutyCycle(byte[] channelId, short Duty)
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
                address = i;
                if (Start() == BoardStatus.StatusOk)
                {
                    buffer[0] = address;
                }
            }
            string[] validAddresses = new string[buffer.Length];
            for (byte i = 0; i < buffer.Length; i++)
            {
                byte[] bytes = new byte[1];
                bytes[0] = buffer[i];
                validAddresses[i] = Convert.ToHexString(bytes);
            }
            address = oldAddress;
            lastOperationStatus = BoardStatus.StatusOk;
            return validAddresses;
        }
    }
}
