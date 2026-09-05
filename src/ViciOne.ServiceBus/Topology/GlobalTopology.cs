using System;
using System.Collections.Generic;
using System.Text.Json;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Topology;

namespace ViciOne.ServiceBus.Advanced.Topology;

/// <summary>
/// Application-wide message-contract conventions. Configuration is frozen when the first
/// runtime topology consumes it; bus-specific policy must never be stored here.
/// </summary>
internal sealed class GlobalTopology
{
    readonly object _lock = new();
    readonly HashSet<Type> _notConsumableMessageTypes;
    readonly IPublishTopologyConfigurator _publish;
    readonly ConnectHandle _publishToSendHandle;
    readonly ISendTopologyConfigurator _send;
    bool _frozen;

    GlobalTopology()
    {
        _send = new SendTopology();
        _send.TryAddConvention(new CorrelationIdSendTopologyConvention());

        _publish = new PublishTopology();
        _notConsumableMessageTypes = [typeof(JsonElement)];

        var observer = new PublishToSendTopologyConfigurationObserver(_send);
        _publishToSendHandle = _publish.ConnectPublishTopologyConfigurationObserver(observer);

    }

    internal static ISendTopology Send => Cached.Instance.GetSendTopology();
    internal static IPublishTopologyConfigurator Publish => Cached.Instance.GetPublishTopology();

    internal static void UseCorrelationId<T>(Func<T, Guid> correlationIdSelector)
        where T : class
    {
        Cached.Instance.Configure(() => Cached.Instance._send.UseCorrelationId(correlationIdSelector));
    }

    internal static void UseCorrelationId<T>(Func<T, Guid?> correlationIdSelector)
        where T : class
    {
        Cached.Instance.Configure(() => Cached.Instance._send.UseCorrelationId(correlationIdSelector));
    }

    internal static void UseCapabilityCorrelationId<T>(Func<T, Guid> correlationIdSelector)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(correlationIdSelector);
        Cached.Instance.ConfigureCapability(() => Cached.Instance._send.UseCorrelationId(correlationIdSelector));
    }

    internal static void UseCapabilityCorrelationId<T>(Func<T, Guid?> correlationIdSelector)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(correlationIdSelector);
        Cached.Instance.ConfigureCapability(() => Cached.Instance._send.UseCorrelationId(correlationIdSelector));
    }

    internal static void MarkMessageTypeNotConsumable(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        Cached.Instance.Configure(() => Cached.Instance._notConsumableMessageTypes.Add(type));
    }

    internal static bool IsConsumableMessageType(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        GlobalTopology instance = Cached.Instance;
        instance.Freeze();
        lock (instance._lock)
            return !instance._notConsumableMessageTypes.Contains(type);
    }

    internal static void SeparatePublishFromSend()
    {
        Cached.Instance.Configure(() => Cached.Instance._publishToSendHandle.Disconnect());
    }

    ISendTopology GetSendTopology()
    {
        Freeze();
        return _send;
    }

    IPublishTopologyConfigurator GetPublishTopology()
    {
        Freeze();
        return _publish;
    }

    void Configure(Action configure)
    {
        lock (_lock)
        {
            if (_frozen)
                throw new InvalidOperationException("Application message conventions are immutable after the first bus topology is created.");

            configure();
        }
    }

    void ConfigureCapability(Action configure)
    {
        lock (_lock)
            configure();
    }

    void Freeze()
    {
        lock (_lock)
            _frozen = true;
    }

    static class Cached
    {
        internal static readonly GlobalTopology Instance = new();
    }
}
