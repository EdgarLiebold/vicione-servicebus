using System;

namespace ViciOne.ServiceBus;

/// <summary>Provides a diagnostic exception base for failures associated with a saga instance or message.</summary>
public class SagaException :
    ViciOneServiceBusException
{
    /// <summary>Creates a saga exception without typed saga context.</summary>
    protected SagaException()
    {
    }

    /// <summary>Creates an exception for the specified saga instance.</summary>
    /// <param name="message">The description of the saga failure.</param>
    /// <param name="sagaType">The runtime saga type used by the operation.</param>
    /// <param name="correlationId">The identifier of the saga instance.</param>
    public SagaException(string message, Type sagaType, Guid correlationId)
        : base(FormatMessage(sagaType, correlationId, message))
    {
        SagaType = sagaType;
        CorrelationId = correlationId;
    }

    /// <summary>Creates an exception for the specified saga instance and underlying failure.</summary>
    /// <param name="message">The description of the saga failure.</param>
    /// <param name="sagaType">The runtime saga type used by the operation.</param>
    /// <param name="correlationId">The identifier of the saga instance.</param>
    /// <param name="innerException">The exception raised while processing the saga.</param>
    public SagaException(string message, Type sagaType, Guid correlationId, Exception innerException)
        : base(FormatMessage(sagaType, correlationId, message), innerException)
    {
        SagaType = sagaType;
        CorrelationId = correlationId;
    }

    /// <summary>Creates an exception for a message processed by the specified saga instance.</summary>
    /// <param name="message">The description of the saga failure.</param>
    /// <param name="sagaType">The runtime saga type used by the operation.</param>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <param name="correlationId">The identifier of the saga instance.</param>
    public SagaException(string message, Type sagaType, Type messageType, Guid correlationId)
        : base(FormatMessage(sagaType, correlationId, messageType, message))
    {
        SagaType = sagaType;
        MessageType = messageType;
        CorrelationId = correlationId;
    }

    /// <summary>Creates an exception for a message processed by the specified saga instance and underlying failure.</summary>
    /// <param name="message">The description of the saga failure.</param>
    /// <param name="sagaType">The runtime saga type used by the operation.</param>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <param name="correlationId">The identifier of the saga instance.</param>
    /// <param name="innerException">The exception raised while processing the saga message.</param>
    public SagaException(string message, Type sagaType, Type messageType, Guid correlationId, Exception innerException)
        : base(FormatMessage(sagaType, correlationId, messageType, message), innerException)
    {
        SagaType = sagaType;
        MessageType = messageType;
        CorrelationId = correlationId;
    }

    /// <summary>Creates an exception for a message associated with the specified saga type.</summary>
    /// <param name="message">The description of the saga failure.</param>
    /// <param name="sagaType">The runtime saga type used by the operation.</param>
    /// <param name="messageType">The runtime type of the message contract.</param>
    public SagaException(string message, Type sagaType, Type messageType)
        : base(FormatMessage(sagaType, messageType, message))
    {
        SagaType = sagaType;
        MessageType = messageType;
    }

    /// <summary>Gets the saga type associated with the failure, when one was supplied.</summary>
    public Type? SagaType { get; }

    /// <summary>Gets the message contract type associated with the failure, when one was supplied.</summary>
    public Type? MessageType { get; }

    /// <summary>Gets the saga instance identifier associated with the failure, when one was supplied.</summary>
    public Guid? CorrelationId { get; }

    static string FormatMessage(Type sagaType, Type messageType, string message)
    {
        ArgumentNullException.ThrowIfNull(sagaType);
        ArgumentNullException.ThrowIfNull(messageType);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        return $"{TypeCache.GetShortName(sagaType)} Saga exception on receipt of {TypeCache.GetShortName(messageType)}: {message}";
    }

    static string FormatMessage(Type sagaType, Guid correlationId, string message)
    {
        ArgumentNullException.ThrowIfNull(sagaType);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        return $"{TypeCache.GetShortName(sagaType)}({correlationId}) Saga exception: {message}";
    }

    static string FormatMessage(Type sagaType, Guid correlationId, Type messageType, string message)
    {
        ArgumentNullException.ThrowIfNull(sagaType);
        ArgumentNullException.ThrowIfNull(messageType);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        return
            $"{TypeCache.GetShortName(sagaType)}({correlationId}) Saga exception on receipt of {TypeCache.GetShortName(messageType)}: {message}";
    }
}
