using System.Collections.Generic;
using System.Reflection;
using RabbitMQ.Client;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.RabbitMq.Tests.RabbitMqTransport;

public sealed class RabbitMqChannelPublishCancellationTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-RABBITMQ-CHANNEL-PUBLISH", "no-ack-shared-token-remains-linked")]
    public async Task SharedNoAckPublish_RetainsCallerAndOwnerCancellationUntilTheSdkFinishes(bool cancelOwner)
    {
        using var owner = new CancellationTokenSource();
        using var caller = new CancellationTokenSource();
        ChannelContext underlying = DispatchProxy.Create<ChannelContext, PendingPublishContext>();
        var probe = (PendingPublishContext)(object)underlying;
        var shared = new SharedChannelContext(underlying, owner.Token);

        try
        {
            Task returned = shared.BasicPublishAsync("orders", "route", true, new BasicProperties(), [1], false, caller.Token);

            Assert.True(returned.IsCompletedSuccessfully);
            Assert.False(probe.ProviderPublish.IsCompleted);
            Assert.False(probe.ProviderToken.IsCancellationRequested);

            if (cancelOwner)
                owner.Cancel();
            else
                caller.Cancel();

            Assert.True(probe.ProviderToken.IsCancellationRequested);
        }
        finally
        {
            probe.Complete();
            await probe.ProviderPublish;
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [RequirementCoverage("REQ-VSB-RABBITMQ-CHANNEL-PUBLISH", "no-ack-scope-disposal-preserves-inflight-cancellation")]
    public async Task ScopedNoAckPublish_AfterScopeDisposalStillObservesTheOriginalCancellation(int canceledSource)
    {
        using var parent = new CancellationTokenSource();
        using var scopeOwner = new CancellationTokenSource();
        using var caller = new CancellationTokenSource();
        ChannelContext underlying = DispatchProxy.Create<ChannelContext, PendingPublishContext>();
        var probe = (PendingPublishContext)(object)underlying;
        probe.OwnerToken = parent.Token;
        var scope = new ScopeChannelContext(underlying, scopeOwner.Token);

        try
        {
            Task returned = scope.BasicPublishAsync("orders", "route", true, new BasicProperties(), [2], false, caller.Token);

            Assert.True(returned.IsCompletedSuccessfully);
            Assert.False(probe.ProviderPublish.IsCompleted);
            scope.Dispose();

            switch (canceledSource)
            {
                case 0: parent.Cancel(); break;
                case 1: scopeOwner.Cancel(); break;
                default: caller.Cancel(); break;
            }

            Assert.True(probe.ProviderToken.IsCancellationRequested);
        }
        finally
        {
            probe.Complete();
            await probe.ProviderPublish;
            scope.Dispose();
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-CHANNEL-PUBLISH", "awaited-publish-retains-provider-failure")]
    public async Task AwaitedPublish_WaitsAndPropagatesTheOriginalProviderFailure()
    {
        ChannelContext underlying = DispatchProxy.Create<ChannelContext, PendingPublishContext>();
        var probe = (PendingPublishContext)(object)underlying;
        var shared = new SharedChannelContext(underlying, CancellationToken.None);
        var expected = new InvalidOperationException("provider publish failed");

        Task publish = shared.BasicPublishAsync("orders", "route", true, new BasicProperties(), [3], true, CancellationToken.None);
        Assert.False(publish.IsCompleted);
        probe.Fail(expected);

        InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(() => publish);
        Assert.Same(expected, actual);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-CHANNEL-PUBLISH", "concurrent-scope-publishes-retain-parent-link")]
    public async Task ScopeDisposal_FirstCompletedPublishDoesNotDetachSecondPublish()
    {
        using var parent = new CancellationTokenSource();
        using var scopeOwner = new CancellationTokenSource();
        ChannelContext underlying = DispatchProxy.Create<ChannelContext, PendingPublishContext>();
        var probe = (PendingPublishContext)(object)underlying;
        probe.OwnerToken = parent.Token;
        var scope = new ScopeChannelContext(underlying, scopeOwner.Token);

        try
        {
            Task first = scope.BasicPublishAsync("orders", "first", true, new BasicProperties(), [1], false, CancellationToken.None);
            Task second = scope.BasicPublishAsync("orders", "second", true, new BasicProperties(), [2], false, CancellationToken.None);
            Assert.True(first.IsCompletedSuccessfully);
            Assert.True(second.IsCompletedSuccessfully);
            Assert.Equal(2, probe.PublishCount);
            Assert.False(probe.ProviderPublishAt(1).IsCompleted);

            scope.Dispose();
            probe.Complete(0);
            parent.Cancel();

            Assert.False(probe.ProviderTokenAt(0).IsCancellationRequested);
            Assert.True(probe.ProviderTokenAt(1).IsCancellationRequested);
        }
        finally
        {
            probe.Complete(0);
            probe.Complete(1);
            await Task.WhenAll(probe.ProviderPublishAt(0), probe.ProviderPublishAt(1));
            scope.Dispose();
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-CHANNEL-PUBLISH", "completed-no-ack-publish-releases-links")]
    public async Task CompletedNoAckPublish_ReleasesSharedAndScopeCancellationLinks()
    {
        using var owner = new CancellationTokenSource();
        using var parent = new CancellationTokenSource();
        ChannelContext sharedUnderlying = DispatchProxy.Create<ChannelContext, PendingPublishContext>();
        ChannelContext scopedUnderlying = DispatchProxy.Create<ChannelContext, PendingPublishContext>();
        var sharedProbe = (PendingPublishContext)(object)sharedUnderlying;
        var scopedProbe = (PendingPublishContext)(object)scopedUnderlying;
        scopedProbe.OwnerToken = parent.Token;
        var shared = new SharedChannelContext(sharedUnderlying, owner.Token);
        using var scope = new ScopeChannelContext(scopedUnderlying, CancellationToken.None);

        Task sharedReturn = shared.BasicPublishAsync("orders", "shared", true, new BasicProperties(), [1], false, CancellationToken.None);
        Task scopedReturn = scope.BasicPublishAsync("orders", "scoped", true, new BasicProperties(), [2], false, CancellationToken.None);
        Assert.True(sharedReturn.IsCompletedSuccessfully);
        Assert.True(scopedReturn.IsCompletedSuccessfully);

        scope.Dispose();
        sharedProbe.Complete();
        scopedProbe.Complete();
        await Task.WhenAll(sharedProbe.ProviderPublish, scopedProbe.ProviderPublish);
        owner.Cancel();
        parent.Cancel();

        Assert.False(sharedProbe.ProviderToken.IsCancellationRequested);
        Assert.False(scopedProbe.ProviderToken.IsCancellationRequested);
        Assert.False(scope.CancellationToken.IsCancellationRequested);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-CHANNEL-PUBLISH", "awaited-scope-failure-releases-links")]
    public async Task AwaitedScopePublish_PropagatesFailureAndReleasesCancellationLinks()
    {
        using var parent = new CancellationTokenSource();
        ChannelContext underlying = DispatchProxy.Create<ChannelContext, PendingPublishContext>();
        var probe = (PendingPublishContext)(object)underlying;
        probe.OwnerToken = parent.Token;
        using var scope = new ScopeChannelContext(underlying, CancellationToken.None);
        var expected = new InvalidOperationException("scope publish failed");

        Task publish = scope.BasicPublishAsync("orders", "route", true, new BasicProperties(), [3], true, CancellationToken.None);
        Assert.False(publish.IsCompleted);
        scope.Dispose();
        probe.Fail(expected);

        InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(() => publish);
        Assert.Same(expected, actual);
        parent.Cancel();
        Assert.False(probe.ProviderToken.IsCancellationRequested);
        Assert.False(scope.CancellationToken.IsCancellationRequested);
    }

    public class PendingPublishContext : DispatchProxy
    {
        readonly List<TaskCompletionSource> _publishes = [];
        readonly List<CancellationToken> _providerTokens = [];

        public CancellationToken OwnerToken { get; set; }
        public CancellationToken ProviderToken => ProviderTokenAt(0);
        public Task ProviderPublish => ProviderPublishAt(0);
        public int PublishCount => _publishes.Count;
        public CancellationToken ProviderTokenAt(int index) => _providerTokens[index];
        public Task ProviderPublishAt(int index) => _publishes[index].Task;

        public void Complete(int index = 0) => _publishes[index].TrySetResult();
        public void Fail(Exception exception) => _publishes[0].TrySetException(exception);

        protected override object? Invoke(MethodInfo? method, object?[]? args) => method?.Name switch
        {
            "get_CancellationToken" => OwnerToken,
            "BasicPublishAsync" => Publish(args!),
            _ => throw new NotSupportedException($"Unexpected channel operation: {method?.Name}"),
        };

        private Task Publish(object?[] args)
        {
            var publish = new TaskCompletionSource();
            _publishes.Add(publish);
            _providerTokens.Add((CancellationToken)args[6]!);
            return (bool)args[5]! ? publish.Task : Task.CompletedTask;
        }
    }
}
