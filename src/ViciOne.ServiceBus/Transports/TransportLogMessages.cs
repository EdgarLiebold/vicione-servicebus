using System;
using Microsoft.Extensions.Logging;
using ViciOne.ServiceBus.Logging;

namespace ViciOne.ServiceBus.Transports;

/// <summary>Defines strongly typed log messages emitted by transport components.</summary>
public static class TransportLogMessages
{
    /// <summary>Exposes the connect host used by the containing type.</summary>
    public static readonly LogMessage<string> ConnectHost = LogContext.Define<string>(LogLevel.Debug,
        "Connect: {Host}");

    /// <summary>Exposes the connected host used by the containing type.</summary>
    public static readonly LogMessage<string> ConnectedHost = LogContext.Define<string>(LogLevel.Debug,
        "Connected: {Host}");

    /// <summary>Exposes the disconnect host used by the containing type.</summary>
    public static readonly LogMessage<string> DisconnectHost = LogContext.Define<string>(LogLevel.Debug,
        "Disconnect: {Host}");

    /// <summary>Exposes the disconnected host used by the containing type.</summary>
    public static readonly LogMessage<string> DisconnectedHost = LogContext.Define<string>(LogLevel.Debug,
        "Disconnected: {Host}");

    /// <summary>Exposes the stopping send transport used by the containing type.</summary>
    public static readonly LogMessage<string> StoppingSendTransport = LogContext.Define<string>(LogLevel.Debug,
        "Send Transport Stopping: {Destination}");

    /// <summary>Exposes the connect receive endpoint used by the containing type.</summary>
    public static readonly LogMessage<Uri> ConnectReceiveEndpoint = LogContext.Define<Uri>(LogLevel.Debug,
        "Connect receive endpoint: {InputAddress}");

    /// <summary>Exposes the connect subscription endpoint used by the containing type.</summary>
    public static readonly LogMessage<Uri, string> ConnectSubscriptionEndpoint = LogContext.Define<Uri, string>(LogLevel.Debug,
        "Connect subscription endpoint: {InputAddress}({SubscriptionName})");

    /// <summary>Exposes the create receive transport used by the containing type.</summary>
    public static readonly LogMessage<Uri> CreateReceiveTransport = LogContext.Define<Uri>(LogLevel.Debug,
        "Create receive transport: {InputAddress}");

    /// <summary>Exposes the create publish transport used by the containing type.</summary>
    public static readonly LogMessage<Uri> CreatePublishTransport = LogContext.Define<Uri>(LogLevel.Debug,
        "Create publish transport: {DestinationAddress}");

    /// <summary>Exposes the create exchange used by the containing type.</summary>
    public static readonly LogMessage<string> CreateExchange = LogContext.Define<string>(LogLevel.Debug,
        "Create exchange: {Exchange}");

    /// <summary>Exposes the create queue used by the containing type.</summary>
    public static readonly LogMessage<string> CreateQueue = LogContext.Define<string>(LogLevel.Debug,
        "Create queue: {Queue}");

    /// <summary>Exposes the create topic used by the containing type.</summary>
    public static readonly LogMessage<string> CreateTopic = LogContext.Define<string>(LogLevel.Debug,
        "Create topic: {Topic}");

    /// <summary>Exposes the create send transport used by the containing type.</summary>
    public static readonly LogMessage<Uri> CreateSendTransport = LogContext.Define<Uri>(LogLevel.Debug,
        "Create send transport: {DestinationAddress}");

    /// <summary>Exposes the delete queue used by the containing type.</summary>
    public static readonly LogMessage<string> DeleteQueue = LogContext.Define<string>(LogLevel.Debug,
        "Delete queue: {Queue}");

    /// <summary>Exposes the delete subscription used by the containing type.</summary>
    public static readonly LogMessage<string, string> DeleteSubscription = LogContext.Define<string, string>(LogLevel.Debug,
        "Delete subscription: {Queue} {Subscription}");

    /// <summary>Exposes the delete topic used by the containing type.</summary>
    public static readonly LogMessage<string> DeleteTopic = LogContext.Define<string>(LogLevel.Debug,
        "Delete topic: {Topic}");
}
