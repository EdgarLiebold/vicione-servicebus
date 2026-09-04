using System.Threading;
using ViciOne.ServiceBus.Introspection;

namespace ViciOne.ServiceBus.Operations;

/// <summary>
/// Provides extension methods for introspection.
/// </summary>
public static class IntrospectionExtensions
{
    /// <summary>
    /// Gets probe result.
    /// </summary>
    /// <param name="probeSite">The probe site value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public static ProbeResult GetProbeResult(this IProbeSite probeSite, CancellationToken cancellationToken = default)
    {
        var builder = new ProbeResultBuilder(NewId.NextGuid(), cancellationToken);

        probeSite.Probe(builder);

        return builder.Build();
    }
}
