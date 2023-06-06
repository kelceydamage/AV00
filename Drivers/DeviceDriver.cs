using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sensors_test.Drivers
{
    public interface IDeviceDriver
    {
        
    }

    public interface IMotorDriver : IDeviceDriver
    {
        public byte Left { get; }
        public byte Right { get; }
        public byte Forwards { get; }
        public byte Backwards { get; }
        public void Start(byte Direction, byte Power);
        public void Stop();
    }
}
