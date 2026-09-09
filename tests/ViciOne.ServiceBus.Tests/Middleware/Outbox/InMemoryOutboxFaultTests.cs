using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Middleware.Outbox;

public sealed class InMemoryOutboxFaultTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-INMEMORY-OUTBOX-FAULT", "discard-response-before-fault")]
    public async Task HandlerFault_DiscardsItsDeferredResponseBeforePublishingTheRequestFaultAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var handlerEntered = new TaskCompletionSource<ConsumeContext<OutboxRequest>>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        using var harness = new InMemoryTestHarness($"outbox-fault-{NewId.NextGuid():N}")
        {
            TestTimeout = timeout,
            TestInactivityTimeout = timeout,
        };
        harness.OnConfigureInMemoryReceiveEndpoint += configurator =>
        {
            configurator.UseVolatileOutbox();
            configurator.Handler<OutboxRequest>(async context =>
            {
                handlerEntered.TrySetResult(context);
                await context.RespondAsync(new OutboxResponse(context.Message.CorrelationId));
                throw new ExpectedHandlerException();
            });
        };

        try
        {
            await harness.StartAsync(cancellationToken).WaitAsync(timeout, cancellationToken);
            Guid correlationId = NewId.NextGuid();
            IRequestClient<OutboxRequest> client = harness.Bus.CreateRequestClient<OutboxRequest>(
                harness.InputQueueAddress,
                new RequestTimeout(timeout));

            RequestFaultException fault = await Assert.ThrowsAsync<RequestFaultException>(() =>
                client.GetResponseAsync<OutboxResponse>(
                    new OutboxRequest(correlationId),
                    cancellationToken));
            ConsumeContext<OutboxRequest> consumed = await handlerEntered.Task.WaitAsync(
                timeout,
                cancellationToken);
            ISentMessage<Fault<OutboxRequest>> sentFault = Assert.Single(
                harness.Sent.Select<Fault<OutboxRequest>>(SnapshotOnlyToken()));

            Assert.Equal(correlationId, consumed.Message.CorrelationId);
            Assert.Equal(consumed.RequestId, sentFault.Context.RequestId);
            Assert.Contains(
                sentFault.Context.Message.Exceptions,
                exception => exception.ExceptionType == TypeCache<ExpectedHandlerException>.ShortName);
            Assert.Contains(TypeCache<OutboxRequest>.ShortName, fault.Message);
            Assert.Empty(harness.Sent.Select<OutboxResponse>(SnapshotOnlyToken()));
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken).WaitAsync(timeout, CancellationToken.None);
        }
    }

    private static CancellationToken SnapshotOnlyToken() => new(canceled: true);

    private static TimeSpan OperationTimeout() => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions()
        .OperationTimeout!.Value;

    private sealed record OutboxRequest(Guid CorrelationId) : CorrelatedBy<Guid>;

    private sealed record OutboxResponse(Guid CorrelationId) : CorrelatedBy<Guid>;

    private sealed class ExpectedHandlerException : Exception;
}
