// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus
{
    using System.Threading;
    using Introspection;


    public static class IntrospectionExtensions
    {
        public static ProbeResult GetProbeResult(this IProbeSite probeSite, CancellationToken cancellationToken = default)
        {
            var builder = new ProbeResultBuilder(NewId.NextGuid(), cancellationToken);

            probeSite.Probe(builder);

            return builder.Build();
        }
    }
}
