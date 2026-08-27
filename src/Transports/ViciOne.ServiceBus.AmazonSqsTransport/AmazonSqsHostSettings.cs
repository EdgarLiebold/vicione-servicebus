namespace ViciOne.ServiceBus;

using System;
using Amazon;
using AmazonSqsTransport;
using Transports;


/// <summary>
/// Settings to configure a AmazonSQS host explicitly without requiring the fluent interface
/// </summary>
public interface AmazonSqsHostSettings
{
    /// <summary>
    /// The AmazonSQS region to connect
    /// </summary>
    RegionEndpoint? Region { get; }

    AllowTransportHeader? AllowTransportHeader { get; }

    /// <summary>
    /// If true, topics are named "{Scope}_{topicName}" when publishing messages
    /// </summary>
    bool ScopeTopics { get; }

    Uri HostAddress { get; }

    IConnection CreateConnection();
}
