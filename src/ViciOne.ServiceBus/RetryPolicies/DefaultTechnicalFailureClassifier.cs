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
internal sealed class DefaultTechnicalFailureClassifier : ITechnicalFailureClassifier
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
            ConcurrencyException or TransportUnavailableException or TimeoutException or CircuitBreakerOpenException
                => RetryFailureKind.Transient,
            InvalidOperationException { InnerException: DbException { IsTransient: true } } => RetryFailureKind.Transient,
            DbException dbException => dbException.IsTransient
                ? RetryFailureKind.Transient
                : RetryFailureKind.Unclassified,

            _ when IsNonRetryable(exception) => RetryFailureKind.NonRetryable,
            _ => RetryFailureKind.Unclassified,
        };
    }

    static bool IsNonRetryable(Exception exception)
    {
        return exception is OperationCanceledException
            || IsContractFailure(exception)
            || IsSerializationOrSecurityFailure(exception)
            || IsProgrammingFailure(exception);
    }

    static bool IsContractFailure(Exception exception)
    {
        return exception is ConfigurationException or PipeConfigurationException or MessageException
            or PayloadException or UnknownStateException or UnknownEventException or UnhandledEventException;
    }

    static bool IsSerializationOrSecurityFailure(Exception exception)
    {
        return exception is SerializationException or JsonException or SecurityException or UnauthorizedAccessException;
    }

    static bool IsProgrammingFailure(Exception exception)
    {
        return exception is ArgumentException or ObjectDisposedException or InvalidOperationException
            or NotSupportedException or NotImplementedException or NullReferenceException
            or IndexOutOfRangeException or FormatException or OverflowException;
    }
}
