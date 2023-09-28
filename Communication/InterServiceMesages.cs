using NetMQ;
using System.Device.Gpio;
using System.Text.Json;
using Transport.Messages;

namespace AV00.Communication
{
    public interface IEvent
    {
        public const short FrameCount = 4;
        public EnumEventType Type { get; }
        public NetMQMessage Serialize();
        public void Deserialize(NetMQMessage WireMessage);
    }

    [Serializable]
    public readonly struct MotorCommandData
    {
        public readonly PinValue Direction { get => direction; }
        private readonly PinValue direction;
        public readonly ushort PwmAmount { get => pwmAmount; }
        private readonly ushort pwmAmount;
        public readonly string Command { get => command; }
        private readonly string command;

        public MotorCommandData(string Command, PinValue Direction, ushort PwmAmount)
        {
            command = Command;
            direction = Direction;
            pwmAmount = PwmAmount;
        }
    }

    public class TaskEvent : BaseEvent, IEvent
    {
        public EnumEventType Type { get => EnumEventType.Event; }
        public string ServiceName { get => serviceName; }
        private string serviceName;
        public Guid Id { get => id; }
        private Guid id;
        public MotorCommandData Data { get => data; }
        private MotorCommandData data;
        private readonly bool isNull = false;

        public TaskEvent(string ServiceName, MotorCommandData Data, Guid? TaskId = null)
        {
            serviceName = ServiceName;
            if (TaskId != null)
            {
                id = (Guid)TaskId;
            }
            else
            {
                id = Guid.NewGuid();
            }
            data = Data;
        }

        public TaskEvent()
        {
            isNull = true;
            serviceName = "null";
            data = new MotorCommandData("null", PinValue.Low, 0);
        }

        public override NetMQMessage Serialize()
        {
            if (isNull)
            {
                throw new Exception("Null task can not be serialized");
            }
            NetMQMessage WireMessage = new();
            WireMessage.Append(ServiceName);
            WireMessage.Append(Type.ToString());
            WireMessage.Append(Id.ToString());
            WireMessage.Append(JsonSerializer.Serialize(Data));
            return WireMessage;
        }

        public override void Deserialize(NetMQMessage WireMessage)
        {
            serviceName = WireMessage[0].ConvertToString();
            id = Guid.Parse(WireMessage[2].ConvertToString());
            data = JsonSerializer.Deserialize<MotorCommandData>(WireMessage[3].ConvertToString());
        }

        public TaskEventReceipt GenerateReceipt(EnumTaskEventProcessingState ProcessingState)
        {
            if (isNull)
            {
                throw new Exception("Null task can not be serialized");
            }
            return new TaskEventReceipt(ServiceName, Id, ProcessingState);
        }
    }

    public class TaskEventReceipt : BaseEvent, IEvent
    {
        public EnumEventType Type { get => EnumEventType.EventReceipt; }
        public string ServiceName { get => serviceName; }
        private string serviceName;
        public Guid Id { get => id; }
        private Guid id;
        public EnumTaskEventProcessingState ProcessingState { get => processingState; }
        public EnumTaskEventProcessingState processingState;

        public TaskEventReceipt(string ServiceName, Guid TaskId, EnumTaskEventProcessingState ProcessingState)
        {
            serviceName = ServiceName;
            id = TaskId;
            processingState = ProcessingState;
        }

        public override NetMQMessage Serialize()
        {
            NetMQMessage WireMessage = new();
            WireMessage.Append(ServiceName);
            WireMessage.Append(Type.ToString());
            WireMessage.Append(Id.ToString());
            WireMessage.Append(ProcessingState.ToString());
            return WireMessage;
        }

        public override void Deserialize(NetMQMessage WireMessage)
        {
            serviceName = WireMessage[0].ConvertToString();
            id = Guid.Parse(WireMessage[2].ConvertToString());
            processingState = (EnumTaskEventProcessingState)Enum.Parse(typeof(EnumTaskEventProcessingState), WireMessage[3].ConvertToString());
        }
    }

    public enum EnumTaskEventProcessingState
    {
        Unprocessed,
        Processing,
        Processed,
        Error
    }
}
