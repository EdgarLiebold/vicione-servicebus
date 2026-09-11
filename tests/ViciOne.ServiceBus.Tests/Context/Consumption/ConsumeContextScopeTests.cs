using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Context.Consumption;

public sealed class ConsumeContextScopeTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-CONSUME-CONTEXT-SCOPE", "local-payload-isolation-precedence-and-typed-message")]
    public async Task LocalScope_IsolatesPayloadUpdatesAndPreservesTheTypedMessageAsync()
    {
        TimeSpan timeout = TestConfigurationProvider.ForCurrentTestRun().GetValidatedOptions().OperationTimeout!.Value;
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = new InMemoryTestHarness($"consume-scope-{NewId.NextGuid():N}")
        {
            TestTimeout = timeout,
            TestInactivityTimeout = timeout,
        };
        var assertionsCompleted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        harness.Handler<ScopeMessage>(context =>
        {
            var rootPayload = new CounterPayload(10);
            Assert.Same(rootPayload, context.GetOrAddPayload(() => rootPayload));
            var localPayload = new LocalPayload("local");
            var scope = new ConsumeContextScope<ScopeMessage>(context, localPayload);
            var factoryInvoked = false;

            Assert.Same(context.Message, scope.Message);
            Assert.Equal(context.CancellationToken, scope.CancellationToken);
            Assert.True(scope.HasPayloadType(typeof(ConsumeContextScope<ScopeMessage>)));
            Assert.True(scope.HasPayloadType(typeof(LocalPayload)));
            Assert.True(scope.HasPayloadType(typeof(CounterPayload)));
            Assert.True(scope.TryGetPayload(out LocalPayload? selectedLocal));
            Assert.Same(localPayload, selectedLocal);
            Assert.Same(localPayload, scope.GetOrAddPayload(() =>
            {
                factoryInvoked = true;
                return new LocalPayload("unexpected");
            }));
            Assert.Same(rootPayload, scope.GetOrAddPayload(() =>
            {
                factoryInvoked = true;
                return new CounterPayload(99);
            }));
            Assert.False(factoryInvoked);

            CounterPayload scopedPayload = scope.AddOrUpdatePayload(
                () => new CounterPayload(1),
                current => new CounterPayload(current.Value + 1));
            Assert.Equal(11, scopedPayload.Value);
            Assert.True(scope.TryGetPayload(out CounterPayload? selectedScoped));
            Assert.Same(scopedPayload, selectedScoped);
            Assert.True(context.TryGetPayload(out CounterPayload? selectedRoot));
            Assert.Same(rootPayload, selectedRoot);
            Assert.Same(scope, scope.GetOrAddPayload<ConsumeContextScope<ScopeMessage>>(() => throw new InvalidOperationException()));

            Assert.Equal("payloadType", Assert.Throws<ArgumentNullException>(() => scope.HasPayloadType(null!)).ParamName);
            Assert.Equal("payloadFactory", Assert.Throws<ArgumentNullException>(() => scope.GetOrAddPayload<LocalPayload>(null!)).ParamName);
            Assert.Equal(
                "addFactory",
                Assert.Throws<ArgumentNullException>(() => scope.AddOrUpdatePayload<LocalPayload>(null!, current => current)).ParamName);
            Assert.Equal(
                "updateFactory",
                Assert.Throws<ArgumentNullException>(() => scope.AddOrUpdatePayload(() => new LocalPayload("new"), null!)).ParamName);
            Assert.Equal(
                "payloads",
                Assert.Throws<ArgumentNullException>(() => new ConsumeContextScope<ScopeMessage>(context, null!)).ParamName);

            assertionsCompleted.TrySetResult();
            return Task.CompletedTask;
        });

        await harness.StartAsync(cancellationToken);
        try
        {
            await harness.Bus.PublishAsync(new ScopeMessage("value"), cancellationToken);
            await assertionsCompleted.Task.WaitAsync(timeout, cancellationToken);
        }
        finally
        {
            await harness.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONSUME-CONTEXT-SCOPE", "constructors-require-source-context")]
    public void Constructors_RejectMissingSourceContexts()
    {
        Assert.Equal(
            "context",
            Assert.Throws<ArgumentNullException>(() => new ConsumeContextScope((ConsumeContext)null!)).ParamName);
        Assert.Equal(
            "context",
            Assert.Throws<ArgumentNullException>(() => new ConsumeContextScope<ScopeMessage>(null!)).ParamName);
    }

    private sealed record ScopeMessage(string Value);

    private sealed record LocalPayload(string Value);

    private sealed record CounterPayload(int Value);
}
