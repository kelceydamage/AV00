using AV00_Shared.FlowControl;
using AV00_Shared.Models;
using System.Device.Gpio;
using System.Text.Json.Serialization;

namespace AV00.Controllers.MotorController
{
    [Serializable]
    public class MotorCommandEventModel : EventModel, IEventModel
    {
        public PinValue Direction { get => direction; }
        private readonly PinValue direction;
        public float PwmAmount { get => pwmAmount; }
        private readonly float pwmAmount;
        public EnumMotorCommands Command { get => command; }
        private readonly EnumMotorCommands command;
        public EnumExecutionMode Mode { get => mode; }
        private readonly EnumExecutionMode mode;

        public MotorCommandEventModel(
            string ServiceName,
            EnumMotorCommands Command,
            PinValue Direction,
            float PwmAmount,
            EnumExecutionMode Mode = EnumExecutionMode.Blocking,
            Guid? Id = null,
            string? TimeStamp = null
        ) : base(ServiceName, Id, TimeStamp)
        {
            command = Command;
            direction = Direction;
            pwmAmount = PwmAmount;
            mode = Mode;
        }

        [JsonConstructor]
        public MotorCommandEventModel(
            string ServiceName,
            EnumMotorCommands Command,
            PinValue Direction,
            float PwmAmount,
            EnumExecutionMode Mode,
            Guid Id,
            string TimeStamp
        ) : base(ServiceName, Id, TimeStamp)
        {
            command = Command;
            direction = Direction;
            pwmAmount = PwmAmount;
            mode = Mode;
        }
    }
}
