using System;
using Microsoft.Extensions.Logging;
using ViciOne.ServiceBus.Logging;

namespace ViciOne.ServiceBus.Transports;

/// <summary>
/// Provides a transport log messages implementation.
/// </summary>
public static class TransportLogMessages
{
    /// <summary>
    /// Defines the connect host value.
    /// </summary>
    public static readonly LogMessage<string> ConnectHost = LogContext.Define<string>(LogLevel.Debug,
        "Connect: {Host}");

    /// <summary>
    /// Defines the connected host value.
    /// </summary>
    public static readonly LogMessage<string> ConnectedHost = LogContext.Define<string>(LogLevel.Debug,
        "Connected: {Host}");

    /// <summary>
    /// Defines the disconnect host value.
    /// </summary>
    public static readonly LogMessage<string> DisconnectHost = LogContext.Define<string>(LogLevel.Debug,
        "Disconnect: {Host}");

    /// <summary>
    /// Defines the disconnected host value.
    /// </summary>
    public static readonly LogMessage<string> DisconnectedHost = LogContext.Define<string>(LogLevel.Debug,
        "Disconnected: {Host}");

    /// <summary>
    /// Defines the stopping send transport value.
    /// </summary>
    public static readonly LogMessage<string> StoppingSendTransport = LogContext.Define<string>(LogLevel.Debug,
        "Send Transport Stopping: {Destination}");

    /// <summary>
    /// Defines the connect receive endpoint value.
    /// </summary>
    public static readonly LogMessage<Uri> ConnectReceiveEndpoint = LogContext.Define<Uri>(LogLevel.Debug,
        "Connect receive endpoint: {InputAddress}");

    /// <summary>
    /// Defines the connect subscription endpoint value.
    /// </summary>
    public static readonly LogMessage<Uri, string> ConnectSubscriptionEndpoint = LogContext.Define<Uri, string>(LogLevel.Debug,
        "Connect subscription endpoint: {InputAddress}({SubscriptionName})");

    /// <summary>
    /// Defines the create receive transport value.
    /// </summary>
    public static readonly LogMessage<Uri> CreateReceiveTransport = LogContext.Define<Uri>(LogLevel.Debug,
        "Create receive transport: {InputAddress}");

    /// <summary>
    /// Defines the create publish transport value.
    /// </summary>
    public static readonly LogMessage<Uri> CreatePublishTransport = LogContext.Define<Uri>(LogLevel.Debug,
        "Create publish transport: {DestinationAddress}");

    /// <summary>
    /// Defines the create exchange value.
    /// </summary>
    public static readonly LogMessage<string> CreateExchange = LogContext.Define<string>(LogLevel.Debug,
        "Create exchange: {Exchange}");

    /// <summary>
    /// Defines the create queue value.
    /// </summary>
    public static readonly LogMessage<string> CreateQueue = LogContext.Define<string>(LogLevel.Debug,
        "Create queue: {Queue}");

    /// <summary>
    /// Defines the create topic value.
    /// </summary>
    public static readonly LogMessage<string> CreateTopic = LogContext.Define<string>(LogLevel.Debug,
        "Create topic: {Topic}");

    /// <summary>
    /// Defines the create send transport value.
    /// </summary>
    public static readonly LogMessage<Uri> CreateSendTransport = LogContext.Define<Uri>(LogLevel.Debug,
        "Create send transport: {DestinationAddress}");

    /// <summary>
    /// Defines the delete queue value.
    /// </summary>
    public static readonly LogMessage<string> DeleteQueue = LogContext.Define<string>(LogLevel.Debug,
        "Delete queue: {Queue}");

    /// <summary>
    /// Defines the delete subscription value.
    /// </summary>
    public static readonly LogMessage<string, string> DeleteSubscription = LogContext.Define<string, string>(LogLevel.Debug,
        "Delete subscription: {Queue} {Subscription}");

    /// <summary>
    /// Defines the delete topic value.
    /// </summary>
    public static readonly LogMessage<string> DeleteTopic = LogContext.Define<string>(LogLevel.Debug,
        "Delete topic: {Topic}");
}
