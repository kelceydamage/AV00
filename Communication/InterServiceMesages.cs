using AV00.Controllers.MotorController;
using AV00.Shared;
using NetMQ;
using System.Device.Gpio;
using Transport.Messages;

namespace AV00.Communication
{
    public interface IEvent
    {
        public const short FrameCount = 4;
        public EnumEventType Type { get; }
        public NetMQMessage Serialize();
    }

    public class MotorEvent : Event<MotorCommandData>, IEvent
    {
        public MotorEvent(string ServiceName, EnumMotorCommands Command, PinValue Direction, ushort PwmAmount, EnumExecutionMode Mode = EnumExecutionMode.Blocking) : base(ServiceName)
        {
            data = new(Command, Direction, PwmAmount, Id, Mode);
        }

        public MotorEvent(NetMQMessage WireMessage) : base(WireMessage) { }

        public static new MotorEvent Deserialize(NetMQMessage WireMessage)
        {
            return (MotorEvent)Event<MotorCommandData>.Deserialize(WireMessage);
        }
    }
}
