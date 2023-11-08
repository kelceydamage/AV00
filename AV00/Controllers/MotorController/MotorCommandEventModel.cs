using AV00_Shared.FlowControl;
using AV00_Shared.Models;
using System.Text.Json.Serialization;

namespace AV00.Controllers.MotorController
{
    [Serializable]
    public class MotorCommandEventModel : EventModel, IEventModel
    {
        public EnumMotorDirection Direction { get => direction; }
        private readonly EnumMotorDirection direction;
        public float PwmAmount { get => pwmAmount; }
        private readonly float pwmAmount;
        public EnumMotorCommands Command { get => command; }
        private readonly EnumMotorCommands command;
        public EnumExecutionMode Mode { get => mode; }
        private readonly EnumExecutionMode mode;

        public MotorCommandEventModel(
            string ServiceName,
            EnumMotorCommands Command,
            EnumMotorDirection Direction,
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
            int Direction,
            float PwmAmount,
            EnumExecutionMode Mode,
            string Id,
            string TimeStamp
        ) : base(ServiceName, Guid.Parse(Id), TimeStamp)
        {
            command = Command;
            direction = (EnumMotorDirection)Enum.ToObject(typeof(EnumMotorDirection), Direction);
            pwmAmount = PwmAmount;
            mode = Mode;
        }
    }
}
