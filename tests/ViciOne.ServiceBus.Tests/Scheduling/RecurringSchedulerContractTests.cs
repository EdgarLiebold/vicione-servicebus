using System.Reflection;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Scheduling;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Scheduling;

public sealed class RecurringSchedulerContractTests
{
    private static readonly Uri DestinationAddress = new("loopback://localhost/recurring-boundary");

    [Fact]
    [RequirementCoverage("REQ-VSB-RECURRING-SCHEDULER", "constructors-and-command-values-reject-null")]
    public void ConstructorsAndCommandValues_RejectNullCollaborators()
    {
        ISendEndpoint endpoint = DispatchProxy.Create<ISendEndpoint, UnexpectedInvocationProxy>();
        ISendEndpointProvider endpointProvider = DispatchProxy.Create<ISendEndpointProvider, UnexpectedInvocationProxy>();
        RecurringSchedule schedule = DispatchProxy.Create<RecurringSchedule, UnexpectedInvocationProxy>();
        var message = new ScheduledMessage();

        Assert.Equal("sendEndpointProvider", Assert.Throws<ArgumentNullException>(() =>
            new EndpointRecurringMessageScheduler(null!, DestinationAddress)).ParamName);
        Assert.Equal("schedulerAddress", Assert.Throws<ArgumentNullException>(() =>
            new EndpointRecurringMessageScheduler(endpointProvider, null!)).ParamName);
        Assert.Equal("sendEndpoint", Assert.Throws<ArgumentNullException>(() =>
            new EndpointRecurringMessageScheduler((ISendEndpoint)null!)).ParamName);
        Assert.Equal("publishEndpoint", Assert.Throws<ArgumentNullException>(() =>
            new PublishRecurringMessageScheduler(null!)).ParamName);

        Assert.Equal("schedule", Assert.Throws<ArgumentNullException>(() =>
            new ScheduleRecurringMessageCommand<ScheduledMessage>(null!, DestinationAddress, message)).ParamName);
        Assert.Equal("destination", Assert.Throws<ArgumentNullException>(() =>
            new ScheduleRecurringMessageCommand<ScheduledMessage>(schedule, null!, message)).ParamName);
        Assert.Equal("payload", Assert.Throws<ArgumentNullException>(() =>
            new ScheduleRecurringMessageCommand<ScheduledMessage>(schedule, DestinationAddress, null!)).ParamName);
        Assert.Equal("schedule", Assert.Throws<ArgumentNullException>(() =>
            new ScheduledRecurringMessageHandle<ScheduledMessage>(null!, DestinationAddress, message)).ParamName);
        Assert.Equal("destination", Assert.Throws<ArgumentNullException>(() =>
            new ScheduledRecurringMessageHandle<ScheduledMessage>(schedule, null!, message)).ParamName);
        Assert.Equal("payload", Assert.Throws<ArgumentNullException>(() =>
            new ScheduledRecurringMessageHandle<ScheduledMessage>(schedule, DestinationAddress, null!)).ParamName);
        Assert.Equal("destination", Assert.Throws<ArgumentNullException>(() =>
            new ScheduleMessageCommand<ScheduledMessage>(new DateTimeOffset(2039, 1, 2, 3, 4, 5, TimeSpan.Zero), null!, message,
                NewId.NextGuid())).ParamName);
        Assert.Equal("payload", Assert.Throws<ArgumentNullException>(() =>
            new ScheduleMessageCommand<ScheduledMessage>(new DateTimeOffset(2039, 1, 2, 3, 4, 5, TimeSpan.Zero), DestinationAddress, null!,
                NewId.NextGuid())).ParamName);
        Assert.Equal("destination", Assert.Throws<ArgumentNullException>(() =>
            new ScheduledMessageHandle<ScheduledMessage>(NewId.NextGuid(), new DateTimeOffset(2039, 1, 2, 3, 4, 5, TimeSpan.Zero), null!,
                message)).ParamName);
        Assert.Equal("payload", Assert.Throws<ArgumentNullException>(() =>
            new ScheduledMessageHandle<ScheduledMessage>(NewId.NextGuid(), new DateTimeOffset(2039, 1, 2, 3, 4, 5, TimeSpan.Zero),
                DestinationAddress, null!)).ParamName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [RequirementCoverage("REQ-VSB-RECURRING-SCHEDULER", "control-identities-reject-null-or-whitespace")]
    public async Task ControlOperations_RejectMissingScheduleIdentifiersAsync(string? missingValue)
    {
        ISendEndpoint endpoint = DispatchProxy.Create<ISendEndpoint, UnexpectedInvocationProxy>();
        IPublishEndpoint publishEndpoint = DispatchProxy.Create<IPublishEndpoint, UnexpectedInvocationProxy>();
        IRecurringMessageScheduler[] schedulers =
        [
            new EndpointRecurringMessageScheduler(endpoint),
            new PublishRecurringMessageScheduler(publishEndpoint)
        ];

        foreach (IRecurringMessageScheduler scheduler in schedulers)
        {
            await Assert.ThrowsAnyAsync<ArgumentException>(() =>
                scheduler.CancelScheduledRecurringSendAsync(missingValue!, "group", TestContext.Current.CancellationToken));
            await Assert.ThrowsAnyAsync<ArgumentException>(() =>
                scheduler.PauseScheduledRecurringSendAsync("schedule", missingValue!, TestContext.Current.CancellationToken));
            await Assert.ThrowsAnyAsync<ArgumentException>(() =>
                scheduler.ResumeScheduledRecurringSendAsync(missingValue!, "group", TestContext.Current.CancellationToken));
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [RequirementCoverage("REQ-VSB-RECURRING-SCHEDULER", "control-command-constructors-reject-invalid-identities")]
    public void ControlCommandConstructors_RejectMissingScheduleIdentifiers(string? missingValue)
    {
        var timestamp = new DateTimeOffset(2039, 1, 2, 3, 4, 5, TimeSpan.Zero);

        Assert.ThrowsAny<ArgumentException>(() =>
            new CancelScheduledRecurringMessageCommand(missingValue!, "group", timestamp));
        Assert.ThrowsAny<ArgumentException>(() =>
            new PauseScheduledRecurringMessageCommand("schedule", missingValue!, timestamp));
        Assert.ThrowsAny<ArgumentException>(() =>
            new ResumeScheduledRecurringMessageCommand(missingValue!, "group", timestamp));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RECURRING-SCHEDULER", "endpoint-factories-reject-null")]
    public void EndpointFactories_RejectNullEndpoints()
    {
        Assert.Equal("endpoint", Assert.Throws<ArgumentNullException>(() =>
            RecurringMessageSchedulerExtensions.CreateRecurringMessageScheduler((ISendEndpoint)null!)).ParamName);
        Assert.Equal("endpoint", Assert.Throws<ArgumentNullException>(() =>
            RecurringMessageSchedulerExtensions.CreateRecurringMessageScheduler((IPublishEndpoint)null!)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RECURRING-SCHEDULER", "endpoint-factories-preserve-kind-clock-and-topology")]
    public async Task EndpointFactories_PreserveSchedulerKindClockAndTopologyAsync()
    {
        ISendEndpoint sendEndpoint = DispatchProxy.Create<ISendEndpoint, UnexpectedInvocationProxy>();
        IPublishEndpoint publishEndpoint = DispatchProxy.Create<IPublishEndpoint, UnexpectedInvocationProxy>();
        IBusTopology busTopology = DispatchProxy.Create<IBusTopology, MissingPublishAddressTopologyProxy>();
        RecurringSchedule schedule = DispatchProxy.Create<RecurringSchedule, UnexpectedInvocationProxy>();
        var timeProvider = new TestTimeProvider();

        var sendScheduler = Assert.IsType<EndpointRecurringMessageScheduler>(
            sendEndpoint.CreateRecurringMessageScheduler(busTopology, timeProvider));
        var publishScheduler = Assert.IsType<PublishRecurringMessageScheduler>(
            publishEndpoint.CreateRecurringMessageScheduler(busTopology, timeProvider));

        Assert.Same(timeProvider, sendScheduler.TimeProvider);
        Assert.Same(timeProvider, publishScheduler.TimeProvider);

        Assert.Contains("publish address", (await Assert.ThrowsAsync<ArgumentException>(() =>
            sendScheduler.ScheduleRecurringPublishAsync(schedule, new ScheduledMessage(), CancellationToken.None))).Message);
        Assert.Contains("publish address", (await Assert.ThrowsAsync<ArgumentException>(() =>
            publishScheduler.ScheduleRecurringPublishAsync(schedule, new ScheduledMessage(), CancellationToken.None))).Message);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RECURRING-SCHEDULER", "every-scheduling-overload-validates-schedule-first")]
    public async Task EverySchedulingOverload_RejectsANullScheduleBeforeOtherInputsAsync()
    {
        ISendEndpoint sendEndpoint = DispatchProxy.Create<ISendEndpoint, UnexpectedInvocationProxy>();
        IPublishEndpoint publishEndpoint = DispatchProxy.Create<IPublishEndpoint, UnexpectedInvocationProxy>();
        object[] schedulers =
        [
            new EndpointRecurringMessageScheduler(sendEndpoint),
            new PublishRecurringMessageScheduler(publishEndpoint)
        ];
        var inspectedOverloads = 0;

        foreach (object scheduler in schedulers)
        {
            MethodInfo[] methods = scheduler.GetType()
                .GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .Where(method => method.Name.StartsWith("ScheduleRecurring", StringComparison.Ordinal)
                    && method.GetParameters().Any(parameter => parameter.Name == "schedule"))
                .ToArray();

            Assert.NotEmpty(methods);

            foreach (MethodInfo candidate in methods)
            {
                MethodInfo method = candidate.IsGenericMethodDefinition
                    ? candidate.MakeGenericMethod(typeof(ScheduledMessage))
                    : candidate;
                object?[] arguments = method.GetParameters()
                    .Select<ParameterInfo, object?>(parameter => parameter.ParameterType == typeof(Uri)
                        ? DestinationAddress
                        : parameter.ParameterType == typeof(CancellationToken)
                            ? CancellationToken.None
                            : null)
                    .ToArray();

                Exception exception = await CaptureInvocationExceptionAsync(method, scheduler, arguments);

                Assert.Equal("schedule", Assert.IsType<ArgumentNullException>(exception).ParamName);
                inspectedOverloads++;
            }
        }

        Assert.Equal(40, inspectedOverloads);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RECURRING-SCHEDULER", "runtime-typed-recurring-command-preserves-contract-and-payload")]
    public async Task RuntimeTypedRecurringSend_PreservesTheActualContractAndPayloadAsync()
    {
        IRecurringMessageScheduler scheduler = CreateRecordingScheduler(out RecordingSendEndpointProxy recording);
        RecurringSchedule schedule = DispatchProxy.Create<RecurringSchedule, UnexpectedInvocationProxy>();
        var payload = new RecurringPayload("order-42");
        using var cancellation = new CancellationTokenSource();

        ScheduledRecurringMessage runtime = await scheduler.ScheduleRecurringSendAsync(
            DestinationAddress, schedule, (object)payload, cancellation.Token);
        var runtimeCommand = Assert.IsType<ScheduleRecurringMessageCommand<RecurringPayload>>(
            Assert.Single(recording.Calls).Arguments[0]);
        Assert.Same(schedule, runtimeCommand.Schedule);
        Assert.Equal(DestinationAddress, runtimeCommand.Destination);
        Assert.Same(payload, runtimeCommand.Payload);
        Assert.Contains(MessageUrn.ForTypeString<RecurringPayload>(), runtimeCommand.PayloadType);
        Assert.Equal(typeof(ScheduleRecurringMessage), recording.Calls[0].Method.GetGenericArguments()[0]);
        Assert.Equal(cancellation.Token, recording.Calls[0].Arguments[^1]);
        var runtimeHandle = Assert.IsType<ScheduledRecurringMessageHandle<RecurringPayload>>(runtime);
        Assert.Same(payload, runtimeHandle.Payload);
        Assert.Same(schedule, runtimeHandle.Schedule);
        Assert.Equal(DestinationAddress, runtimeHandle.Destination);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RECURRING-SCHEDULER", "declared-recurring-contract-preserves-pipe-and-cancellation")]
    public async Task DeclaredRecurringContract_PreservesTheSelectedContractPipeAndCancellationAsync()
    {
        IRecurringMessageScheduler scheduler = CreateRecordingScheduler(out RecordingSendEndpointProxy recording);
        RecurringSchedule schedule = DispatchProxy.Create<RecurringSchedule, UnexpectedInvocationProxy>();
        var payload = new RecurringPayload("order-42");
        IPipe<SendContext> pipe = DispatchProxy.Create<IPipe<SendContext>, UnexpectedInvocationProxy>();
        using var cancellation = new CancellationTokenSource();

        ScheduledRecurringMessage declared = await scheduler.ScheduleRecurringSendAsync(
            DestinationAddress, schedule, (object)payload, typeof(IRecurringPayload), pipe, cancellation.Token);
        var call = Assert.Single(recording.Calls);
        var declaredCommand = Assert.IsType<ScheduleRecurringMessageCommand<IRecurringPayload>>(call.Arguments[0]);
        Assert.Same(payload, declaredCommand.Payload);
        Assert.Same(schedule, declaredCommand.Schedule);
        Assert.Equal(DestinationAddress, declaredCommand.Destination);
        Assert.Equal([MessageUrn.ForTypeString<IRecurringPayload>()], declaredCommand.PayloadType);
        Assert.DoesNotContain(MessageUrn.ForTypeString<RecurringPayload>(), declaredCommand.PayloadType);
        Assert.Same(pipe, call.Arguments[1]);
        Assert.Equal(cancellation.Token, call.Arguments[^1]);
        Assert.Equal(typeof(ScheduleRecurringMessage), call.Method.GetGenericArguments()[0]);
        var declaredHandle = Assert.IsType<ScheduledRecurringMessageHandle<IRecurringPayload>>(declared);
        Assert.Same(payload, declaredHandle.Payload);
        Assert.Same(schedule, declaredHandle.Schedule);
        Assert.Equal(DestinationAddress, declaredHandle.Destination);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RECURRING-SCHEDULER", "publish-declared-contract-preserves-command-pipe-and-handle")]
    public async Task PublishDeclaredRecurringContract_PreservesCommandPipeAndHandleAsync()
    {
        IRecurringMessageScheduler scheduler = CreateRecordingPublishScheduler(out RecordingPublishEndpointProxy recording);
        RecurringSchedule schedule = DispatchProxy.Create<RecurringSchedule, UnexpectedInvocationProxy>();
        var payload = new RecurringPayload("order-43");
        IPipe<SendContext> pipe = DispatchProxy.Create<IPipe<SendContext>, UnexpectedInvocationProxy>();
        using var cancellation = new CancellationTokenSource();

        ScheduledRecurringMessage handle = await scheduler.ScheduleRecurringSendAsync(
            DestinationAddress, schedule, (object)payload, typeof(IRecurringPayload), pipe, cancellation.Token);

        var call = Assert.Single(recording.Calls);
        var command = Assert.IsType<ScheduleRecurringMessageCommand<IRecurringPayload>>(call.Arguments[0]);
        Assert.Same(schedule, command.Schedule);
        Assert.Same(payload, command.Payload);
        Assert.Equal(DestinationAddress, command.Destination);
        Assert.Equal([MessageUrn.ForTypeString<IRecurringPayload>()], command.PayloadType);
        Assert.Same(pipe, call.Arguments[1]);
        Assert.Equal(cancellation.Token, call.Arguments[^1]);
        Assert.Equal(typeof(ScheduleRecurringMessage), call.Method.GetGenericArguments()[0]);
        var typedHandle = Assert.IsType<ScheduledRecurringMessageHandle<IRecurringPayload>>(handle);
        Assert.Same(schedule, typedHandle.Schedule);
        Assert.Same(payload, typedHandle.Payload);
        Assert.Equal(DestinationAddress, typedHandle.Destination);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RECURRING-SCHEDULER", "publish-failure-does-not-return-recurring-handle")]
    public async Task PublishDeclaredRecurringContract_PropagatesPublicationFailureAsync()
    {
        IRecurringMessageScheduler scheduler = CreateRecordingPublishScheduler(out RecordingPublishEndpointProxy recording);
        RecurringSchedule schedule = DispatchProxy.Create<RecurringSchedule, UnexpectedInvocationProxy>();
        var failure = new InvalidOperationException("publication rejected");
        recording.Failure = failure;
        IPipe<SendContext> pipe = DispatchProxy.Create<IPipe<SendContext>, UnexpectedInvocationProxy>();

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            scheduler.ScheduleRecurringSendAsync(DestinationAddress, schedule,
                (object)new RecurringPayload("order-44"), typeof(IRecurringPayload), pipe, TestContext.Current.CancellationToken));

        Assert.Same(failure, exception);
        Assert.Single(recording.Calls);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-RECURRING-SCHEDULER", "declared-recurring-contract-rejects-mismatched-payload-before-send")]
    public async Task DeclaredRecurringContract_RejectsMismatchedPayloadBeforeSendingAsync(bool withPipe)
    {
        IRecurringMessageScheduler scheduler = CreateRecordingScheduler(out RecordingSendEndpointProxy recording);
        RecurringSchedule schedule = DispatchProxy.Create<RecurringSchedule, UnexpectedInvocationProxy>();
        IPipe<SendContext> pipe = DispatchProxy.Create<IPipe<SendContext>, UnexpectedInvocationProxy>();

        ArgumentException failure = withPipe
            ? await Assert.ThrowsAsync<ArgumentException>(() => scheduler.ScheduleRecurringSendAsync(
                DestinationAddress, schedule, new object(), typeof(IRecurringPayload), pipe, TestContext.Current.CancellationToken))
            : await Assert.ThrowsAsync<ArgumentException>(() => scheduler.ScheduleRecurringSendAsync(
                DestinationAddress, schedule, new object(), typeof(IRecurringPayload), TestContext.Current.CancellationToken));

        Assert.Contains("Unexpected message type", failure.Message, StringComparison.Ordinal);
        Assert.Empty(recording.Calls);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-RECURRING-SCHEDULER", "runtime-recurring-publish-resolves-destination-and-preserves-command")]
    public async Task RuntimeRecurringPublish_ResolvesDestinationAndPreservesTheCommandAsync(bool publishControlCommand)
    {
        IBusTopology topology = DispatchProxy.Create<IBusTopology, RecordingPublishAddressTopologyProxy>();
        var addressLookup = (RecordingPublishAddressTopologyProxy)(object)topology;
        RecordingSendEndpointProxy? sendRecording = null;
        RecordingPublishEndpointProxy? publishRecording = null;
        IRecurringMessageScheduler scheduler = publishControlCommand
            ? CreateRecordingPublishScheduler(out publishRecording, topology)
            : CreateRecordingScheduler(out sendRecording, topology);
        RecurringSchedule schedule = DispatchProxy.Create<RecurringSchedule, UnexpectedInvocationProxy>();
        var payload = new RecurringPayload("publish-order-45");
        using var cancellation = new CancellationTokenSource();

        ScheduledRecurringMessage handle = await scheduler.ScheduleRecurringPublishAsync(schedule, (object)payload, cancellation.Token);

        Assert.Equal(typeof(RecurringPayload), Assert.Single(addressLookup.Lookups));
        var call = Assert.Single(publishControlCommand ? publishRecording!.Calls : sendRecording!.Calls);
        var command = Assert.IsType<ScheduleRecurringMessageCommand<RecurringPayload>>(call.Arguments[0]);
        Assert.Same(schedule, command.Schedule);
        Assert.Same(payload, command.Payload);
        Assert.Equal(DestinationAddress, command.Destination);
        Assert.Contains(MessageUrn.ForTypeString<RecurringPayload>(), command.PayloadType);
        Assert.Contains(MessageUrn.ForTypeString<IRecurringPayload>(), command.PayloadType);
        Assert.Equal(2, command.PayloadType.Length);
        Assert.Equal(typeof(ScheduleRecurringMessage), call.Method.GetGenericArguments()[0]);
        Assert.Equal(cancellation.Token, call.Arguments[^1]);
        var typedHandle = Assert.IsType<ScheduledRecurringMessageHandle<RecurringPayload>>(handle);
        Assert.Same(schedule, typedHandle.Schedule);
        Assert.Same(payload, typedHandle.Payload);
        Assert.Equal(DestinationAddress, typedHandle.Destination);
    }

    private static IRecurringMessageScheduler CreateRecordingScheduler(out RecordingSendEndpointProxy recording, IBusTopology? topology = null)
    {
        ISendEndpoint endpoint = DispatchProxy.Create<IAdvancedSendEndpoint, RecordingSendEndpointProxy>();
        recording = (RecordingSendEndpointProxy)(object)endpoint;
        return new EndpointRecurringMessageScheduler(endpoint, topology);
    }

    private static IRecurringMessageScheduler CreateRecordingPublishScheduler(out RecordingPublishEndpointProxy recording, IBusTopology? topology = null)
    {
        IPublishEndpoint endpoint = DispatchProxy.Create<AdvancedPublishEndpoint, RecordingPublishEndpointProxy>();
        recording = (RecordingPublishEndpointProxy)(object)endpoint;
        return new PublishRecurringMessageScheduler(endpoint, topology);
    }

    private static async Task<Exception> CaptureInvocationExceptionAsync(MethodInfo method, object target, object?[] arguments)
    {
        try
        {
            object? result = method.Invoke(target, arguments);
            if (result is Task task)
                await task;
        }
        catch (TargetInvocationException exception) when (exception.InnerException is not null)
        {
            return exception.InnerException;
        }
        catch (Exception exception)
        {
            return exception;
        }

        throw new InvalidOperationException($"{method.Name} accepted a null recurring schedule.");
    }

    private sealed record ScheduledMessage;

    private interface IRecurringPayload
    {
        string Id { get; }
    }

    private sealed record RecurringPayload(string Id) : IRecurringPayload;

    private interface AdvancedPublishEndpoint : IPublishEndpoint, IAdvancedPublishEndpoint;

    private sealed class TestTimeProvider : TimeProvider
    {
    }

    private class MissingPublishAddressTopologyProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod?.Name == nameof(IBusTopology.TryGetPublishAddress))
                return false;

            throw new InvalidOperationException($"The topology boundary invoked {targetMethod?.Name}.");
        }
    }

    private class RecordingPublishAddressTopologyProxy : DispatchProxy
    {
        public List<Type> Lookups { get; } = [];

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod?.Name != nameof(IBusTopology.TryGetPublishAddress)
                || args is not { Length: 2 }
                || args[0] is not Type messageType)
                throw new InvalidOperationException($"The topology boundary invoked {targetMethod?.Name}.");

            Lookups.Add(messageType);
            args[1] = DestinationAddress;
            return true;
        }
    }

    private class UnexpectedInvocationProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new InvalidOperationException($"The invalid boundary invoked {targetMethod?.Name}.");
    }

    private class RecordingSendEndpointProxy : DispatchProxy
    {
        public List<(MethodInfo Method, object?[] Arguments)> Calls { get; } = [];

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod?.Name != nameof(ISendEndpoint.SendAsync) || args is null)
                throw new InvalidOperationException($"The recurring scheduler unexpectedly invoked {targetMethod?.Name}.");

            Calls.Add((targetMethod, args.ToArray()));
            return Task.CompletedTask;
        }
    }

    private class RecordingPublishEndpointProxy : DispatchProxy
    {
        public List<(MethodInfo Method, object?[] Arguments)> Calls { get; } = [];

        public Exception? Failure { get; set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod?.Name != nameof(IPublishEndpoint.PublishAsync) || args is null)
                throw new InvalidOperationException($"The recurring scheduler unexpectedly invoked {targetMethod?.Name}.");

            Calls.Add((targetMethod, args.ToArray()));
            return Failure is null ? Task.CompletedTask : Task.FromException(Failure);
        }
    }
}
