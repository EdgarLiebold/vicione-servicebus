using System.Text;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Mediator;
using ViciOne.ServiceBus.Mediator.Contexts;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Mediator.Contexts;

public sealed class MediatorReceiveContextContractTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-MEDIATOR-RECEIVE-CONTEXT", "content-type-is-owned-across-reads-deliveries-and-message-contracts")]
    public async Task ContentTypeMutation_IsIsolatedAcrossReadsDeliveriesAndMessageContractsAsync()
    {
        var receives = new List<ReceiveContext>();
        IMediator mediator = MediatorFactory.Create(configuration =>
        {
            configuration.Limits(MessageLimits.Conservative);
            configuration.Handler<ContextMessage>(context =>
            {
                receives.Add(context.Advanced().ReceiveContext);
                return Task.CompletedTask;
            });
            configuration.Handler<OtherContextMessage>(context =>
            {
                receives.Add(context.Advanced().ReceiveContext);
                return Task.CompletedTask;
            });
        });

        try
        {
            CancellationToken token = TestContext.Current.CancellationToken;
            var timeout = TimeSpan.FromSeconds(10);
            await mediator.SendAsync(new ContextMessage("first"), token).WaitAsync(timeout, token);
            await mediator.SendAsync(new ContextMessage("second"), token).WaitAsync(timeout, token);
            await mediator.SendAsync(new OtherContextMessage("other"), token).WaitAsync(timeout, token);
            Assert.Equal(3, receives.Count);
            var owned = receives[0].ContentType;

            try
            {
                owned.MediaType = "text/plain";
                owned.Parameters["profile"] = "caller-only";

                Assert.Equal("text/plain", owned.MediaType);
                Assert.Equal("caller-only", owned.Parameters["profile"]);
                foreach (ReceiveContext receive in receives)
                {
                    var firstRead = receive.ContentType;
                    var secondRead = receive.ContentType;
                    Assert.NotSame(owned, firstRead);
                    Assert.NotSame(firstRead, secondRead);
                    Assert.Equal("application/json", firstRead.MediaType);
                    Assert.Equal("application/json", secondRead.MediaType);
                    Assert.Null(firstRead.Parameters["profile"]);
                    Assert.Null(secondRead.Parameters["profile"]);
                }
            }
            finally
            {
                owned.MediaType = "application/json";
                owned.Parameters.Remove("profile");
            }
        }
        finally
        {
            await mediator.DisposeAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(10), CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MEDIATOR-RECEIVE-CONTEXT", "metadata-body-and-owned-completion")]
    public async Task MaterializedContext_ExposesExactMetadataAndAwaitsAttachedWorkAsync()
    {
        var captured = new TaskCompletionSource<MediatorReceiveContext<ContextMessage>>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var attached = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var message = new ContextMessage("expected");
        await using IMediator mediator = MediatorFactory.Create(configuration =>
        {
            configuration.Limits(MessageLimits.Conservative);
            configuration.Handler<ContextMessage>(context =>
            {
                var receiveContext = Assert.IsType<MediatorReceiveContext<ContextMessage>>(
                    context.Advanced().ReceiveContext);
                Assert.Throws<ArgumentNullException>(() => receiveContext.AddReceiveTask(null!));
                receiveContext.AddReceiveTask(attached.Task);
                captured.TrySetResult(receiveContext);
                return Task.CompletedTask;
            });
        });

        Task dispatch = mediator.SendAsync(message, TestContext.Current.CancellationToken);
        MediatorReceiveContext<ContextMessage> receive = await captured.Task.WaitAsync(
            TestContext.Current.CancellationToken);

        try
        {
            Assert.False(dispatch.IsCompleted);
            Assert.Equal(new Uri("loopback://localhost/mediator"), receive.InputAddress);
            Assert.Equal("application/json", receive.ContentType.MediaType);
            Assert.Equal("{\"value\":\"expected\"}", Encoding.UTF8.GetString(receive.Body.ToArray()));
            Assert.False(receive.Redelivered);
            Assert.False(receive.PublishFaults);
            Assert.False(receive.IsFaulted);
            Assert.NotNull(receive.PublishTopology);
            Assert.NotNull(receive.SendEndpointProvider);
            Assert.NotNull(receive.PublishEndpointProvider);
            Assert.NotNull(receive.TransportHeaders);
            Assert.True(receive.ElapsedTime >= TimeSpan.Zero);
        }
        finally
        {
            attached.TrySetResult();
        }

        await dispatch;
        await receive.ReceiveCompleted;

        Assert.True(receive.IsDelivered);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MEDIATOR-RECEIVE-CONTEXT", "notification-boundaries-and-cancellation")]
    public async Task Notifications_RejectInvalidInputsAndHonorCancellationBeforeChangingStateAsync()
    {
        var captured = new TaskCompletionSource<(MediatorReceiveContext<ContextMessage> Receive, ConsumeContext<ContextMessage> Consume)>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        await using IMediator mediator = MediatorFactory.Create(configuration =>
        {
            configuration.Limits(MessageLimits.Conservative);
            configuration.Handler<ContextMessage>(context =>
            {
                captured.TrySetResult((
                    Assert.IsType<MediatorReceiveContext<ContextMessage>>(context.Advanced().ReceiveContext),
                    context));
                return Task.CompletedTask;
            });
        });
        await mediator.SendAsync(new ContextMessage("boundary"), TestContext.Current.CancellationToken);
        var (receive, consume) = await captured.Task.WaitAsync(TestContext.Current.CancellationToken);
        CancellationToken testToken = TestContext.Current.CancellationToken;

        Assert.Throws<ArgumentNullException>(() =>
        {
            _ = receive.NotifyConsumedAsync<ContextMessage>(null!, TimeSpan.Zero, "consumer", testToken);
        });
        Assert.Throws<ArgumentException>(() =>
        {
            _ = receive.NotifyConsumedAsync(consume, TimeSpan.Zero, " ", testToken);
        });
        Assert.Throws<ArgumentNullException>(() =>
        {
            _ = receive.NotifyFaultedAsync(consume, TimeSpan.Zero, "consumer", null!, testToken);
        });
        Assert.Throws<ArgumentNullException>(() =>
        {
            _ = receive.NotifyFaultedAsync(null!, testToken);
        });

        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        OperationCanceledException canceled = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            receive.NotifyFaultedAsync(new InvalidOperationException("must not be observed"), cancellation.Token));

        Assert.Equal(cancellation.Token, canceled.CancellationToken);
        Assert.False(receive.IsFaulted);

        await receive.NotifyFaultedAsync(new InvalidOperationException("expected"), testToken);
        Assert.True(receive.IsFaulted);
    }

    private sealed record ContextMessage(string Value);
    private sealed record OtherContextMessage(string Value);
}
