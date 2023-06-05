using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sensors_test.Drivers.MotorControllers
{
    public interface IMotorDriver
    {
        public ExpansionBoards.DFRIOExpansion Controller { get; }
        public byte MotorDriverAddress { get; }
        public byte MotorDirectionPin { get; }
        public uint MotorPwmFrequency { get; }
        public byte Left { get; }
        public byte Right { get; }
        public byte Forwards { get; }
        public byte Backwards { get; }
        public byte DutyDownCycleStep { get; }
        public short DutyDownCycleIntervalMs { get; }

        public void RunMotor() { }
    }

    internal class PDSGBGearboxMotorController
    {
        public byte Motor1DirectionPin { get; } = 17;
        public byte Motor2DirectionPin { get; } = 16;
        private readonly IMotorDriver motorDriver1;
        private readonly IMotorDriver motorDriver2;

        public PDSGBGearboxMotorController(IMotorDriver MotorDriver1, IMotorDriver MotorDriver2)
        {
            motorDriver1 = MotorDriver1;
            motorDriver2 = MotorDriver2;
        }

        public void Move() { }
        public void Turn() { }

    }
}
