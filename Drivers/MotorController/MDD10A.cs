using System.Device.I2c;

namespace sensors_test.Drivers.MotorController
{
    public class MDD10A
    {
        private ExpansionBoards.DFRIOExpansion Controller { get; set; }
        public byte MotorDriverAddress { get; private set; } = 0x10;
        public byte Motor1DirectionPin { get; private set; } = 17;
        public byte Motor2DirectionPin { get; private set; } = 16;
        byte Left { get; set; } = 0;
        byte Right { get; set; } = 1;
        byte Forwards { get; set; } = 0;
        byte Backwards { get; set; } = 1;
        private readonly int dutyDownCycleInterval = 1;
        private readonly float dutyDownCycleDelay = 0.03f;
        private readonly int pwmChip;
        private readonly int pwmChannelNumber;

        public MDD10A(I2cConnectionSettings Settings)
        {
            Controller = new ExpansionBoards.DFRIOExpansion(Settings);
        }
    }
}
