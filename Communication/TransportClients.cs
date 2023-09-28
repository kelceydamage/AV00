using System.Configuration;
using Transport.Messages;
using Transport.Generics;
using Transport.Client;

namespace AV00.Communication
{
    public class ServiceBusClient : BaseTransportClient
    {
        private readonly PushClient ServiceBusProducer;

        public ServiceBusClient(string ServiceBusClientSocket, string ReceiptEventSocket, short FrameCount) : base(
            new SubscriberClient($"{ReceiptEventSocket}"),
            FrameCount
        )
        {
            ServiceBusProducer = new PushClient(ServiceBusClientSocket);
        }

        public ServiceBusClient(ConnectionStringSettingsCollection Connections) : base(
            new SubscriberClient($"{Connections["ReceiptEventSocket"].ConnectionString}"),
            short.Parse(Connections["TransportMessageFrameCount"].ConnectionString)
        )
        {
            ServiceBusProducer = new PushClient(Connections["ServiceBusClientSocket"].ConnectionString);
        }

        public void PushTask(BaseEvent Task)
        {
            ServiceBusProducer.SendMQMessage(Task.Serialize());
        }
    }

    public class TaskExecutorClient : BaseTransportClient
    {
        private readonly PushClient ServiceBusProducer;

        public TaskExecutorClient(string TaskEventSocket, string ServiceBusClientSocket, short FrameCount) : base(
            new SubscriberClient($">{TaskEventSocket}"),
            FrameCount
        )
        {
            ServiceBusProducer = new($">{ServiceBusClientSocket}");
        }

        public TaskExecutorClient(ConnectionStringSettingsCollection Connections) : base(
            new SubscriberClient($">{Connections["TaskEventSocket"].ConnectionString}"),
            short.Parse(Connections["TransportMessageFrameCount"].ConnectionString)
        )
        {
            ServiceBusProducer = new($">{Connections["ServiceBusClientSocket"].ConnectionString}");
        }

        public void PublishReceipt(BaseEvent Receipt)
        {
            ServiceBusProducer.SendMQMessage(Receipt.Serialize());
        }
    }
}
