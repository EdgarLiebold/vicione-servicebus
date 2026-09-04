using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ViciOne.ServiceBus.Operations;
/// <summary>
/// Readiness-oriented durable-sender health check. It reports durable backlog/quarantine state without
/// exposing payloads, destinations, message identifiers or exception messages.
/// </summary>
internal sealed class DurableSenderHealthCheck<TBus> : IHealthCheck
    where TBus : class, IBus
{
    readonly DurableSenderPolicy<TBus> _policy;
    readonly IDurableSendStore<TBus> _store;
    readonly TimeProvider _timeProvider;

    public DurableSenderHealthCheck(
        IEnumerable<IDurableSendStore<TBus>> stores,
        DurableSenderPolicy<TBus> policy,
        TimeProvider timeProvider)
    {
        _store = DurableSenderComposition.RequireExactlyOne<IDurableSendStore<TBus>, TBus>(stores, "persistence store");
        _policy = policy ?? throw new ArgumentNullException(nameof(policy));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        DurableSendStoreSnapshot snapshot;
        try
        {
            snapshot = await _store.GetSnapshotAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy(
                $"Durable sender store snapshot failed ({DiagnosticTypeName(exception.GetType())}).");
        }

        if (snapshot.StoredCount > _policy.Limits.MaximumStoredCount || snapshot.StoredBytes > _policy.Limits.MaximumStoredBytes)
        {
            return HealthCheckResult.Unhealthy(
                "Durable sender retained storage exceeded its configured hard bound.",
                data: Data(snapshot));
        }

        DateTimeOffset now = _timeProvider.GetUtcNow();
        bool stale = snapshot.OldestPendingEnqueuedAt is { } oldest && now - oldest >= _policy.HealthDegradedAfter;
        bool capacityExhausted = snapshot.StoredCount >= _policy.Limits.MaximumStoredCount
            || snapshot.StoredBytes >= _policy.Limits.MaximumStoredBytes;
        if (snapshot.QuarantinedCount > 0 || stale || capacityExhausted)
        {
            return HealthCheckResult.Degraded(
                snapshot.QuarantinedCount > 0
                    ? "Durable sender contains quarantined deliveries requiring operator attention."
                    : stale
                        ? "Durable sender backlog exceeded the configured health-age threshold."
                        : "Durable sender retained storage reached a configured hard capacity bound.",
                data: Data(snapshot));
        }

        return HealthCheckResult.Healthy("Durable sender is within configured reliability bounds.", Data(snapshot));
    }


    static string DiagnosticTypeName(Type type)
    {
        string name = type.FullName ?? type.Name;
        return name.Length <= 512 ? name : name[..512];
    }

    static IReadOnlyDictionary<string, object> Data(DurableSendStoreSnapshot snapshot)
        => new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["storedCount"] = snapshot.StoredCount,
            ["storedBytes"] = snapshot.StoredBytes,
            ["pendingCount"] = snapshot.PendingCount,
            ["retryScheduledCount"] = snapshot.RetryScheduledCount,
            ["awaitingConsumerCompletionCount"] = snapshot.AwaitingConsumerCompletionCount,
            ["quarantinedCount"] = snapshot.QuarantinedCount,
        };
}
