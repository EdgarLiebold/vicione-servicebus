using System;
using ViciOne.ServiceBus.Azure.Table.Infrastructure;

namespace ViciOne.ServiceBus.Azure.Table;

/// <summary>Indicates that Azure Table rejected a saga write because its persistence state changed concurrently.</summary>
public sealed class AzureTableSagaConcurrencyException :
    ConcurrencyException
{
    /// <summary>Creates a concurrency exception for the conflicting saga identity.</summary>
    /// <param name="message">A description of the concurrency conflict.</param>
    /// <param name="sagaType">The saga state type involved in the conflict.</param>
    /// <param name="correlationId">The conflicting saga correlation identifier.</param>
    /// <exception cref="ArgumentNullException"><paramref name="message"/> or <paramref name="sagaType"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="message"/> is blank or <paramref name="correlationId"/> is empty.</exception>
    public AzureTableSagaConcurrencyException(string message, Type sagaType, Guid correlationId)
        : base(
            ValidateMessage(message),
            ValidateSagaType(sagaType),
            AzureTableKeyValidator.ValidateCorrelationId(correlationId, nameof(correlationId)))
    {
    }

    /// <summary>Creates a concurrency exception that retains the rejected Azure Table operation error.</summary>
    /// <param name="message">A description of the concurrency conflict.</param>
    /// <param name="sagaType">The saga state type involved in the conflict.</param>
    /// <param name="correlationId">The conflicting saga correlation identifier.</param>
    /// <param name="innerException">The concurrency failure reported by Azure Table.</param>
    /// <exception cref="ArgumentNullException"><paramref name="message"/>, <paramref name="sagaType"/>, or <paramref name="innerException"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="message"/> is blank or <paramref name="correlationId"/> is empty.</exception>
    public AzureTableSagaConcurrencyException(
        string message,
        Type sagaType,
        Guid correlationId,
        Exception innerException)
        : base(
            ValidateMessage(message),
            ValidateSagaType(sagaType),
            AzureTableKeyValidator.ValidateCorrelationId(correlationId, nameof(correlationId)),
            innerException ?? throw new ArgumentNullException(nameof(innerException)))
    {
    }

    private static string ValidateMessage(string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        return message;
    }

    private static Type ValidateSagaType(Type sagaType)
    {
        ArgumentNullException.ThrowIfNull(sagaType);
        return sagaType;
    }
}
