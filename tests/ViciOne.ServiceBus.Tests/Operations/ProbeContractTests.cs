using Microsoft.Extensions.Time.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.Operations;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Operations;

public sealed class ProbeContractTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-BUS-PROBE-CONTRACT", "owner-and-cancellation-boundaries")]
    public void ProbeEntryPoint_RejectsAMissingOwnerAndPreservesCancellation()
    {
        Assert.Equal(
            "probeSite",
            Assert.Throws<ArgumentNullException>(() =>
                ProbeSiteExtensions.GetProbeResult(null!, TestContext.Current.CancellationToken)).ParamName);
        using var source = new CancellationTokenSource();
        source.Cancel();
        var site = new CancellationAwareProbeSite();

        OperationCanceledException exception = Assert.Throws<OperationCanceledException>(
            () => site.GetProbeResult(source.Token));

        Assert.Equal(source.Token, exception.CancellationToken);
        Assert.Equal(source.Token, site.ObservedToken);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BUS-PROBE-CONTRACT", "context-input-validation")]
    public void ProbeContext_RejectsMissingOwnersKeysAndValueSources()
    {
        ProbeContext context = CreateDriver().Context;

        Assert.Equal("context", Assert.Throws<ArgumentNullException>(
            () => ProbeContextExtensions.CreateFilterScope(null!, "filter")).ParamName);
        Assert.Equal("context", Assert.Throws<ArgumentNullException>(
            () => ProbeContextExtensions.CreateConsumerFactoryScope<object>(null!, "source")).ParamName);
        Assert.Equal("context", Assert.Throws<ArgumentNullException>(
            () => ProbeContextExtensions.CreateMessageScope(null!, "message")).ParamName);
        Assert.Equal("filterType", Assert.Throws<ArgumentException>(() => context.CreateFilterScope(" ")).ParamName);
        Assert.Equal("source", Assert.Throws<ArgumentException>(
            () => context.CreateConsumerFactoryScope<object>(string.Empty)).ParamName);
        Assert.Equal("messageType", Assert.Throws<ArgumentException>(() => context.CreateMessageScope(" ")).ParamName);
        Assert.Equal("key", Assert.Throws<ArgumentException>(() => context.Add(" ", "value")).ParamName);
        Assert.Equal("key", Assert.Throws<ArgumentException>(() => context.Add(" ", new object())).ParamName);
        Assert.Equal("key", Assert.Throws<ArgumentException>(() => context.CreateScope(" ")).ParamName);
        Assert.Equal("values", Assert.Throws<ArgumentNullException>(() => context.Set((object)null!)).ParamName);
        Assert.Equal(
            "values",
            Assert.Throws<ArgumentNullException>(
                () => context.Set((IEnumerable<KeyValuePair<string, object?>>)null!)).ParamName);
        Assert.Equal(
            "values",
            Assert.Throws<ArgumentException>(
                () => context.Set([new KeyValuePair<string, object?>(" ", "value")])).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BUS-PROBE-SNAPSHOT", "set-remove-and-standard-scopes")]
    public void ProbeContext_ProducesTheDocumentedValuesAndStandardScopes()
    {
        var driver = CreateDriver();
        ProbeContext context = driver.Context;
        context.Add("removedString", "present");
        context.Add("removedString", (string?)null);
        context.Add("removedObject", new object());
        context.Add("removedObject", (object?)null);
        context.Set(new { Name = "sample", Removed = "" });
        context.Set(
        [
            new KeyValuePair<string, object?>("count", 7),
            new KeyValuePair<string, object?>("removedPair", null),
        ]);
        context.CreateFilterScope("dispatch");
        context.CreateConsumerFactoryScope<ProbeConsumer>("container");
        context.CreateMessageScope("message-contract").Add("enabled", true);

        IReadOnlyDictionary<string, object> results = driver.Build().Results;

        Assert.DoesNotContain("removedString", results);
        Assert.DoesNotContain("removedObject", results);
        Assert.DoesNotContain("removed", results);
        Assert.DoesNotContain("removedPair", results);
        Assert.Equal("sample", Assert.Contains("name", results));
        Assert.Equal(7, Assert.Contains("count", results));
        IReadOnlyDictionary<string, object> filter = Scope(results, "filters");
        Assert.Equal("dispatch", Assert.Contains("filterType", filter));
        IReadOnlyDictionary<string, object> factory = Scope(results, "consumerFactory");
        Assert.Equal("container", Assert.Contains("source", factory));
        Assert.Equal(TypeCache<ProbeConsumer>.ShortName, Assert.Contains("consumerType", factory));
        Assert.Equal(true, Assert.Contains("enabled", Scope(results, "message-contract")));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BUS-PROBE-CONTRACT", "scope-key-collision-preserves-existing-value")]
    public void CreateScope_RejectsAScalarKeyWithoutReplacingItsValue()
    {
        var driver = CreateDriver();
        driver.Context.Add("endpoint", "input");

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => driver.Context.CreateScope("endpoint"));

        Assert.Equal("The key already exists and is not a scope collection: endpoint", exception.Message);
        Assert.Equal("input", Assert.Contains("endpoint", driver.Build().Results));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BUS-PROBE-SNAPSHOT", "request-result-clock-and-token-metadata")]
    public void ProbeResult_UsesTheSuppliedRequestIdentityClockAndCancellationToken()
    {
        var startedAt = new DateTimeOffset(2042, 4, 3, 2, 1, 0, TimeSpan.Zero);
        var clock = new FakeTimeProvider(startedAt);
        Guid probeId = Guid.NewGuid();
        using var source = new CancellationTokenSource();
        var driver = new ProbeResultBuilderTestDriver(probeId, source.Token, clock);
        Assert.Equal(source.Token, driver.Context.CancellationToken);
        clock.Advance(TimeSpan.FromSeconds(17));

        IProbeResult result = driver.Build();

        Assert.Equal(probeId, result.ProbeId);
        Assert.NotEqual(Guid.Empty, result.ResultId);
        Assert.NotEqual(probeId, result.ResultId);
        Assert.Equal(startedAt, result.StartTimestamp);
        Assert.Equal(TimeSpan.FromSeconds(17), result.Duration);
        Assert.NotNull(result.Host);
    }

    private static ProbeResultBuilderTestDriver CreateDriver() =>
        new(Guid.NewGuid(), TestContext.Current.CancellationToken, TimeProvider.System);

    private static IReadOnlyDictionary<string, object> Scope(
        IReadOnlyDictionary<string, object> parent,
        string key) =>
        Assert.IsAssignableFrom<IReadOnlyDictionary<string, object>>(Assert.Contains(key, parent));

    private sealed class CancellationAwareProbeSite : IProbeSite
    {
        public CancellationToken ObservedToken { get; private set; }

        public void Probe(ProbeContext context)
        {
            ObservedToken = context.CancellationToken;
            context.CancellationToken.ThrowIfCancellationRequested();
        }
    }

    private sealed class ProbeConsumer;
}
