using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Azure.Messaging.ServiceBus.Administration;
using ViciOne.ServiceBus.AzureServiceBus.Configuration;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Topology;

namespace ViciOne.ServiceBus.AzureServiceBus.Topology;

/// <summary>Defines the Azure Service Bus topic and implemented-message relationships for a published contract.</summary>
/// <typeparam name="TMessage">The published message contract.</typeparam>
public class ServiceBusMessagePublishTopology<TMessage> :
    MessagePublishTopology<TMessage>,
    IServiceBusMessagePublishTopologyConfigurator<TMessage>
    where TMessage : class
{
    readonly Lazy<CreateTopicOptions> _createTopicOptions;
    readonly IList<IServiceBusMessagePublishTopology> _implementedMessageTypes;
    readonly IServiceBusPublishTopology _publishTopology;
    readonly ServiceBusTopicConfigurator _topicConfigurator;

    /// <summary>Creates publish topology for a message contract.</summary>
    /// <param name="publishTopology">The bus-wide Azure publish topology.</param>
    /// <param name="messageTopology">The provider-neutral topology supplying the entity name.</param>
    public ServiceBusMessagePublishTopology(IServiceBusPublishTopology publishTopology, IMessageTopology<TMessage> messageTopology)
        : base(publishTopology)
    {
        _publishTopology = publishTopology;

        _topicConfigurator = new ServiceBusTopicConfigurator(messageTopology.EntityName, MessageTypeCache<TMessage>.IsTemporaryMessageType);
        _implementedMessageTypes = new List<IServiceBusMessagePublishTopology>();

        _createTopicOptions = new Lazy<CreateTopicOptions>(() => _topicConfigurator.GetCreateTopicOptions());
    }

    /// <summary>Builds the absolute topic address for the message contract.</summary>
    /// <param name="baseAddress">The Azure Service Bus namespace address.</param>
    /// <param name="publishAddress">Receives the absolute topic address.</param>
    /// <returns>Always <see langword="true"/>.</returns>
    public override bool TryGetPublishAddress(Uri baseAddress, [NotNullWhen(true)] out Uri? publishAddress)
    {
        publishAddress = new ServiceBusEndpointAddress(new Uri(baseAddress.GetLeftPart(UriPartial.Authority)),
            _topicConfigurator.FullPath, _topicConfigurator.AutoDeleteOnIdle, ServiceBusEndpointAddress.AddressType.Topic);

        return true;
    }

    /// <summary>Gets a separate copy of the lazily materialized Azure topic declaration options.</summary>
    public CreateTopicOptions CreateTopicOptions
    {
        get
        {
            _ = _createTopicOptions.Value;
            return _topicConfigurator.GetCreateTopicOptions();
        }
    }

    /// <summary>Includes the Azure topic's path and idle-deletion constraints in publish-topology validation.</summary>
    /// <returns>The topic configuration failures.</returns>
    public override IEnumerable<ValidationResult> Validate()
    {
        return Exclude ? Array.Empty<ValidationResult>() : _topicConfigurator.Validate();
    }

    /// <summary>Builds sender settings and topic topology for the message contract.</summary>
    /// <returns>The topic send settings.</returns>
    public SendSettings GetSendSettings()
    {
        var createTopicOptions = _topicConfigurator.GetCreateTopicOptions();

        var builder = new PublishEndpointBrokerTopologyBuilder(_publishTopology);

        Apply(builder);

        return new TopicSendSettings(createTopicOptions, builder.BuildBrokerTopology());
    }

    /// <summary>Creates a subscription configurator bound to the message topic.</summary>
    /// <param name="subscriptionName">The subscription name.</param>
    /// <returns>The subscription configurator with a bounded Azure name.</returns>
    public ServiceBusSubscriptionConfigurator GetSubscriptionConfigurator(string subscriptionName)
    {
        return new ServiceBusSubscriptionConfigurator(_publishTopology.FormatSubscriptionName(subscriptionName), CreateTopicOptions.Name);
    }

    /// <summary>Gets the topic path before an optional base path is applied.</summary>
    public string Path => _topicConfigurator.Path;

    /// <summary>Gets or sets the optional namespace-relative prefix applied to the topic path.</summary>
    public string? BasePath
    {
        get => _topicConfigurator.BasePath;
        set
        {
            EnsureTopicOptionMutable(nameof(BasePath), _topicConfigurator.BasePath, value);
            _topicConfigurator.BasePath = value;
        }
    }

    /// <summary>Gets the topic path including its optional base path.</summary>
    public string FullPath => _topicConfigurator.FullPath;

    /// <summary>Sets how long the topic retains message identifiers for duplicate detection.</summary>
    public TimeSpan? DuplicateDetectionHistoryTimeWindow
    {
        set
        {
            EnsureTopicOptionMutable(nameof(DuplicateDetectionHistoryTimeWindow), _topicConfigurator.DuplicateDetectionHistoryTimeWindow, value);
            _topicConfigurator.DuplicateDetectionHistoryTimeWindow = value;
        }
    }

    /// <summary>Enables or disables topic partitioning.</summary>
    public bool? EnablePartitioning
    {
        set
        {
            EnsureTopicOptionMutable(nameof(EnablePartitioning), _topicConfigurator.EnablePartitioning, value);
            _topicConfigurator.EnablePartitioning = value;
        }
    }

    /// <summary>Sets the maximum topic size in megabytes.</summary>
    public long? MaxSizeInMegabytes
    {
        set
        {
            EnsureTopicOptionMutable(nameof(MaxSizeInMegabytes), _topicConfigurator.MaxSizeInMegabytes, value);
            _topicConfigurator.MaxSizeInMegabytes = value;
        }
    }

    /// <summary>Sets the maximum individual message size in kilobytes.</summary>
    public long? MaxMessageSizeInKilobytes
    {
        set
        {
            EnsureTopicOptionMutable(nameof(MaxMessageSizeInKilobytes), _topicConfigurator.MaxMessageSizeInKilobytes, value);
            _topicConfigurator.MaxMessageSizeInKilobytes = value;
        }
    }

    /// <summary>Enables or disables duplicate detection.</summary>
    public bool? RequiresDuplicateDetection
    {
        set
        {
            EnsureTopicOptionMutable(nameof(RequiresDuplicateDetection), _topicConfigurator.RequiresDuplicateDetection, value);
            _topicConfigurator.RequiresDuplicateDetection = value;
        }
    }

    /// <summary>Enables or disables broker ordering support.</summary>
    public bool? SupportOrdering
    {
        set
        {
            EnsureTopicOptionMutable(nameof(SupportOrdering), _topicConfigurator.SupportOrdering, value);
            _topicConfigurator.SupportOrdering = value;
        }
    }

    /// <summary>Enables duplicate detection and sets the identifier-retention window.</summary>
    /// <param name="historyTimeWindow">How long the broker retains message identifiers.</param>
    public void EnableDuplicateDetection(TimeSpan historyTimeWindow)
    {
        EnsureTopicOptionMutable(nameof(RequiresDuplicateDetection), _topicConfigurator.RequiresDuplicateDetection, true);
        EnsureTopicOptionMutable(nameof(DuplicateDetectionHistoryTimeWindow), _topicConfigurator.DuplicateDetectionHistoryTimeWindow,
            (TimeSpan?)historyTimeWindow);
        _topicConfigurator.EnableDuplicateDetection(historyTimeWindow);
    }

    /// <summary>Sets the idle interval after which the topic is deleted.</summary>
    public TimeSpan? AutoDeleteOnIdle
    {
        set
        {
            EnsureTopicOptionMutable(nameof(AutoDeleteOnIdle), _topicConfigurator.AutoDeleteOnIdle, value);
            _topicConfigurator.AutoDeleteOnIdle = value;
        }
    }

    /// <summary>Sets the default time to live for messages sent to the topic.</summary>
    public TimeSpan? DefaultMessageTimeToLive
    {
        set
        {
            EnsureTopicOptionMutable(nameof(DefaultMessageTimeToLive), _topicConfigurator.DefaultMessageTimeToLive, value);
            _topicConfigurator.DefaultMessageTimeToLive = value;
        }
    }

    /// <summary>Enables or disables broker-side batching.</summary>
    public bool? EnableBatchedOperations
    {
        set
        {
            EnsureTopicOptionMutable(nameof(EnableBatchedOperations), _topicConfigurator.EnableBatchedOperations, value);
            _topicConfigurator.EnableBatchedOperations = value;
        }
    }

    /// <summary>Sets provider metadata stored with the topic.</summary>
    public string UserMetadata
    {
        set
        {
            EnsureTopicOptionMutable(nameof(UserMetadata), _topicConfigurator.UserMetadata, value);
            _topicConfigurator.UserMetadata = value;
        }
    }

    /// <summary>Adds this topic and direct implemented-message topics to a publish topology builder.</summary>
    /// <param name="builder">The publish topology builder.</param>
    public void Apply(IPublishEndpointBrokerTopologyBuilder builder)
    {
        if (Exclude)
            return;

        var topicHandle = builder.CreateTopic(_createTopicOptions.Value);

        builder.Topic = topicHandle;

        foreach (var configurator in _implementedMessageTypes)
            configurator.Apply(builder);
    }

    /// <summary>Registers an implemented message contract whose topic may be connected directly.</summary>
    /// <typeparam name="T">The implemented message contract.</typeparam>
    /// <param name="configurator">The implemented contract's publish topology.</param>
    /// <param name="direct">Whether the relationship should be applied directly.</param>
    public void AddImplementedMessageConfigurator<T>(IServiceBusMessagePublishTopologyConfigurator<T> configurator, bool direct)
        where T : class
    {
        var adapter = new ImplementedTypeAdapter<T>(configurator, direct);

        _implementedMessageTypes.Add(adapter);
    }

    void EnsureTopicOptionMutable<T>(string optionName, T currentValue, T newValue)
    {
        if (_createTopicOptions.IsValueCreated && !EqualityComparer<T>.Default.Equals(currentValue, newValue))
            throw new InvalidOperationException($"Azure Service Bus topic options were already evaluated; {optionName} cannot change.");
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
