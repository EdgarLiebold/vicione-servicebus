using System;
using Microsoft.Extensions.Logging;
using ViciOne.ServiceBus.Logging;

namespace ViciOne.ServiceBus.Transports;

/// <summary>Defines strongly typed log messages emitted by transport components.</summary>
public static class TransportLogMessages
{
    /// <summary>Gets the template emitted before a broker host connection begins.</summary>
    public static LogMessage<string> ConnectHost { get; } = LogContext.Define<string>(LogLevel.Debug,
        "Connect: {Host}");

    /// <summary>Gets the template emitted after a broker host connection succeeds.</summary>
    public static LogMessage<string> ConnectedHost { get; } = LogContext.Define<string>(LogLevel.Debug,
        "Connected: {Host}");

    /// <summary>Gets the template emitted before a broker host disconnect begins.</summary>
    public static LogMessage<string> DisconnectHost { get; } = LogContext.Define<string>(LogLevel.Debug,
        "Disconnect: {Host}");

    /// <summary>Gets the template emitted after a broker host disconnect completes.</summary>
    public static LogMessage<string> DisconnectedHost { get; } = LogContext.Define<string>(LogLevel.Debug,
        "Disconnected: {Host}");

    /// <summary>Gets the template emitted when a send transport begins stopping.</summary>
    public static LogMessage<string> StoppingSendTransport { get; } = LogContext.Define<string>(LogLevel.Debug,
        "Send Transport Stopping: {Destination}");

    /// <summary>Gets the template emitted when a receive endpoint is connected.</summary>
    public static LogMessage<Uri> ConnectReceiveEndpoint { get; } = LogContext.Define<Uri>(LogLevel.Debug,
        "Connect receive endpoint: {InputAddress}");

    /// <summary>Gets the template emitted when a subscription endpoint is connected.</summary>
    public static LogMessage<Uri, string> ConnectSubscriptionEndpoint { get; } = LogContext.Define<Uri, string>(LogLevel.Debug,
        "Connect subscription endpoint: {InputAddress}({SubscriptionName})");

    /// <summary>Gets the template emitted when a receive transport is created.</summary>
    public static LogMessage<Uri> CreateReceiveTransport { get; } = LogContext.Define<Uri>(LogLevel.Debug,
        "Create receive transport: {InputAddress}");

    /// <summary>Gets the template emitted when a publish transport is created.</summary>
    public static LogMessage<Uri> CreatePublishTransport { get; } = LogContext.Define<Uri>(LogLevel.Debug,
        "Create publish transport: {DestinationAddress}");

    /// <summary>Gets the template emitted when an exchange is created.</summary>
    public static LogMessage<string> CreateExchange { get; } = LogContext.Define<string>(LogLevel.Debug,
        "Create exchange: {Exchange}");

    /// <summary>Gets the template emitted when a queue is created.</summary>
    public static LogMessage<string> CreateQueue { get; } = LogContext.Define<string>(LogLevel.Debug,
        "Create queue: {Queue}");

    /// <summary>Gets the template emitted when a topic is created.</summary>
    public static LogMessage<string> CreateTopic { get; } = LogContext.Define<string>(LogLevel.Debug,
        "Create topic: {Topic}");

    /// <summary>Gets the template emitted when a send transport is created.</summary>
    public static LogMessage<Uri> CreateSendTransport { get; } = LogContext.Define<Uri>(LogLevel.Debug,
        "Create send transport: {DestinationAddress}");

    /// <summary>Gets the template emitted when a queue is deleted.</summary>
    public static LogMessage<string> DeleteQueue { get; } = LogContext.Define<string>(LogLevel.Debug,
        "Delete queue: {Queue}");

    /// <summary>Gets the template emitted when a subscription is deleted.</summary>
    public static LogMessage<string, string> DeleteSubscription { get; } = LogContext.Define<string, string>(LogLevel.Debug,
        "Delete subscription: {Queue} {Subscription}");

    /// <summary>Gets the template emitted when a topic is deleted.</summary>
    public static LogMessage<string> DeleteTopic { get; } = LogContext.Define<string>(LogLevel.Debug,
        "Delete topic: {Topic}");
}
