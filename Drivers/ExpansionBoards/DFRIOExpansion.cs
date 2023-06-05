// Based on https://github.com/DFRobot/DFRobot_RaspberryPi_Expansion_Board/blob/master/DFRobot_RaspberryPi_Expansion_Board.py

namespace sensors_test.Drivers.ExpansionBoards
{
    internal class DFRIOExpansion
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
            statusOk = 0x00,
            statusError = 0x01,
            statusErrorDeviceNotDetected = 0x02,
            statusErrorSoftwareVersion = 0x03,
            statusErrorParameter = 0x04,
        }
        private const byte pwmChannelCount = 4;
        private const byte adcChannelCount = 4;
        private const byte secondaryAddressRegister = 0x00;
        private const byte pidRegister = 0x01;
        private const byte vidRegister = 0x02;
        private const byte pwmControlRegister = 0x03;
        private const byte pwmFrequencyRegister = 0x04;
        private const byte pwmDuty1Register = 0x06;
        private const byte pwmDuty2Register = 0x08;
        private const byte pwmDuty3Register = 0x0a;
        private const byte pwmDuty4Register = 0x0c;
        private readonly byte[] allPwmRegisters = new byte[]
        { 
            pwmDuty1Register,
            pwmDuty2Register,
            pwmDuty3Register,
            pwmDuty4Register 
        };
        private const byte adcControlRegister = 0x0e;
        private const byte adcValue1Register = 0x0f;
        private const byte adcValue2Register = 0x11;
        private const byte adcValue3Register = 0x13;
        private const byte adcValue4Register = 0x15;
        private readonly byte[] allAdcRegisters = new byte[]
        { 
            adcValue1Register,
            adcValue2Register,
            adcValue3Register,
            adcValue4Register 
        };
        private const byte defaultPidRegister = 0xdf;
        private const byte defaultVidRegister = 0x10;
        private const byte enableByte = 0x01;
        private const byte disableByte = 0x00;
        private bool isPwmEnabled = false;
        private byte address;
        private BoardStatus lastOperationStatus = BoardStatus.statusOk;

        public DFRIOExpansion(byte Address)
        {
            address = Address;
            isPwmEnabled = false;
        }

        public BoardStatus Start()
        {
            byte[] pidBuffer = new byte[1];
            readBytes(pidRegister, pidBuffer, 1);
            byte[] vidBuffer = new byte[1];
            readBytes(pidRegister, vidBuffer, 1);
            if (lastOperationStatus == BoardStatus.statusOk)
            {
                if (pidBuffer[0] != defaultPidRegister)
                {
                    lastOperationStatus = BoardStatus.statusErrorDeviceNotDetected;
                }
                else if (vidBuffer[0] != defaultVidRegister)
                {
                    lastOperationStatus = BoardStatus.statusErrorSoftwareVersion;
                }
                else
                {
                    SetPwmDisable();
                    SetPwmDutyCycle(allPwmRegisters, 0);
                    SetAdcDisable();
                }
            }
            return lastOperationStatus;
        }

        public virtual byte[] writeBytes(byte register, byte[] buffer)
        {
            return buffer;
        }

        public virtual byte[] readBytes(byte register, byte[] buffer, int length)
        {
            return buffer;
        }

        public void SetAddress(byte Address)
        {
            if (address < 1 || address > 127)
            {
                lastOperationStatus = BoardStatus.statusErrorParameter;
            }
            else
            {
                byte[] buffer = new byte[1];
                buffer[0] = Address;
                writeBytes(secondaryAddressRegister, buffer);
            }
        }

        public void SetPwmDisable()
        {
            byte[] buffer = new byte[1];
            buffer[0] = disableByte;
            writeBytes(pwmControlRegister, buffer);
            if (lastOperationStatus == BoardStatus.statusOk)
            {
                isPwmEnabled = false;
            }
        }

        public void SetPwmEnable()
        {
            byte[] buffer = new byte[1];
            buffer[0] = enableByte;
            writeBytes(pwmControlRegister, buffer);
            if (lastOperationStatus == BoardStatus.statusOk)
            {
                isPwmEnabled = true;
            }
        }

        public void SetPwmFrequency(uint frequency)
        {
            if (frequency < 1 || frequency > 1000)
            {
                lastOperationStatus = BoardStatus.statusErrorParameter;
            }
            else
            {
                bool pwmPreviousFlag = isPwmEnabled;
                SetPwmDisable();
                byte[] buffer = new byte[2];
                buffer[0] = (byte)(frequency >> 8);
                buffer[1] = (byte)(frequency & 0xff);
                writeBytes(pwmFrequencyRegister, buffer);
                Thread.Sleep(100);
                if (pwmPreviousFlag)
                {
                    SetPwmEnable();
                }
            }
        }

        public void SetPwmDutyCycle(byte channelId, short Duty)
        {
            if (Duty < 0 ||  Duty > 100)
            {
                lastOperationStatus = BoardStatus.statusErrorParameter;
            }
            byte[] buffer = new byte[2];
            buffer[0] = (byte)Duty;
            buffer[0] = (byte)(Duty * 10 % 10);
            writeBytes(channelId, buffer);
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
            writeBytes(adcControlRegister, buffer);
        }

        public void SetAdcDisable()
        {
            byte[] buffer = new byte[1];
            buffer[0] = disableByte;
            writeBytes(adcControlRegister, buffer);
        }

        public int GetAdcValue(byte channelId)
        {
            byte[] buffer = new byte[2];
            readBytes(channelId, buffer, 2);
            if (lastOperationStatus == BoardStatus.statusOk)
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
                if (Start() == BoardStatus.statusOk)
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
            lastOperationStatus = BoardStatus.statusOk;
            return validAddresses;
        }


    }
} 
