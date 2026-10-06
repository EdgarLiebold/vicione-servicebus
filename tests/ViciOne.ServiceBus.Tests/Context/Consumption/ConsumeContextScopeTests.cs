using System.Reflection;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Context.Consumption;

public sealed class ConsumeContextScopeTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-CONSUME-CONTEXT-SCOPE", "typed-notifications-preserve-all-arguments")]
    public void TypedNotifications_PreserveEverySourceArgument()
    {
        ReceiveContext receiveContext = DispatchProxy.Create<ReceiveContext, MinimalReceiveContextProxy>();
        ((MinimalReceiveContextProxy)(object)receiveContext).PublishEndpointProvider =
            DispatchProxy.Create<IPublishEndpointProvider, UnexpectedInvocationProxy>();
        SerializerContext serializerContext = DispatchProxy.Create<SerializerContext, UnexpectedInvocationProxy>();
        AdvancedScopeConsumeContext source =
            DispatchProxy.Create<AdvancedScopeConsumeContext, ScopeNotificationContextProxy>();
        var sourceProxy = (ScopeNotificationContextProxy)(object)source;
        sourceProxy.ReceiveContext = receiveContext;
        sourceProxy.SerializerContext = serializerContext;
        var scope = new ConsumeContextScope<ScopeMessage>(source);
        var failure = new ExpectedScopeFailure();
        TimeSpan consumedDuration = TimeSpan.FromMilliseconds(19);
        TimeSpan faultedDuration = TimeSpan.FromMilliseconds(23);
        using var cancellation = new CancellationTokenSource();

        Task consumed = scope.NotifyConsumedAsync(consumedDuration, "consumer", cancellation.Token);
        Task faulted = scope.NotifyFaultedAsync(faultedDuration, "consumer", failure, cancellation.Token);
        Assert.Same(sourceProxy.NotificationTask, consumed);
        Assert.Same(sourceProxy.NotificationTask, faulted);
        Assert.False(consumed.IsCompleted);
        Assert.False(faulted.IsCompleted);
        sourceProxy.CompleteNotification();
        Assert.True(consumed.IsCompletedSuccessfully);
        Assert.True(faulted.IsCompletedSuccessfully);

        Assert.Collection(
            sourceProxy.NotificationInvocations,
            invocation =>
            {
                Assert.Equal(nameof(ConsumeContext.NotifyConsumedAsync), invocation.MethodName);
                Assert.Same(scope, invocation.Arguments[0]);
                Assert.Equal(consumedDuration, invocation.Arguments[1]);
                Assert.Equal("consumer", invocation.Arguments[2]);
                Assert.Equal(cancellation.Token, invocation.Arguments[3]);
            },
            invocation =>
            {
                Assert.Equal(nameof(ConsumeContext.NotifyFaultedAsync), invocation.MethodName);
                Assert.Same(scope, invocation.Arguments[0]);
                Assert.Equal(faultedDuration, invocation.Arguments[1]);
                Assert.Equal("consumer", invocation.Arguments[2]);
                Assert.Same(failure, invocation.Arguments[3]);
                Assert.Equal(cancellation.Token, invocation.Arguments[4]);
            });
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONSUME-CONTEXT-SCOPE", "empty-scope-owns-new-payloads")]
    public async Task EmptyScopes_OwnNewPayloadsWithoutMutatingTheSourceAsync()
    {
        TimeSpan timeout = TestConfigurationProvider.ForCurrentTestRun().GetValidatedOptions().OperationTimeout!.Value;
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = new InMemoryTestHarness($"empty-consume-scope-{NewId.NextGuid():N}")
        {
            TestTimeout = timeout,
            TestInactivityTimeout = timeout,
        };
        var assertionsCompleted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        harness.AddHandler<ScopeMessage>(context =>
        {
            var scope = new ConsumeContextScope(context.Advanced());
            var typedScope = new ConsumeContextScope<ScopeMessage>(context);

            Assert.Same(context.Message, typedScope.Message);
            Assert.Equal(context.CancellationToken, scope.CancellationToken);
            Assert.Equal(context.CancellationToken, typedScope.CancellationToken);
            Assert.True(scope.TryGetPayload(out ConsumeContextScope? selectedScope));
            Assert.Same(scope, selectedScope);
            Assert.Same(
                scope,
                scope.AddOrUpdatePayload<ConsumeContextScope>(
                    () => throw new InvalidOperationException(),
                    _ => throw new InvalidOperationException()));

            var created = new CounterPayload(1);
            Assert.Same(created, scope.GetOrAddPayload(() => created));
            CounterPayload updated = scope.AddOrUpdatePayload<CounterPayload>(
                () => throw new InvalidOperationException(),
                current => new CounterPayload(current.Value + 1));
            Assert.Equal(new CounterPayload(2), updated);
            LocalPayload added = scope.AddOrUpdatePayload(
                () => new LocalPayload("added"),
                _ => throw new InvalidOperationException());
            Assert.Equal(new LocalPayload("added"), added);
            Assert.False(context.TryGetPayload(out CounterPayload? sourceCounter));
            Assert.Null(sourceCounter);
            Assert.False(context.TryGetPayload(out LocalPayload? sourceLocal));
            Assert.Null(sourceLocal);
            Assert.False(scope.TryGetPayload(out UnrelatedPayload? unrelated));
            Assert.Null(unrelated);

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
        harness.AddHandler<ScopeMessage>(context =>
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
        Assert.Equal(
            "context",
            Assert.Throws<ArgumentNullException>(
                () => new ConsumeContextScope((ConsumeContext)null!, new object())).ParamName);
        Assert.Equal(
            "context",
            Assert.Throws<ArgumentNullException>(
                () => new ConsumeContextScope<ScopeMessage>(null!, new object())).ParamName);
    }

    private sealed record ScopeMessage(string Value);

    private sealed record LocalPayload(string Value);

    private sealed record CounterPayload(int Value);

    private sealed record UnrelatedPayload(string Value);

    private sealed record NotificationInvocation(string MethodName, object?[] Arguments);

    private sealed class ExpectedScopeFailure : Exception;

    private interface AdvancedScopeConsumeContext :
        ConsumeContext<ScopeMessage>,
        ConsumeContext;

    private class ScopeNotificationContextProxy : DispatchProxy
    {
        public ReceiveContext ReceiveContext { get; set; } = null!;

        public SerializerContext SerializerContext { get; set; } = null!;

        readonly TaskCompletionSource _notification = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task NotificationTask => _notification.Task;

        public void CompleteNotification() => _notification.SetResult();

        public List<NotificationInvocation> NotificationInvocations { get; } = [];

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            return targetMethod?.Name switch
            {
                "get_ReceiveContext" => ReceiveContext,
                "get_SerializerContext" => SerializerContext,
                nameof(ConsumeContext.NotifyConsumedAsync) or nameof(ConsumeContext.NotifyFaultedAsync) =>
                    RecordNotification(targetMethod.Name, args),
                _ => throw new InvalidOperationException(
                    $"The scope notification source unexpectedly invoked {targetMethod?.Name}."),
            };
        }

        private object RecordNotification(string methodName, object?[]? args)
        {
            NotificationInvocations.Add(new NotificationInvocation(methodName, args?.ToArray() ?? []));
            return NotificationTask;
        }
    }

    private class MinimalReceiveContextProxy : DispatchProxy
    {
        public IPublishEndpointProvider PublishEndpointProvider { get; set; } = null!;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            targetMethod?.Name == "get_PublishEndpointProvider"
                ? PublishEndpointProvider
                : throw new InvalidOperationException(
                    $"The minimal receive context unexpectedly invoked {targetMethod?.Name}.");
    }

    private class UnexpectedInvocationProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new InvalidOperationException($"The proxy unexpectedly invoked {targetMethod?.Name}.");
    }
}
