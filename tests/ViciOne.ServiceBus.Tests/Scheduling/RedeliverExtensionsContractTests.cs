using System.Reflection;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Scheduling;

public sealed class RedeliverExtensionsContractTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-EXPLICIT-REDELIVERY", "missing-consume-context-is-rejected")]
    public async Task Redeliver_RejectsAMissingConsumeContextAsync()
    {
        ArgumentNullException exception = await Assert.ThrowsAsync<ArgumentNullException>(() =>
            RedeliverExtensions.RedeliverAsync<RedeliveryMessage>(
                null!,
                TimeSpan.Zero,
                cancellationToken: TestContext.Current.CancellationToken));

        Assert.Equal("context", exception.ParamName);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-EXPLICIT-REDELIVERY", "payload-receives-exact-call-and-task-identity")]
    public async Task Redeliver_ForwardsTheExactDelayCallbackTokenAndTaskToThePayloadAsync(bool includeCallback)
    {
        var redelivery = new RecordingRedeliveryContext();
        ConsumeContext<RedeliveryMessage> context = CreateContext(redelivery);
        TimeSpan delay = TimeSpan.FromMinutes(17);
        using var cancellation = new CancellationTokenSource();
        Action<ConsumeContext, SendContext>? callback = includeCallback ? static (_, _) => { } : null;

        Task actual = context.RedeliverAsync(delay, callback, cancellation.Token);

        Assert.Same(redelivery.Completion.Task, actual);
        Assert.Equal(delay, redelivery.Delay);
        Assert.Same(callback, redelivery.Callback);
        Assert.Equal(cancellation.Token, redelivery.CancellationToken);

        redelivery.Completion.SetResult(true);
        await actual;
    }

    private static ConsumeContext<RedeliveryMessage> CreateContext(MessageRedeliveryContext redelivery)
    {
        ConsumeContext<RedeliveryMessage> context =
            DispatchProxy.Create<ConsumeContext<RedeliveryMessage>, PayloadProxy>();
        ((PayloadProxy)(object)context).Redelivery = redelivery;
        return context;
    }

    private sealed record RedeliveryMessage;

    private sealed class RecordingRedeliveryContext : MessageRedeliveryContext
    {
        public Action<ConsumeContext, SendContext>? Callback { get; private set; }

        public CancellationToken CancellationToken { get; private set; }

        public TaskCompletionSource<bool> Completion { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TimeSpan Delay { get; private set; }

        public Task ScheduleRedeliveryAsync(TimeSpan delay, Action<ConsumeContext, SendContext>? callback = null,
            CancellationToken cancellationToken = default)
        {
            Delay = delay;
            Callback = callback;
            CancellationToken = cancellationToken;
            return Completion.Task;
        }
    }

    private class PayloadProxy : DispatchProxy
    {
        public MessageRedeliveryContext? Redelivery { get; set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod?.Name == "TryGetPayload"
                && targetMethod.GetGenericArguments() is [Type requestedType]
                && requestedType == typeof(MessageRedeliveryContext))
            {
                args![0] = Redelivery;
                return Redelivery is not null;
            }

            throw new InvalidOperationException($"The redelivery contract invoked {targetMethod?.Name}.");
        }
    }
}
