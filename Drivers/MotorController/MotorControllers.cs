using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sensors_test.Drivers.MotorControllers
{
    public interface MotorDriver
    {
        private ExpansionBoards.DFRIOExpansion controller { get; set; }
        public byte MotorDriverAddress { get; set; }
        public byte Motor1DirectionPin { get; set; }
        public byte Motor2DirectionPin { get; set; }
        byte Left { get; set; }
        byte Right { get; set; }
        byte Forwards { get; set; }
        byte Backwards { get; set; }
    }

    internal class PDSGBGearboxMotorController
    {
        private readonly MotorDriver motorDriver;
        PDSGBGearboxMotorController(MotorDriver MotorDriver)
        {
            motorDriver = MotorDriver;
        }
    }
}
