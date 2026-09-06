using System;
using System.Linq.Expressions;

namespace ViciOne.ServiceBus;

/// <summary>Represents an error related to saga.</summary>
public class SagaException :
    ViciOneServiceBusException
{
    /// <summary>Initializes a new instance.</summary>
    protected SagaException()
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="message">The message to process.</param>
    /// <param name="sagaType">The runtime saga type used by the operation.</param>
    /// <param name="correlationId">The correlation id.</param>
    public SagaException(string message, Type sagaType, Guid correlationId)
        : base(FormatMessage(sagaType, correlationId, message))
    {
        SagaType = sagaType;
        CorrelationId = correlationId;
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="message">The message to process.</param>
    /// <param name="sagaType">The runtime saga type used by the operation.</param>
    /// <param name="correlationId">The correlation id.</param>
    /// <param name="innerException">The inner exception.</param>
    public SagaException(string message, Type sagaType, Guid correlationId, Exception innerException)
        : base(FormatMessage(sagaType, correlationId, message), innerException)
    {
        SagaType = sagaType;
        CorrelationId = correlationId;
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="message">The message to process.</param>
    /// <param name="sagaType">The runtime saga type used by the operation.</param>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <param name="correlationId">The correlation id.</param>
    public SagaException(string message, Type sagaType, Type messageType, Guid correlationId)
        : base(FormatMessage(sagaType, correlationId, messageType, message))
    {
        SagaType = sagaType;
        MessageType = messageType;
        CorrelationId = correlationId;
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="message">The message to process.</param>
    /// <param name="sagaType">The runtime saga type used by the operation.</param>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <param name="findExpression">The find expression.</param>
    public SagaException(string message, Type sagaType, Type messageType, Expression findExpression)
        : base($"{sagaType.FullName} {message}({messageType.FullName}) - {findExpression}")
    {
        SagaType = sagaType;
        MessageType = messageType;
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="message">The message to process.</param>
    /// <param name="sagaType">The runtime saga type used by the operation.</param>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <param name="findExpression">The find expression.</param>
    /// <param name="innerException">The inner exception.</param>
    public SagaException(string message, Type sagaType, Type messageType, Expression findExpression, Exception innerException)
        : base($"{sagaType.FullName} {message}({messageType.FullName}) - {findExpression}", innerException)
    {
        SagaType = sagaType;
        MessageType = messageType;
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="message">The message to process.</param>
    /// <param name="sagaType">The runtime saga type used by the operation.</param>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <param name="correlationId">The correlation id.</param>
    /// <param name="innerException">The inner exception.</param>
    public SagaException(string message, Type sagaType, Type messageType, Guid correlationId, Exception innerException)
        : base(FormatMessage(sagaType, correlationId, messageType, message), innerException)
    {
        SagaType = sagaType;
        MessageType = messageType;
        CorrelationId = correlationId;
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="message">The message to process.</param>
    /// <param name="sagaType">The runtime saga type used by the operation.</param>
    /// <param name="messageType">The runtime type of the message contract.</param>
    public SagaException(string message, Type sagaType, Type messageType)
        : base(FormatMessage(sagaType, messageType, message))
    {
        SagaType = sagaType;
        MessageType = messageType;
        CorrelationId = Guid.Empty;
    }

    /// <summary>Gets the saga type.</summary>
    public Type? SagaType { get; }

    /// <summary>Gets the message type.</summary>
    public Type? MessageType { get; }

    /// <summary>Gets the correlation id.</summary>
    public Guid? CorrelationId { get; }

    static string FormatMessage(Type sagaType, Type messageType, string message)
    {
        return $"{TypeCache.GetShortName(sagaType)} Saga exception on receipt of {TypeCache.GetShortName(messageType)}: {message}";
    }

    static string FormatMessage(Type sagaType, Guid correlationId, string message)
    {
        return $"{TypeCache.GetShortName(sagaType)}({correlationId}) Saga exception: {message}";
    }

    static string FormatMessage(Type sagaType, Guid correlationId, Type messageType, string message)
    {
        return
            $"{TypeCache.GetShortName(sagaType)}({correlationId}) Saga exception on receipt of {TypeCache.GetShortName(messageType)}: {message}";
    }
}
