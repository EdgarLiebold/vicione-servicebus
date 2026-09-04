using System.Threading;
using ViciOne.ServiceBus.Introspection;

namespace ViciOne.ServiceBus;

public static class IntrospectionExtensions
{
    public static ProbeResult GetProbeResult(this IProbeSite probeSite, CancellationToken cancellationToken = default)
    {
        var builder = new ProbeResultBuilder(NewId.NextGuid(), cancellationToken);

        probeSite.Probe(builder);

        return builder.Build();
    }
}
