using System.Configuration;
using Transport.Generics;
using Transport.Client;
using Transport.Event;
using System.Collections.Specialized;
using AV00_Shared.Models;

namespace AV00.Communication
{
    public class ServiceBusClient : BaseTransportClient
    {
        private readonly PushClient transportRelayClient;

        public ServiceBusClient(string ServiceBusClientSocket, string ReceiptEventSocket, short FrameCount) : base(
            new SubscriberClient($"{ReceiptEventSocket}"),
            FrameCount
        )
        {
            transportRelayClient = new PushClient(ServiceBusClientSocket);
        }

        public ServiceBusClient(ConnectionStringSettingsCollection Connections, NameValueCollection Settings) : base(
            new SubscriberClient($"{Connections["EventReceiptSocket"].ConnectionString}"),
            short.Parse(Settings["TransportMessageFrameCount"] ?? throw new Exception())
        )
        {
            transportRelayClient = new PushClient(Connections["TransportRelayClientSocket"].ConnectionString);
        }

        public void PushTask<T>(Event<T> Task)
        {
            transportRelayClient.SendMQMessage(Task.Serialize());
        }
    }

    public class TaskExecutorClient : BaseTransportClient
    {
        private readonly PushClient transportRelayClient;

        public TaskExecutorClient(string TaskEventSocket, string ServiceBusClientSocket, short FrameCount) : base(
            new SubscriberClient($">{TaskEventSocket}"),
            FrameCount
        )
        {
            transportRelayClient = new($">{ServiceBusClientSocket}");
        }

        public TaskExecutorClient(ConnectionStringSettingsCollection Connections, NameValueCollection Settings) : base(
            new SubscriberClient($">{Connections["EventSocket"].ConnectionString}"),
            short.Parse(Settings["TransportMessageFrameCount"] ?? throw new Exception())
        )
        {
            transportRelayClient = new($">{Connections["TransportRelayClientSocket"].ConnectionString}");
        }

        public void PublishReceipt(Event<TaskExecutionEventModel> Receipt)
        {
            transportRelayClient.SendMQMessage(Receipt.Serialize());
        }
    }
}
