using sensors_test.Drivers;
using sensors_test.Drivers.ExpansionBoards;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Device.Pwm;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static sensors_test.Drivers.ExpansionBoards.DFRIOExpansion;

namespace sensors_test.Controllers.MotorController
{

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
