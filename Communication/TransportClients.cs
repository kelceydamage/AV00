using System.Configuration;
using Transport.Generics;
using Transport.Client;
using Transport.Messages;
using System.Collections.Specialized;

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

        public ServiceBusClient(ConnectionStringSettingsCollection Connections, NameValueCollection Settings) : base(
            new SubscriberClient($"{Connections["ReceiptEventSocket"].ConnectionString}"),
            short.Parse(Settings["TransportMessageFrameCount"] ?? throw new Exception())
        )
        {
            ServiceBusProducer = new PushClient(Connections["ServiceBusClientSocket"].ConnectionString);
        }

        public void PushTask<T>(Event<T> Task)
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

        public TaskExecutorClient(ConnectionStringSettingsCollection Connections, NameValueCollection Settings) : base(
            new SubscriberClient($">{Connections["TaskEventSocket"].ConnectionString}"),
            short.Parse(Settings["TransportMessageFrameCount"] ?? throw new Exception())
        )
        {
            ServiceBusProducer = new($">{Connections["ServiceBusClientSocket"].ConnectionString}");
        }

        public void PublishReceipt(Event<TaskExecution> Receipt)
        {
            ServiceBusProducer.SendMQMessage(Receipt.Serialize());
        }
    }
}
