using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Time.Testing;
using ViciOne.ServiceBus.Payloads;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Abstractions.Tests.Contexts;

public sealed class PipeContextTimeProviderExtensionsTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-CONTEXT-TIME-PROVIDER", "system-default")]
    public void ContextWithoutAnExplicitProvider_UsesTheSystemProvider()
    {
        var context = new TestPipeContext();

        TimeProvider actual = context.GetTimeProvider();

        Assert.Same(TimeProvider.System, actual);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONTEXT-TIME-PROVIDER", "set-and-replace")]
    public void ExplicitProvider_CanBeSetAndReplacedAtOneContextBoundary()
    {
        var context = new TestPipeContext();
        var first = new FakeTimeProvider(new DateTimeOffset(2026, 8, 24, 9, 10, 11, TimeSpan.Zero));
        var replacement = new FakeTimeProvider(new DateTimeOffset(2027, 1, 2, 3, 4, 5, TimeSpan.Zero));

        context.SetTimeProvider(first);
        Assert.Same(first, context.GetTimeProvider());

        context.SetTimeProvider(replacement);

        Assert.Same(replacement, context.GetTimeProvider());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONTEXT-TIME-PROVIDER", "null-boundaries")]
    public void NullContextAndProvider_AreRejectedAtThePublicBoundary()
    {
        var context = new TestPipeContext();

        Assert.Equal(
            "context",
            Assert.Throws<ArgumentNullException>(() =>
                PipeContextTimeProviderExtensions.GetTimeProvider(null!)).ParamName);
        Assert.Equal(
            "context",
            Assert.Throws<ArgumentNullException>(() =>
                PipeContextTimeProviderExtensions.SetTimeProvider(null!, TimeProvider.System)).ParamName);
        Assert.Equal(
            "timeProvider",
            Assert.Throws<ArgumentNullException>(() => context.SetTimeProvider(null!)).ParamName);
    }

    private sealed class TestPipeContext : PipeContext
    {
        private readonly IPayloadCache _payloads = new ListPayloadCache();

        public CancellationToken CancellationToken => CancellationToken.None;

        public bool HasPayloadType(Type payloadType) => _payloads.HasPayloadType(payloadType);

        public bool TryGetPayload<T>([NotNullWhen(true)] out T? payload)
            where T : class => _payloads.TryGetPayload(out payload);

        public T GetOrAddPayload<T>(PayloadFactory<T> payloadFactory)
            where T : class => _payloads.GetOrAddPayload(payloadFactory);

        public T AddOrUpdatePayload<T>(PayloadFactory<T> addFactory, UpdatePayloadFactory<T> updateFactory)
            where T : class => _payloads.AddOrUpdatePayload(addFactory, updateFactory);
    }
}
