using System.Reflection;
using Microsoft.Extensions.Time.Testing;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Middleware.InMemoryOutbox;
using ViciOne.ServiceBus.Scheduling;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Scheduling;

public sealed class SchedulingExtensionContractTests
{
    private static readonly Uri InputAddress = new("loopback://localhost/scheduler-input");
    private static readonly Uri ExplicitDestination = new("loopback://localhost/scheduler-destination");
    private static readonly DateTimeOffset DueAt = new(2041, 2, 3, 4, 5, 6, TimeSpan.Zero);

    [Theory]
    [InlineData(ScheduleCancellationMode.Unknown)]
    [InlineData(ScheduleCancellationMode.Unsupported)]
    [InlineData(ScheduleCancellationMode.CallerSpecifiedTokenWithoutCancellation)]
    [InlineData(ScheduleCancellationMode.CallerSpecifiedToken)]
    [InlineData(ScheduleCancellationMode.ProviderAssignedToken)]
    [RequirementCoverage("REQ-VSB-SCHEDULER-CONTEXT-BOUNDARY", "cancellation-capability-survives-consume-and-outbox-scopes")]
    public void CancellationCapability_SurvivesConsumeAndOutboxScopes(ScheduleCancellationMode mode)
    {
        ReceiveContext receive = DispatchProxy.Create<ReceiveContext, SchedulerReceiveContextProxy>();
        ((SchedulerReceiveContextProxy)(object)receive).InputAddress = InputAddress;
        ConsumeContext consume = DispatchProxy.Create<ConsumeContext, SchedulerConsumeContextProxy>();
        ((SchedulerConsumeContextProxy)(object)consume).ReceiveContext = receive;
        IBusTopology topology = DispatchProxy.Create<IBusTopology, UnexpectedInvocationProxy>();
        var provider = DispatchProxy.Create<ModeScheduleProvider, ModeProviderProxy>();
        ((ModeProviderProxy)(object)provider).Mode = mode;
        var resolved = new MessageScheduler(provider, topology);
        var resolutions = 0;
        MessageSchedulerFactory factory = _ =>
        {
            resolutions++;
            return resolved;
        };

        var scoped = new ConsumeMessageSchedulerContext(consume, factory);
        var outbox = new InMemoryOutboxMessageSchedulerContext(consume, factory, Task.CompletedTask);

        Assert.Equal(mode,
            ((IScheduleCancellationCapability)scoped).CancellationMode);
        Assert.Equal(mode,
            ((IScheduleCancellationCapability)outbox).CancellationMode);
        Assert.Equal(2, resolutions);
        Assert.Equal(mode,
            ((IScheduleCancellationCapability)scoped).CancellationMode);
        Assert.Equal(2, resolutions);
    }

    public interface ModeScheduleProvider : IScheduleMessageProvider, IScheduleCancellationCapability
    {
    }

    public class ModeProviderProxy : DispatchProxy
    {
        public ScheduleCancellationMode Mode { get; set; }

        protected override object? Invoke(MethodInfo? method, object?[]? arguments) =>
            method?.Name == "get_CancellationMode" ? Mode
            : throw new InvalidOperationException($"Scheduling was unexpected: {method?.Name}");
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ADVANCED-SCHEDULER-BOUNDARY", "every-extension-rejects-null-scheduler")]
    public void EveryAdvancedSchedulerExtension_RejectsANullScheduler()
    {
        MethodInfo[] methods = typeof(AdvancedMessageSchedulerExtensions)
            .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .OrderBy(static method => method.ToString(), StringComparer.Ordinal)
            .ToArray();

        Assert.NotEmpty(methods);
        foreach (MethodInfo definition in methods)
        {
            MethodInfo method = definition.IsGenericMethodDefinition
                ? definition.MakeGenericMethod(typeof(ProbeMessage))
                : definition;
            object?[] arguments = CreateNullReceiverArguments(method);

            TargetInvocationException invocation = Assert.Throws<TargetInvocationException>(() => method.Invoke(null, arguments));
            ArgumentNullException exception = Assert.IsType<ArgumentNullException>(invocation.InnerException);
            Assert.Equal("scheduler", exception.ParamName);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SCHEDULER-CONTEXT-BOUNDARY", "adapter-is-sealed-and-requires-collaborators")]
    public void ConsumeSchedulerContext_IsSealedAndRequiresItsCollaborators()
    {
        Assert.True(typeof(ConsumeMessageSchedulerContext).IsSealed);
        Assert.Equal("consumeContext", Assert.Throws<ArgumentNullException>(() =>
            new ConsumeMessageSchedulerContext(null!, _ => throw new InvalidOperationException())).ParamName);

        ConsumeContext context = DispatchProxy.Create<ConsumeContext, UnexpectedInvocationProxy>();
        Assert.Equal("schedulerFactory", Assert.Throws<ArgumentNullException>(() =>
            new ConsumeMessageSchedulerContext(context, null!)).ParamName);

        ReceiveContext receiveContext = DispatchProxy.Create<ReceiveContext, SchedulerReceiveContextProxy>();
        ((SchedulerReceiveContextProxy)(object)receiveContext).InputAddress = new Uri("loopback://localhost/scheduler-input");
        ConsumeContext schedulerContext = DispatchProxy.Create<ConsumeContext, SchedulerConsumeContextProxy>();
        ((SchedulerConsumeContextProxy)(object)schedulerContext).ReceiveContext = receiveContext;
        var adapter = new ConsumeMessageSchedulerContext(schedulerContext, _ => null!);

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => _ = adapter.TimeProvider);
        Assert.Equal("The message scheduler factory returned null.", exception.Message);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SCHEDULER-CONTEXT-DELEGATION", "all-thirty-three-advanced-operations")]
    public async Task EveryAdvancedOperation_DelegatesExactlyOnceWithTheExpectedDestinationAndArgumentsAsync()
    {
        ReceiveContext receiveContext = DispatchProxy.Create<ReceiveContext, SchedulerReceiveContextProxy>();
        ((SchedulerReceiveContextProxy)(object)receiveContext).InputAddress = InputAddress;
        ConsumeContext consumeContext = DispatchProxy.Create<ConsumeContext, SchedulerConsumeContextProxy>();
        ((SchedulerConsumeContextProxy)(object)consumeContext).ReceiveContext = receiveContext;
        IAdvancedMessageScheduler scheduler = DispatchProxy.Create<IAdvancedMessageScheduler, RecordingSchedulerProxy>();
        var schedulerProxy = (RecordingSchedulerProxy)(object)scheduler;
        var factoryInvocationCount = 0;
        var adapter = new ConsumeMessageSchedulerContext(consumeContext, _ =>
        {
            factoryInvocationCount++;
            return scheduler;
        });

        Assert.Same(schedulerProxy.Clock, adapter.TimeProvider);
        Assert.Equal(1, factoryInvocationCount);
        schedulerProxy.Invocations.Clear();

        MethodInfo[] operations = typeof(ConsumeMessageSchedulerContext)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
            .Where(static method => typeof(Task).IsAssignableFrom(method.ReturnType)
                && (method.Name.Contains("Schedule", StringComparison.Ordinal)
                    || method.Name.Contains("Cancel", StringComparison.Ordinal)))
            .OrderBy(static method => method.ToString(), StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(33, operations.Length);
        foreach (MethodInfo definition in operations)
        {
            MethodInfo operation = definition.IsGenericMethodDefinition
                ? definition.MakeGenericMethod(typeof(ProbeMessage))
                : definition;
            object?[] arguments = CreateOperationArguments(operation);

            var task = Assert.IsAssignableFrom<Task>(operation.Invoke(adapter, arguments));
            await task;

            SchedulerInvocation invocation = Assert.Single(schedulerProxy.Invocations);
            Assert.Equal(operation.Name.Split('.').Last(), invocation.Method.Name);
            object?[] expectedArguments = IsContextRelativeSend(operation)
                ? [InputAddress, .. arguments]
                : arguments;
            Assert.Equal(expectedArguments.Length, invocation.Arguments.Length);
            for (var index = 0; index < expectedArguments.Length; index++)
                Assert.Equal(expectedArguments[index], invocation.Arguments[index]);

            schedulerProxy.Invocations.Clear();
        }

        Assert.Equal(1, factoryInvocationCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SCHEDULING-EXTENSION-BOUNDARY", "every-overload-rejects-null-context")]
    public void EverySchedulingExtension_RejectsANullConsumeContext()
    {
        Type[] extensionTypes =
        [
            typeof(ConsumeContextSchedulerExtensions),
            typeof(ConsumeContextSelfSchedulerExtensions),
            typeof(SchedulePublishExtensions),
            typeof(SchedulingExtensions),
        ];

        foreach (Type extensionType in extensionTypes)
        {
            MethodInfo[] methods = extensionType
                .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
                .OrderBy(static method => method.ToString(), StringComparer.Ordinal)
                .ToArray();

            Assert.NotEmpty(methods);
            foreach (MethodInfo definition in methods)
            {
                MethodInfo method = definition.IsGenericMethodDefinition
                    ? definition.MakeGenericMethod(typeof(ProbeMessage))
                    : definition;
                object?[] arguments = CreateNullReceiverArguments(method);

                TargetInvocationException invocation = Assert.Throws<TargetInvocationException>(() => method.Invoke(null, arguments));
                ArgumentNullException exception = Assert.IsType<ArgumentNullException>(invocation.InnerException);
                Assert.Equal("context", exception.ParamName);
            }
        }
    }

    private static object?[] CreateNullReceiverArguments(MethodInfo method) =>
        method
            .GetParameters()
            .Select(static (parameter, index) => index == 0
                ? null
                : parameter.ParameterType.IsValueType
                    ? Activator.CreateInstance(parameter.ParameterType)
                    : null)
            .ToArray();

    private static object?[] CreateOperationArguments(MethodInfo operation) =>
        operation
            .GetParameters()
            .Select<ParameterInfo, object?>(static parameter =>
            {
                Type type = parameter.ParameterType;
                if (type == typeof(Uri))
                    return ExplicitDestination;
                if (type == typeof(DateTimeOffset))
                    return DueAt;
                if (type == typeof(Guid))
                    return Guid.Parse("7a2ca689-d810-4b92-a14d-c400f4a50c53");
                if (type == typeof(CancellationToken))
                    return TestContext.Current.CancellationToken;
                if (type == typeof(Type))
                    return typeof(ProbeMessage);
                if (type == typeof(ProbeMessage) || type == typeof(object))
                    return new ProbeMessage();
                if (type == typeof(IPipe<SendContext<ProbeMessage>>))
                    return Pipe.Empty<SendContext<ProbeMessage>>();
                if (type == typeof(IPipe<SendContext>))
                    return Pipe.Empty<SendContext>();

                throw new InvalidOperationException($"No scheduler test value is defined for parameter type {type}.");
            })
            .ToArray();

    private static bool IsContextRelativeSend(MethodInfo operation) =>
        operation.Name.EndsWith(nameof(IAdvancedMessageScheduler.ScheduleSendAsync), StringComparison.Ordinal)
        && operation.GetParameters().All(static parameter => parameter.ParameterType != typeof(Uri));

    private sealed record ProbeMessage;

    private class UnexpectedInvocationProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new InvalidOperationException($"The invalid scheduler boundary invoked {targetMethod?.Name}.");
    }

    private class SchedulerConsumeContextProxy : DispatchProxy
    {
        public ReceiveContext ReceiveContext { get; set; } = null!;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            targetMethod?.Name == "get_ReceiveContext"
                ? ReceiveContext
                : throw new InvalidOperationException($"The scheduler context invoked {targetMethod?.Name}.");
    }

    private class SchedulerReceiveContextProxy : DispatchProxy
    {
        public Uri InputAddress { get; set; } = null!;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            targetMethod?.Name == "get_InputAddress"
                ? InputAddress
                : throw new InvalidOperationException($"The scheduler receive context invoked {targetMethod?.Name}.");
    }

    private sealed record SchedulerInvocation(MethodInfo Method, object?[] Arguments);

    private class RecordingSchedulerProxy : DispatchProxy
    {
        private static readonly MethodInfo FromResultMethod = typeof(Task)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(static method => method.Name == nameof(Task.FromResult));

        public TimeProvider Clock { get; } = new FakeTimeProvider(DueAt);

        public List<SchedulerInvocation> Invocations { get; } = [];

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            MethodInfo method = targetMethod ?? throw new InvalidOperationException("The scheduler proxy received no method metadata.");
            if (method.Name == "get_TimeProvider")
                return Clock;

            object?[] arguments = args?.ToArray() ?? [];
            Invocations.Add(new SchedulerInvocation(method, arguments));

            if (method.ReturnType == typeof(Task))
                return Task.CompletedTask;
            if (method.ReturnType.IsGenericType && method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>))
            {
                Type resultType = method.ReturnType.GetGenericArguments()[0];
                return FromResultMethod.MakeGenericMethod(resultType).Invoke(null, [null]);
            }

            throw new InvalidOperationException($"The scheduler operation {method.Name} has unsupported return type {method.ReturnType}.");
        }
    }
}
