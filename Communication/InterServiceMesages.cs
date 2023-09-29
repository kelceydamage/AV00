using NetMQ;
using Transport.Messages;

namespace AV00.Communication
{
    public interface IEvent
    {
        public const short FrameCount = 4;
        public EnumEventType Type { get; }
        public NetMQMessage Serialize();
    }
}
