using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Describes one closed job-consumer interface discovered on a consumer type.</summary>
internal sealed class JobInterfaceType :
    IMessageInterfaceType
{
    readonly Lazy<IMessageConnectorFactory> _consumeConnectorFactory;

    /// <summary>Creates a lazy connector factory for the discovered job and consumer types.</summary>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <param name="consumerType">The runtime consumer type used by the operation.</param>
    public JobInterfaceType(Type messageType, Type consumerType)
    {
        MessageType = messageType;

        _consumeConnectorFactory = new Lazy<IMessageConnectorFactory>(() => (IMessageConnectorFactory)
            (Activator.CreateInstance(typeof(JobMessageConnectorFactory<,>).MakeGenericType(consumerType, messageType)) ?? throw new System.InvalidOperationException("The requested runtime type could not be activated.")));
    }

    /// <summary>Gets the discovered job contract type.</summary>
    public Type MessageType { get; }

    /// <summary>Gets the connector for the requested consumer type.</summary>
    /// <typeparam name="T">The requested consumer type.</typeparam>
    /// <returns>The consumer connector.</returns>
    public IConsumerMessageConnector<T> GetConsumerConnector<T>()
        where T : class
    {
        return _consumeConnectorFactory.Value.CreateConsumerConnector<T>();
    }

    /// <summary>Delegates instance-connector creation, which job consumers do not support.</summary>
    /// <typeparam name="T">The requested instance type.</typeparam>
    /// <returns>The instance connector.</returns>
    public IInstanceMessageConnector<T> GetInstanceConnector<T>()
        where T : class
    {
        return _consumeConnectorFactory.Value.CreateInstanceConnector<T>();
    }
}
