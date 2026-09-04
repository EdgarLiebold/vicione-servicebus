using System;
using System.Security.Cryptography;
using System.Text;
using ViciOne.ServiceBus.Metadata;
using ViciOne.ServiceBus.Topology;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.AzureServiceBusTransport.Topology;

public class ServiceBusPublishTopology :
    PublishTopology,
    IServiceBusPublishTopologyConfigurator
{
    readonly IMessageTopology _messageTopology;

    public ServiceBusPublishTopology(IMessageTopology messageTopology)
    {
        _messageTopology = messageTopology;
    }

    IServiceBusMessagePublishTopology<T> IServiceBusPublishTopology.GetMessageTopology<T>()
    {
        return GetMessageTopology<T>() as IServiceBusMessagePublishTopologyConfigurator<T>
            ?? throw new InvalidOperationException($"The publish topology for {typeof(T).FullName} is not an Azure Service Bus topology.");
    }

    public string FormatSubscriptionName(string subscriptionName)
    {
        string name;
        if (subscriptionName.Length > 50)
        {
            string hashed;
            using (var hasher = SHA1.Create())
            {
                var buffer = Encoding.UTF8.GetBytes(subscriptionName);
                var hash = hasher.ComputeHash(buffer);
                hashed = FormatUtil.Formatter.Format(hash).Substring(0, 6);
            }

            name = $"{subscriptionName.Substring(0, 43)}-{hashed}";
        }
        else
            name = subscriptionName;

        return name;
    }

    public string GenerateSubscriptionName(string entityName, string? hostScope = null)
    {
        if (entityName == null)
            throw new ArgumentNullException(nameof(entityName));

        return FormatSubscriptionName(string.IsNullOrWhiteSpace(hostScope) ? entityName : $"{entityName}-{hostScope}");
    }

    IServiceBusMessagePublishTopologyConfigurator IServiceBusPublishTopologyConfigurator.GetMessageTopology(Type messageType)
    {
        return GetMessageTopology(messageType) as IServiceBusMessagePublishTopologyConfigurator
            ?? throw new InvalidOperationException($"The publish topology for {messageType.FullName} is not an Azure Service Bus topology.");
    }

    public BrokerTopology GetPublishBrokerTopology()
    {
        var builder = new PublishEndpointBrokerTopologyBuilder(this);

        ForEachMessageType<IServiceBusMessagePublishTopology>(x =>
        {
            x.Apply(builder);

            builder.Topic = null;
        });

        return builder.BuildBrokerTopology();
    }

    IServiceBusMessagePublishTopologyConfigurator<T> IServiceBusPublishTopologyConfigurator.GetMessageTopology<T>()
    {
        return GetMessageTopology<T>() as IServiceBusMessagePublishTopologyConfigurator<T>
            ?? throw new InvalidOperationException($"The publish topology for {typeof(T).FullName} is not an Azure Service Bus topology.");
    }

    protected override IMessagePublishTopologyConfigurator CreateMessageTopology<T>()
    {
        var messageTopology = new ServiceBusMessagePublishTopology<T>(this, _messageTopology.GetMessageTopology<T>());

        var connector = new ImplementedMessageTypeConnector<T>(this, messageTopology);

        ImplementedMessageTypeCache<T>.EnumerateImplementedTypes(connector);

        OnMessageTopologyCreated(messageTopology);

        return messageTopology;
    }


    class ImplementedMessageTypeConnector<TMessage> :
        IImplementedMessageType
        where TMessage : class
    {
        readonly ServiceBusMessagePublishTopology<TMessage> _messagePublishTopologyConfigurator;
        readonly IServiceBusPublishTopologyConfigurator _publishTopology;

        public ImplementedMessageTypeConnector(IServiceBusPublishTopologyConfigurator publishTopology,
            ServiceBusMessagePublishTopology<TMessage> messagePublishTopologyConfigurator)
        {
            _publishTopology = publishTopology;
            _messagePublishTopologyConfigurator = messagePublishTopologyConfigurator;
        }

        public void ImplementsMessageType<T>(bool direct)
            where T : class
        {
            IServiceBusMessagePublishTopologyConfigurator<T> messageTopology = _publishTopology.GetMessageTopology<T>();

            _messagePublishTopologyConfigurator.AddImplementedMessageConfigurator(messageTopology, direct);
        }
    }
}
