using System;
using System.Collections.Generic;
using System.Threading;
using ViciOne.ServiceBus.Metadata;

namespace ViciOne.ServiceBus.Introspection;

/// <summary>Builds probe result components.</summary>
public class ProbeResultBuilder :
    ScopeProbeContext,
    IProbeResultBuilder
{
    readonly Guid _probeId;
    readonly Guid _resultId;
    readonly DateTimeOffset _startTimestamp;
    readonly long _startedAt;
    readonly TimeProvider _timeProvider;

    /// <summary>Initializes a new instance.</summary>
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

    /// <summary>Builds the configured component.</summary>
    /// <returns>The configured component.</returns>
    public new ProbeResult Build()
    {
        TimeSpan duration = _timeProvider.GetElapsedTime(_startedAt);

        return new Result(_probeId, _resultId, _startTimestamp, duration, HostMetadataCache.Host, base.Build());
    }


    class Result :
        ProbeResult
    {
        public Result(Guid probeId, Guid resultId, DateTimeOffset startTimestamp, TimeSpan duration, HostInfo host,
            IDictionary<string, object> results)
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
        public IDictionary<string, object> Results { get; }
    }
}
