using sensors_test.Drivers.ExpansionBoards;
using static sensors_test.Drivers.ExpansionBoards.DFRIOExpansion;

namespace sensors_test.Drivers.Motors
{
    public class MDD10A : PWMDeviceDriver
    {
        public byte MotorDriverAddress { get; } = 0x10;
        public byte MotorDirectionPin { get; }
        public uint MotorPwmFrequency { get; } = 18000;
        public byte Left { get; } = 0;
        public byte Right { get; } = 1;
        public byte Forwards { get; } = 0;
        public byte Backwards { get; } = 1;
        public byte DutyDownCycleStep { get; } = 1;
        public short DutyDownCycleIntervalMs { get; } = 300;
        public byte PwmChannel { get; }
        private readonly byte motorDirectionPin;
        private byte currentDirection = 0;

        public MDD10A(byte MotorDirectionPinId, PwmChannelRegisters PwmChannelRegister, IExpansionBoard PWMController) : base(PWMController)
        {
            MotorDirectionPin = MotorDirectionPinId;
            PwmChannel = (byte)PwmChannelRegister;
        }
    }
}
