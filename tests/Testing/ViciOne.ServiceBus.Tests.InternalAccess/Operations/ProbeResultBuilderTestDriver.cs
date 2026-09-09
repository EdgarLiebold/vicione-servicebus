namespace ViciOne.ServiceBus.Tests.InternalAccess.Operations;

public sealed class ProbeResultBuilderTestDriver
{
    private readonly ProbeResultBuilder _builder;

    public ProbeResultBuilderTestDriver(
        Guid probeId,
        CancellationToken cancellationToken,
        TimeProvider timeProvider)
    {
        _builder = new ProbeResultBuilder(probeId, cancellationToken, timeProvider);
    }

    public ProbeContext Context => _builder;

    public IProbeResult Build() => _builder.Build();
}
