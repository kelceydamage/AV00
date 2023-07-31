﻿using System;
using System.Collections.Generic;
using System.Device.I2c;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sensors_test.Drivers.IO
{
    public class I2cChannel
    {
        protected readonly I2cDevice I2c;
        public readonly I2cConnectionSettings I2cConnectionSettings;
        public I2cChannel(I2cConnectionSettings Settings)
        {
            I2cConnectionSettings = Settings;
            I2c = I2cDevice.Create(I2cConnectionSettings);
        }

        public void WriteBytes(byte register, byte[] buffer)
        {
            byte[] prependedRegister = new byte[buffer.Length + 1];
            prependedRegister[0] = register;
            buffer.CopyTo(prependedRegister, 1);
            I2c.Write(prependedRegister);
        }

        public void ReadBytes(byte register, byte[] buffer)
        {
            I2c.WriteByte(register);
            I2c.Read((Span<byte>)buffer);
        }

        // Turn the MSB and LSB into a signed 16-bit value
        public static short[] MergeMSBAndLSB(Span<byte> buffer)
        {
            short[] merged = new short[3];

            merged[0] = (short)((buffer[0] << 8) | buffer[1]);
            merged[1] = (short)((buffer[2] << 8) | buffer[3]);
            merged[2] = (short)((buffer[4] << 8) | buffer[5]);

            return merged;
        }
    }
}