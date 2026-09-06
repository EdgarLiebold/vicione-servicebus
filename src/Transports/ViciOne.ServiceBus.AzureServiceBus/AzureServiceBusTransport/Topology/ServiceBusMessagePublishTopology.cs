using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Azure.Messaging.ServiceBus.Administration;
using ViciOne.ServiceBus.AzureServiceBus.Configuration;
using ViciOne.ServiceBus.Topology;

namespace ViciOne.ServiceBus.AzureServiceBus.Topology;

/// <summary>
/// Provides a service bus message publish topology implementation.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
public class ServiceBusMessagePublishTopology<TMessage> :
    MessagePublishTopology<TMessage>,
    IServiceBusMessagePublishTopologyConfigurator<TMessage>
    where TMessage : class
{
    readonly Lazy<CreateTopicOptions> _createTopicOptions;
    readonly IList<IServiceBusMessagePublishTopology> _implementedMessageTypes;
    readonly IServiceBusPublishTopology _publishTopology;
    readonly ServiceBusTopicConfigurator _topicConfigurator;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="publishTopology">The publish topology value.</param>
    /// <param name="messageTopology">The message topology value.</param>
    public ServiceBusMessagePublishTopology(IServiceBusPublishTopology publishTopology, IMessageTopology<TMessage> messageTopology)
        : base(publishTopology)
    {
        _publishTopology = publishTopology;

        _topicConfigurator = new ServiceBusTopicConfigurator(messageTopology.EntityName, MessageTypeCache<TMessage>.IsTemporaryMessageType);
        _implementedMessageTypes = new List<IServiceBusMessagePublishTopology>();

        _createTopicOptions = new Lazy<CreateTopicOptions>(() => _topicConfigurator.GetCreateTopicOptions());
    }

    /// <summary>
    /// Attempts to get publish address.
    /// </summary>
    /// <param name="baseAddress">The base address value.</param>
    /// <param name="publishAddress">The publish address value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public override bool TryGetPublishAddress(Uri baseAddress, [NotNullWhen(true)] out Uri? publishAddress)
    {
        publishAddress = new ServiceBusEndpointAddress(new Uri(baseAddress.GetLeftPart(UriPartial.Authority)),
            _topicConfigurator.FullPath, _topicConfigurator.AutoDeleteOnIdle, ServiceBusEndpointAddress.AddressType.Topic);

        return true;
    }

    /// <summary>
    /// Gets the create topic options value.
    /// </summary>
    public CreateTopicOptions CreateTopicOptions => _createTopicOptions.Value;

    /// <summary>
    /// Gets send settings.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public SendSettings GetSendSettings()
    {
        var createTopicOptions = _topicConfigurator.GetCreateTopicOptions();

        var builder = new PublishEndpointBrokerTopologyBuilder(_publishTopology);

        Apply(builder);

        return new TopicSendSettings(createTopicOptions, builder.BuildBrokerTopology());
    }

    /// <summary>
    /// Gets subscription configurator.
    /// </summary>
    /// <param name="subscriptionName">The subscription name value.</param>
    /// <returns>The result of the operation.</returns>
    public ServiceBusSubscriptionConfigurator GetSubscriptionConfigurator(string subscriptionName)
    {
        return new ServiceBusSubscriptionConfigurator(_publishTopology.FormatSubscriptionName(subscriptionName), CreateTopicOptions.Name);
    }

    /// <summary>
    /// Gets the path value.
    /// </summary>
    public string Path => _topicConfigurator.Path;

    /// <summary>
    /// Gets or sets the base path value.
    /// </summary>
    public string? BasePath
    {
        get => _topicConfigurator.BasePath;
        set => _topicConfigurator.BasePath = value;
    }

    /// <summary>
    /// Gets the full path value.
    /// </summary>
    public string FullPath => _topicConfigurator.FullPath;

    /// <summary>
    /// Gets or sets the duplicate detection history time window value.
    /// </summary>
    public TimeSpan? DuplicateDetectionHistoryTimeWindow
    {
        set => _topicConfigurator.DuplicateDetectionHistoryTimeWindow = value;
    }

    /// <summary>
    /// Gets or sets the enable partitioning value.
    /// </summary>
    public bool? EnablePartitioning
    {
        set => _topicConfigurator.EnablePartitioning = value;
    }

    /// <summary>
    /// Gets or sets the max size in megabytes value.
    /// </summary>
    public long? MaxSizeInMegabytes
    {
        set => _topicConfigurator.MaxSizeInMegabytes = value;
    }

    /// <summary>
    /// Gets or sets the max message size in kilobytes value.
    /// </summary>
    public long? MaxMessageSizeInKilobytes
    {
        set => _topicConfigurator.MaxMessageSizeInKilobytes = value;
    }

    /// <summary>
    /// Gets or sets the requires duplicate detection value.
    /// </summary>
    public bool? RequiresDuplicateDetection
    {
        set => _topicConfigurator.RequiresDuplicateDetection = value;
    }

    /// <summary>
    /// Gets or sets the support ordering value.
    /// </summary>
    public bool? SupportOrdering
    {
        set => _topicConfigurator.SupportOrdering = value;
    }

    /// <summary>
    /// Performs the enable duplicate detection operation.
    /// </summary>
    /// <param name="historyTimeWindow">The history time window value.</param>
    public void EnableDuplicateDetection(TimeSpan historyTimeWindow)
    {
        _topicConfigurator.EnableDuplicateDetection(historyTimeWindow);
    }

    /// <summary>
    /// Gets or sets the auto delete on idle value.
    /// </summary>
    public TimeSpan? AutoDeleteOnIdle
    {
        set => _topicConfigurator.AutoDeleteOnIdle = value;
    }

    /// <summary>
    /// Gets or sets the default message time to live value.
    /// </summary>
    public TimeSpan? DefaultMessageTimeToLive
    {
        set => _topicConfigurator.DefaultMessageTimeToLive = value;
    }

    /// <summary>
    /// Gets or sets the enable batched operations value.
    /// </summary>
    public bool? EnableBatchedOperations
    {
        set => _topicConfigurator.EnableBatchedOperations = value;
    }

    /// <summary>
    /// Gets or sets the user metadata value.
    /// </summary>
    public string UserMetadata
    {
        set => _topicConfigurator.UserMetadata = value;
    }

    /// <summary>
    /// Applies this specification to the target builder.
    /// </summary>
    /// <param name="builder">The builder value.</param>
    public void Apply(IPublishEndpointBrokerTopologyBuilder builder)
    {
        if (Exclude)
            return;

        var topicHandle = builder.CreateTopic(_createTopicOptions.Value);

        builder.Topic = topicHandle;

        foreach (var configurator in _implementedMessageTypes)
            configurator.Apply(builder);
    }

    /// <summary>
    /// Adds implemented message configurator to the configuration.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="configurator">The configurator value.</param>
    /// <param name="direct">The direct value.</param>
    public void AddImplementedMessageConfigurator<T>(IServiceBusMessagePublishTopologyConfigurator<T> configurator, bool direct)
        where T : class
    {
        var adapter = new ImplementedTypeAdapter<T>(configurator, direct);

        _implementedMessageTypes.Add(adapter);
    }


    class ImplementedTypeAdapter<T> :
        IServiceBusMessagePublishTopology
        where T : class
    {
        readonly IServiceBusMessagePublishTopologyConfigurator<T> _configurator;
        readonly bool _direct;

        public ImplementedTypeAdapter(IServiceBusMessagePublishTopologyConfigurator<T> configurator, bool direct)
        {
            _configurator = configurator;
            _direct = direct;
        }

        public void Apply(IPublishEndpointBrokerTopologyBuilder builder)
        {
            if (_direct)
            {
                var implementedBuilder = builder.CreateImplementedBuilder();

                _configurator.Apply(implementedBuilder);
            }
        }
    }
}
