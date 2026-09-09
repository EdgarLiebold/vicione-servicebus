using System;
using System.Data.Common;
using System.Runtime.Serialization;
using System.Security;
using System.Text.Json;

namespace ViciOne.ServiceBus.RetryPolicies;

/// <summary>
/// Conservatively classifies technical failures. Only failures with an explicit transient contract are
/// retried; structural, programming, security and serialization failures are terminal.
/// </summary>
public sealed class DefaultTechnicalFailureClassifier : ITechnicalFailureClassifier
{
    /// <inheritdoc />
    public RetryFailureKind Classify(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        if (exception is AggregateException aggregateException)
            return ClassifyAggregate(aggregateException.Flatten());

        RetryFailureKind direct = ClassifySingle(exception);
        if (direct == RetryFailureKind.NonRetryable || exception.InnerException is null)
            return direct;

        RetryFailureKind inner = Classify(exception.InnerException);
        if (inner == RetryFailureKind.NonRetryable)
            return inner;

        return direct == RetryFailureKind.Transient ? direct : inner;
    }

    RetryFailureKind ClassifyAggregate(AggregateException exception)
    {
        var sawTransient = false;
        var sawUnclassified = false;

        foreach (Exception innerException in exception.InnerExceptions)
        {
            RetryFailureKind kind = Classify(innerException);
            if (kind == RetryFailureKind.NonRetryable)
                return RetryFailureKind.NonRetryable;

            if (kind == RetryFailureKind.Transient)
                sawTransient = true;
            else
                sawUnclassified = true;
        }

        if (sawUnclassified)
            return RetryFailureKind.Unclassified;

        return sawTransient ? RetryFailureKind.Transient : RetryFailureKind.Unclassified;
    }

    static RetryFailureKind ClassifySingle(Exception exception)
    {
        if (exception is IRetryFailureClassification classified)
        {
            return classified.RetryFailureKind switch
            {
                RetryFailureKind.Transient => RetryFailureKind.Transient,
                RetryFailureKind.NonRetryable => RetryFailureKind.NonRetryable,
                _ => RetryFailureKind.Unclassified,
            };
        }

        return exception switch
        {
            ConnectionException connectionException => connectionException.IsTransient
                ? RetryFailureKind.Transient
                : RetryFailureKind.NonRetryable,
            ConcurrencyException => RetryFailureKind.Transient,
            TransportUnavailableException => RetryFailureKind.Transient,
            TimeoutException => RetryFailureKind.Transient,
            CircuitBreakerOpenException => RetryFailureKind.Transient,
            InvalidOperationException { InnerException: DbException { IsTransient: true } } => RetryFailureKind.Transient,
            DbException dbException => dbException.IsTransient
                ? RetryFailureKind.Transient
                : RetryFailureKind.Unclassified,

            OperationCanceledException => RetryFailureKind.NonRetryable,
            ConfigurationException => RetryFailureKind.NonRetryable,
            PipeConfigurationException => RetryFailureKind.NonRetryable,
            MessageException => RetryFailureKind.NonRetryable,
            PayloadException => RetryFailureKind.NonRetryable,
            UnknownStateException => RetryFailureKind.NonRetryable,
            UnknownEventException => RetryFailureKind.NonRetryable,
            UnhandledEventException => RetryFailureKind.NonRetryable,
            SerializationException => RetryFailureKind.NonRetryable,
            JsonException => RetryFailureKind.NonRetryable,
            SecurityException => RetryFailureKind.NonRetryable,
            UnauthorizedAccessException => RetryFailureKind.NonRetryable,
            ArgumentException => RetryFailureKind.NonRetryable,
            ObjectDisposedException => RetryFailureKind.NonRetryable,
            InvalidOperationException => RetryFailureKind.NonRetryable,
            NotSupportedException => RetryFailureKind.NonRetryable,
            NotImplementedException => RetryFailureKind.NonRetryable,
            NullReferenceException => RetryFailureKind.NonRetryable,
            IndexOutOfRangeException => RetryFailureKind.NonRetryable,
            FormatException => RetryFailureKind.NonRetryable,
            OverflowException => RetryFailureKind.NonRetryable,
            _ => RetryFailureKind.Unclassified,
        };
    }
}
