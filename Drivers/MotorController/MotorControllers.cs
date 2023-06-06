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
        public byte Motor2DirectionPin { get; } = 18;
        private readonly IMotorDriver turningMotor;
        private readonly IMotorDriver driveMotor;

        public PDSGBGearboxMotorController(IMotorDriver TurningMotor, IMotorDriver DriveMotor)
        {
            turningMotor = TurningMotor;
            driveMotor = DriveMotor;
        }

        public void Move() { }
        public void Turn() { }

    }
}
