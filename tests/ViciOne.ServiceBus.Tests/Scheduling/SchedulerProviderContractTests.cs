using System.Reflection;
using System.Net.Mime;
using ViciOne.ServiceBus.Advanced.Serialization;
using ViciOne.ServiceBus.InMemoryTransport.Runtime;
using ViciOne.ServiceBus.Scheduling;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Scheduling;

public sealed class SchedulerProviderContractTests
{
    private static readonly Uri Destination = new("loopback://localhost/scheduler-provider-boundary");
    private static readonly DateTimeOffset DueAt = new(2039, 1, 2, 3, 4, 5, TimeSpan.Zero);

    [Fact]
    [RequirementCoverage("REQ-VSB-SCHEDULER-PROVIDER-BOUNDARY", "constructors-reject-missing-collaborators")]
    public void Constructors_RejectMissingCollaborators()
    {
        Assert.Equal("sendEndpointProvider", Assert.Throws<ArgumentNullException>(() =>
            new DelayedScheduleMessageProvider(null!)).ParamName);
        Assert.Equal("schedulerEndpoint", Assert.Throws<ArgumentNullException>(() =>
            new EndpointScheduleMessageProvider(null!)).ParamName);
        Assert.Equal("publishEndpoint", Assert.Throws<ArgumentNullException>(() =>
            new PublishScheduleMessageProvider(null!)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SCHEDULER-PROVIDER-BOUNDARY", "schedule-inputs-fail-before-dispatch")]
    public async Task ScheduleInputs_FailBeforeProviderDispatchAsync()
    {
        ISendEndpointProvider endpoints = DispatchProxy.Create<ISendEndpointProvider, UnexpectedInvocationProxy>();
        IPublishEndpoint publisher = DispatchProxy.Create<IPublishEndpoint, UnexpectedInvocationProxy>();
        IScheduleMessageProvider[] providers =
        [
            new DelayedScheduleMessageProvider(endpoints),
            new EndpointScheduleMessageProvider(_ => throw new InvalidOperationException("The endpoint resolver must not run.")),
            new PublishScheduleMessageProvider(publisher),
        ];

        foreach (IScheduleMessageProvider provider in providers)
        {
            Assert.Equal("destinationAddress", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
                provider.ScheduleSendAsync(null!, DueAt, new Probe(), Pipe.Empty<SendContext<Probe>>(),
                    TestContext.Current.CancellationToken))).ParamName);
            Assert.Equal("message", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
                provider.ScheduleSendAsync<Probe>(Destination, DueAt, null!, Pipe.Empty<SendContext<Probe>>(),
                    TestContext.Current.CancellationToken))).ParamName);
            Assert.Equal("pipe", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
                provider.ScheduleSendAsync(Destination, DueAt, new Probe(), null!,
                    TestContext.Current.CancellationToken))).ParamName);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SCHEDULER-PROVIDER-BOUNDARY", "accepted-command-follows-configured-token")]
    public async Task AcceptedCommand_UsesConfiguredTokenAcrossWireMetadataAndHandleAsync()
    {
        var payload = new Probe();
        Guid configuredToken = NewId.NextGuid();
        DateTimeOffset localDueAt = new(2039, 1, 2, 5, 4, 5, TimeSpan.FromHours(2));
        ScheduleMessage? sentCommand = null;
        InMemorySendContext<ScheduleMessage>? sentContext = null;
        using var cancellation = new CancellationTokenSource();
        var provider = new RecordingProvider(async (command, pipe, token) =>
        {
            Assert.Equal(cancellation.Token, token);
            sentCommand = command;
            sentContext = new InMemorySendContext<ScheduleMessage>(command, token);
            await pipe.SendAsync(sentContext);
        });

        ScheduledMessage<Probe> accepted = await provider.ScheduleSendAsync(Destination, localDueAt, payload,
            Pipe.Execute<SendContext<Probe>>(context =>
            {
                Assert.Same(payload, context.Message);
                context.ScheduledMessageId = configuredToken;
                context.Headers.Set("scheduler-probe", "configured");
            }), cancellation.Token);

        Assert.NotNull(sentCommand);
        Assert.NotNull(sentContext);
        Assert.Equal(configuredToken, sentCommand.TokenId);
        Assert.Equal(configuredToken, sentContext.ScheduledMessageId);
        Assert.Equal(configuredToken, sentContext.CorrelationId);
        Assert.True(sentContext.Headers.TryGetHeader(MessageHeaders.SchedulingTokenId, out object? tokenHeader));
        Assert.Equal(configuredToken.ToString("D"), tokenHeader);
        Assert.True(sentContext.Headers.TryGetHeader("scheduler-probe", out object? configuredHeader));
        Assert.Equal("configured", configuredHeader);
        Assert.Equal(configuredToken, accepted.TokenId);
        Assert.Equal(localDueAt.ToUniversalTime(), sentCommand.DueAt);
        Assert.Equal(sentCommand.DueAt, accepted.DueAt);
        Assert.Equal(Destination, sentCommand.Destination);
        Assert.Equal(Destination, accepted.Destination);
        Assert.Same(payload, sentCommand.Payload);
        Assert.Same(payload, accepted.Payload);
        Assert.Equal(MessageTypeCache<Probe>.MessageTypeNames.ToArray(), sentCommand.PayloadType);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SCHEDULER-PROVIDER-BOUNDARY", "dispatch-without-pipe-cannot-accept")]
    public async Task DispatchWithoutApplyingPipe_CannotReturnAcceptedHandleAsync()
    {
        var provider = new RecordingProvider((_, _, _) => Task.CompletedTask);

        InvalidOperationException error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            provider.ScheduleSendAsync(Destination, DueAt, new Probe(), Pipe.Empty<SendContext<Probe>>(),
                TestContext.Current.CancellationToken));

        Assert.Contains("without applying its scheduling pipe", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SCHEDULER-PROVIDER-BOUNDARY", "serialized-command-rejects-token-change")]
    public async Task SerializedCommand_RejectsTokenChangeBeforeAcceptanceAsync()
    {
        Guid replacementToken = NewId.NextGuid();
        var provider = new RecordingProvider(async (command, pipe, token) =>
        {
            var context = new InMemorySendContext<ScheduleMessage>(command, token)
            {
                Serializer = new CopyBodySerializer(new ContentType("application/octet-stream"),
                    new BinaryMessageBody(new byte[] { 1, 2, 3 })),
            };
            Assert.Equal(3, context.Body.Length);
            await pipe.SendAsync(context);
        });

        InvalidOperationException error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            provider.ScheduleSendAsync(Destination, DueAt, new Probe(),
                Pipe.Execute<SendContext<Probe>>(context => context.ScheduledMessageId = replacementToken),
                TestContext.Current.CancellationToken));

        Assert.Contains("cannot change after the command body has been serialized", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SCHEDULER-PROVIDER-BOUNDARY", "accepted-pipe-rejects-replay")]
    public async Task AcceptedPipe_RejectsReuseForAnotherDispatchAsync()
    {
        IPipe<SendContext<ScheduleMessage>>? capturedPipe = null;
        var provider = new RecordingProvider(async (command, pipe, token) =>
        {
            capturedPipe = pipe;
            await pipe.SendAsync(new InMemorySendContext<ScheduleMessage>(command, token));
        });

        ScheduledMessage<Probe> accepted = await provider.ScheduleSendAsync(Destination, DueAt, new Probe(),
            Pipe.Empty<SendContext<Probe>>(), TestContext.Current.CancellationToken);

        Assert.NotEqual(Guid.Empty, accepted.TokenId);
        Assert.NotNull(capturedPipe);
        InvalidOperationException error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            capturedPipe.SendAsync(new InMemorySendContext<ScheduleMessage>(new ScheduleMessageCommand())));
        Assert.Contains("cannot be applied to another send context", error.Message, StringComparison.Ordinal);
    }

    private sealed class RecordingProvider(
        Func<ScheduleMessage, IPipe<SendContext<ScheduleMessage>>, CancellationToken, Task> dispatch) :
        BaseScheduleMessageProvider
    {
        protected override Task ScheduleSendAsync(ScheduleMessage message,
            IPipe<SendContext<ScheduleMessage>> pipe, CancellationToken cancellationToken) =>
            dispatch(message, pipe, cancellationToken);

        protected override Task CancelScheduledSendAsync(Guid tokenId, Uri? destinationAddress,
            CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed record Probe;

    private class UnexpectedInvocationProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new InvalidOperationException($"The invalid boundary invoked {targetMethod?.Name}.");
    }
}
