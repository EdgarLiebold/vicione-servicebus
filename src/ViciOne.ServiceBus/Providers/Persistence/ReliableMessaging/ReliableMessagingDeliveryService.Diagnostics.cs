using System;
using Microsoft.Extensions.Logging;

namespace ViciOne.ServiceBus.Providers.Persistence;

internal sealed partial class ReliableMessagingDeliveryService<TBus>
    where TBus : class, IBus
{
    void TryLogRetryScheduled(
        Guid durableSendId,
        int attempt,
        int maximumAttempts,
        double delaySeconds,
        string? failureType)
    {
        try
        {
            LogRetryScheduled(durableSendId, attempt, maximumAttempts, delaySeconds, failureType);
        }
        catch
        {
            // Logging is an observer and cannot change delivery semantics.
        }
    }

    void TryLogQuarantinedNonRetryable(Guid durableSendId, string contractIdentity, string? failureType)
    {
        try
        {
            LogQuarantinedNonRetryable(durableSendId, contractIdentity, failureType);
        }
        catch
        {
        }
    }

    void TryLogQuarantined(Guid durableSendId, int attempt, DurableSendFailureKind failureKind, string? failureType)
    {
        try
        {
            LogQuarantined(durableSendId, attempt, failureKind, failureType);
        }
        catch
        {
        }
    }

    void TryLogClassifierFailed(string classifierType, string classifierFailureType)
    {
        try
        {
            LogClassifierFailed(classifierType, classifierFailureType);
        }
        catch
        {
        }
    }

    void TryLogStatePersistenceFailed(Guid durableSendId, string dispatchOutcome, string persistenceFailureType)
    {
        try
        {
            LogStatePersistenceFailed(durableSendId, dispatchOutcome, persistenceFailureType);
        }
        catch
        {
        }
    }

    void TryLogTelemetrySnapshotFailed(string failureType)
    {
        try
        {
            LogTelemetrySnapshotFailed(failureType);
        }
        catch
        {
        }
    }

    void TryLogConsumerCompletionTimeout(Guid durableSendId, int attempts, int maximumAttempts)
    {
        try
        {
            LogConsumerCompletionTimeout(durableSendId, attempts, maximumAttempts);
        }
        catch
        {
        }
    }

    void TryLogInvalidCompletionMode(Guid durableSendId, int completionMode)
    {
        try
        {
            LogInvalidCompletionMode(durableSendId, completionMode);
        }
        catch
        {
        }
    }

    [LoggerMessage(5001, LogLevel.Debug,
        "Durable send {DurableSendId} scheduled for retry {Attempt}/{MaximumAttempts} after {DelaySeconds}s ({FailureType}).")]
    partial void LogRetryScheduled(Guid durableSendId, int attempt, int maximumAttempts, double delaySeconds, string? failureType);

    [LoggerMessage(5002, LogLevel.Warning,
        "Durable send {DurableSendId} ({ContractIdentity}) was quarantined after a non-retryable transport failure ({FailureType}).")]
    partial void LogQuarantinedNonRetryable(Guid durableSendId, string contractIdentity, string? failureType);

    [LoggerMessage(5003, LogLevel.Warning,
        "Durable send {DurableSendId} was quarantined after attempt {Attempt}: {FailureKind} ({FailureType}).")]
    partial void LogQuarantined(Guid durableSendId, int attempt, DurableSendFailureKind failureKind, string? failureType);

    [LoggerMessage(5004, LogLevel.Warning,
        "Transport failure classifier {ClassifierType} threw {ClassifierFailureType} while classifying a durable send; its result was ignored.")]
    partial void LogClassifierFailed(string classifierType, string classifierFailureType);

    [LoggerMessage(5005, LogLevel.Error,
        "Durable send {DurableSendId} could not persist delivery state after {DispatchOutcome}; durable-state persistence failed with {PersistenceFailureType}.")]
    partial void LogStatePersistenceFailed(Guid durableSendId, string dispatchOutcome, string persistenceFailureType);

    [LoggerMessage(5006, LogLevel.Debug,
        "Durable sender telemetry snapshot refresh failed with {FailureType}; delivery semantics are unaffected.")]
    partial void LogTelemetrySnapshotFailed(string failureType);

    [LoggerMessage(5007, LogLevel.Warning,
        "Durable send {DurableSendId} was quarantined after {Attempts}/{MaximumAttempts} volatile delivery attempts without consumer completion.")]
    partial void LogConsumerCompletionTimeout(Guid durableSendId, int attempts, int maximumAttempts);

    [LoggerMessage(5008, LogLevel.Error,
        "Durable send {DurableSendId} was quarantined because dispatcher returned unsupported completion mode {CompletionMode}.")]
    partial void LogInvalidCompletionMode(Guid durableSendId, int completionMode);
}
