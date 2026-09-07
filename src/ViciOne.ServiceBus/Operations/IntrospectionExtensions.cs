using System;
using System.Threading;
using ViciOne.ServiceBus.Introspection;

namespace ViciOne.ServiceBus.Operations;

/// <summary>Builds structurally read-only probe results from service-bus diagnostic sites.</summary>
public static class IntrospectionExtensions
{
    /// <summary>Captures the current diagnostic structure of a probe site.</summary>
    /// <param name="probeSite">The diagnostic site to inspect.</param>
    /// <param name="cancellationToken">Cancels the probe operation.</param>
    /// <returns>The completed read-only probe result.</returns>
    public static IProbeResult GetProbeResult(this IProbeSite probeSite, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(probeSite);

        var builder = new ProbeResultBuilder(NewId.NextGuid(), cancellationToken);

        probeSite.Probe(builder);

        return builder.Build();
    }
}
