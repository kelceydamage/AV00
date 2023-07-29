// based on https://github.com/NachtRaveVL/PCA9685-Arduino/blob/master/src/PCA9685.cpp

using System;
using System.Collections.Generic;
using System.Device.I2c;
using System.Device.Pwm;
using System.Linq;
using System.Net;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace sensors_test.Drivers.IO
{
    public class PCA9685 : IPwmController
    {

        static readonly byte i2cAddress = 0x40;
        //static readonly byte i2cAddressMask = 0x3F;
        //static readonly byte i2cProxyAddress = 0xE0;
        //static readonly byte i2cProxyAddressMask = 0xFE;

        // Register addresses from data sheet
        static readonly byte mode1Register = 0x00;
        //static readonly byte mode2Register = 0x01;
        //static readonly byte subAddress1Register = 0x02;
        //static readonly byte subAddress2Register = 0x03;
        //static readonly byte subAddress3Register = 0x04;
        //static readonly byte allCallRegister = 0x05;
        static readonly byte led0Register = 0x06; // Start of LEDx regs, 4B per reg, 2B on phase, 2B off phase, little-endian. 16 channels
        static readonly byte prescaleRegister = 0xFE;
        //static readonly byte allLedRegister = 0xFA;

        // Mode1 register values
        static readonly byte mode1Restart = 0x80;
        //static readonly byte mode1ExternalClock = 0x10;
        //static readonly byte mode1AutoIncrement = 0x20;
        static readonly byte mode1Sleep = 0x10;
        //static readonly byte mode1SubAddress1 = 0x08;
        //static readonly byte mode1SubAddress2 = 0x04;
        //static readonly byte mode1SubAddress3 = 0x02;
        //static readonly byte mode1AllCall = 0x01;

        // Mode2 register values
        //static readonly byte mode2OutDRV_TPOLE = 0x04;
        //static readonly byte mode2Invert = 0x10;
        //static readonly byte mode2OutNE_TPHIGH = 0x01;
        //static readonly byte mode2OutNE_HIGHZ = 0x02;
        //static readonly byte mode2OCH_ONACK = 0x08;

        static readonly byte softwareReset = 0x06;  // Sent to address 0x00 to reset all devices on Wire line
        static readonly ushort pwmFull = 0x1000;    // Special value for full on/full off LEDx modes
        static readonly ushort pwmMask = 0x0FFF;    // Mask for 12-bit/4096 possible phase positions

        static readonly int defaultChannelCount = 16;
        static readonly int minChannel = 0;
        //static readonly int defaultMaxChannel = defaultChannelCount - 1;
        //static readonly int allChannels = -1;       // Special value for ALLLED registers

        private readonly int channelCount;
        private readonly int maxChannel;
        private int referenceClockSpeed = 25000000;
        private readonly float referenceClockDivider = 4096.0f;
        private readonly I2cChannel i2c;
        private readonly byte[] pwmWriteChannels;
        private readonly byte[] pwmReadChannels;
        private enum PhaseBalancerSettings
        {
            None,
            Count,
            Undefined,
            Linear
        }
        private readonly PhaseBalancerSettings phaseBalancer = PhaseBalancerSettings.None;

        public byte[] PwmChannelAddresses
        {
            get { return pwmWriteChannels; }
        }
        public string Name { get { return "PCA9685"; } set { } }
        public int PwmMaxFrequencyHz { get { return 1526; } }
        public int PwmMinFrequencyHz { get { return 24; } }
        public int PwmBitDepth { get { return 12; } }
        public int PwmChannelCount { get { return channelCount; } }

        public PCA9685(int I2cBus, int ChannelCount = 16)
        {
            I2cConnectionSettings I2cSettings = new(I2cBus, i2cAddress);
            i2c = new I2cChannel(I2cSettings);
            if (ChannelCount < 1 || ChannelCount > defaultChannelCount)
            {
                throw new Exception("ChannelCount must be between 1 and 16");
            }
            channelCount = ChannelCount;
            maxChannel = channelCount - 1;
            pwmWriteChannels = new byte[channelCount];
            pwmReadChannels = new byte[channelCount];
            for (int i = 0; i < channelCount; i++)
            {
                pwmWriteChannels[i] = (byte)(led0Register + (i * 0x04));
                pwmReadChannels[i] = (byte)(led0Register + (i << 2));
            }
            Reset();
        }

        public void Reset()
        {
            i2c.WriteBytes(mode1Register, new byte[] { softwareReset });
            Thread.Sleep(1);
        }

        // The overall PWM frequency in Hertz.
        public float GetFrequency()
        {
            byte[] prescaleReadBuffer = new byte[1];
            i2c.ReadBytes(prescaleRegister, prescaleReadBuffer);
            if (prescaleReadBuffer[0] < 3)
            {
                throw new Exception("The device pre_scale register (0xFE) was not read or returned a value < 3");
            }
            return referenceClockSpeed / referenceClockDivider / prescaleReadBuffer[0];
        }

        // Min: 24Hz, Max: 1526Hz, Default: 200Hz. As Hz increases channel resolution
        // diminishes, as raw pre-scaler value, computed per datasheet, starts to require
        // much larger frequency increases for single-digit increases of the raw pre-scaler
        // value that ultimately controls the PWM frequency produced.
        public void SetFrequency(float Frequency) // Hz
        {
            int prescalerValue = (int)(referenceClockSpeed / (referenceClockDivider * Frequency)) - 1;
            if (prescalerValue > 255) prescalerValue = 255;
            if (prescalerValue < 3) prescalerValue = 3;

            byte[] mode1ReadBuffer = new byte[1];
            i2c.ReadBytes(mode1Register, mode1ReadBuffer);
            // The PRE_SCALE register can only be set when the SLEEP bit of MODE1 register is set to logic 1.
            i2c.WriteBytes(mode1Register, new byte[] { (byte)((mode1ReadBuffer[0] & ~mode1Restart) | mode1Sleep) });
            i2c.WriteBytes(prescaleRegister, new byte[] { (byte)prescalerValue });
            // It takes 500us max for the oscillator to be up and running once SLEEP bit has been set to logic 0.
            i2c.WriteBytes(mode1Register, new byte[] { (byte)((mode1ReadBuffer[0] & ~mode1Sleep) | mode1Restart) });
            Thread.Sleep(1);
        }

        public void SetChannelPwmAll(ushort PwmAmount)
        {
            for (int i = 0; i < channelCount; i++)
            {
                SetChannelPwm(i, PwmAmount);
            }
        }

        // PWM amounts 0 - 4096, 0 full off, 4096 full on
        public void SetChannelPwm(int ChannelId, ushort PwmAmount)
        {
            ValidateChannelId(ChannelId);

            GetPhaseCycle(ChannelId, PwmAmount, out ushort phaseBegin, out ushort phaseEnd);

            i2c.WriteBytes(
                pwmWriteChannels[ChannelId],
                new byte[]
                { 
                    (byte)(phaseEnd & 0xff),    // low
                    (byte)(phaseEnd >> 8),      // high
                    (byte)(phaseBegin & 0xff),  // low
                    (byte)(phaseBegin >> 8),    // high
                }
            );
        }

        private void ValidateChannelId(int ChannelId)
        {
            if (ChannelId < minChannel || ChannelId > maxChannel)
            {
                throw new Exception($"Channel must be between {minChannel} and {maxChannel} inclusive");
            }
        }

        // Not sure if this is the right way to do this, ~but it's what the Adafruit library does.
        public ushort GetChannelPwm(int ChannelId)
        {
            ValidateChannelId(ChannelId);

            byte[] pwmReadBuffer = new byte[4];
            i2c.ReadBytes(pwmReadChannels[ChannelId], pwmReadBuffer);
            ushort phaseBegin, phaseEnd;
            phaseEnd = (ushort)pwmReadBuffer[0];
            phaseEnd |= (ushort)(pwmReadBuffer[1] << 8);
            phaseBegin = (ushort)pwmReadBuffer[2];
            phaseBegin |= (ushort)(pwmReadBuffer[3] << 8);

            if (phaseEnd >= pwmFull)
            {
                return 0;
            }
            else if (phaseBegin >= pwmFull)
            {
                return pwmFull;
            }
            else if (phaseBegin <= phaseEnd)
            {
                return (ushort)(phaseEnd - phaseBegin);
            }
            return (ushort)((phaseEnd + pwmFull) - phaseBegin);
        }

        public byte GetLastI2cError()
        {
            throw new NotImplementedException();
        }

        public void SetChannelOn(int Channel)
        {
            throw new NotImplementedException();
        }

        public void GetPhaseCycle(int ChannelId, ushort pwmAmount, out ushort phaseBegin, out ushort phaseEnd)
        {
            phaseBegin = 0;
            if (phaseBalancer == PhaseBalancerSettings.Linear)
            {
                phaseBegin = (ushort)((ushort)(ChannelId * ((4096 / 16) / 16)) & pwmMask);
            }
            if (pwmAmount == 0)
            {
                phaseEnd = pwmFull;
            }
            else if (pwmAmount == pwmFull)
            {
                phaseBegin |= pwmFull;
                phaseEnd = 0;
            }
            else
            {
                phaseEnd = (ushort)((phaseBegin + pwmAmount) & pwmMask);
            }
        }

        // If there is a calibrated reference clock speed, use it to calculate the frequency
        public void SetCalibratedReferenceClockSpeed(int ReferenceClockSpeed)
        {
            referenceClockSpeed = ReferenceClockSpeed;
        }
    }
}
