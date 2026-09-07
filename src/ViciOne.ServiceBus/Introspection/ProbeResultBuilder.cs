using System;
using System.Collections.Generic;
using System.Threading;
using ViciOne.ServiceBus.Metadata;

namespace ViciOne.ServiceBus.Introspection;

/// <summary>Collects a diagnostic probe and creates its structurally read-only result.</summary>
internal sealed class ProbeResultBuilder :
    ScopeProbeContext
{
    readonly Guid _probeId;
    readonly Guid _resultId;
    readonly DateTimeOffset _startTimestamp;
    readonly long _startedAt;
    readonly TimeProvider _timeProvider;

    /// <summary>Initializes a collector for one probe request.</summary>
    /// <param name="probeId">The probe id.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <param name="timeProvider">The time source used by the operation.</param>
    public ProbeResultBuilder(Guid probeId, CancellationToken cancellationToken, TimeProvider? timeProvider = null)
        : base(cancellationToken)
    {
        _probeId = probeId;
        _timeProvider = timeProvider ?? TimeProvider.System;

        _resultId = Guid.NewGuid();
        _startTimestamp = _timeProvider.GetUtcNow();
        _startedAt = _timeProvider.GetTimestamp();
    }

    /// <summary>Builds a structurally read-only snapshot of the collected diagnostic data.</summary>
    /// <returns>The completed probe result.</returns>
    public IProbeResult Build()
    {
        TimeSpan duration = _timeProvider.GetElapsedTime(_startedAt);

        return new Result(_probeId, _resultId, _startTimestamp, duration, HostMetadataCache.Host, BuildResults());
    }
    sealed class Result :
        IProbeResult
    {
        public Result(Guid probeId, Guid resultId, DateTimeOffset startTimestamp, TimeSpan duration, HostInfo host,
            IReadOnlyDictionary<string, object> results)
        {
            ProbeId = probeId;
            ResultId = resultId;
            StartTimestamp = startTimestamp;
            Duration = duration;
            Host = host;
            Results = results;
        }

        public Guid ResultId { get; }
        public Guid ProbeId { get; }
        public DateTimeOffset StartTimestamp { get; }
        public TimeSpan Duration { get; }
        public HostInfo Host { get; }
        public IReadOnlyDictionary<string, object> Results { get; }
    }
}
