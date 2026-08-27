namespace ViciOne.ServiceBus.AmazonSqsTransport.LocalIntegration.Tests;

using Amazon.SQS;
using Amazon.SQS.Model;
using System.Reflection;
using System.Runtime.ExceptionServices;
using ViciOne.ServiceBus.AmazonSqsTransport.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

public sealed class AmazonSqsReceiveLockTests
{
    [Fact]
    [RequirementCoverage("OBL-R0-CLOUD-0235", "one-completion-does-not-cancel-another-in-flight-renewal")]
    public async Task ReleasingOneLock_DoesNotReleaseSubsequentMessages()
    {
        await using AmazonSqsLocalStack fixture = AmazonSqsLocalStack.Create("receivelocks");
        string queueName = fixture.Name("input");
        Guid completedId = Guid.NewGuid();
        Guid heldId = Guid.NewGuid();
        var completedEntered = NewObservation();
        var heldEntered = NewObservation();
        var completedReceive = NewObservation<Task>();
        var releaseHeld = NewObservation();
        var heldReceive = NewObservation<Task>();
        var heldDeliveryCount = 0;
        using AmazonSQSClient providerClient = fixture.CreateSqsClient();
        IAmazonSQS observedClient = DispatchProxy.Create<IAmazonSQS, VisibilityRenewalProbe>();
        var visibilityRenewals = (VisibilityRenewalProbe)(object)observedClient;
        visibilityRenewals.Inner = providerClient;
        IBusControl bus = Bus.Factory.CreateUsingAmazonSqs(configurator =>
        {
            fixture.ConfigureHost(
                configurator,
                host => host.ClientFactories(() => observedClient, fixture.CreateSnsClient));
            configurator.ReceiveEndpoint(queueName, endpoint =>
            {
                endpoint.ConfigureConsumeTopology = false;
                endpoint.QueueAttributes[QueueAttributeName.VisibilityTimeout] = 1;
                endpoint.PrefetchCount = 4;
                endpoint.ConcurrentMessageLimit = 4;
                endpoint.Handler<LockProbe>(async context =>
                {
                    if (context.Message.Id == completedId)
                    {
                        completedReceive.TrySetResult(context.ReceiveContext.ReceiveCompleted);
                        completedEntered.TrySetResult();
                        await heldEntered.Task.WaitAsync(fixture.OperationTimeout, context.CancellationToken);
                        return;
                    }

                    if (context.Message.Id != heldId)
                        throw new InvalidDataException($"Unexpected receive-lock probe '{context.Message.Id}'.");

                    if (Interlocked.Increment(ref heldDeliveryCount) == 1)
                    {
                        heldReceive.TrySetResult(context.ReceiveContext.ReceiveCompleted);
                        heldEntered.TrySetResult();
                    }

                    await releaseHeld.Task.WaitAsync(fixture.OperationTimeout, context.CancellationToken);
                });
            });
        });
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        bool started = false;

        try
        {
            await bus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = true;
            ISendEndpoint input = await bus.GetSendEndpoint(new Uri($"queue:{queueName}"))
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            await input.Send(new LockProbe(completedId), cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            await input.Send(new LockProbe(heldId), cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);

            await completedEntered.Task.WaitAsync(fixture.OperationTimeout, cancellationToken);
            await heldEntered.Task.WaitAsync(fixture.OperationTimeout, cancellationToken);
            Task firstSettlement = await completedReceive.Task.WaitAsync(fixture.OperationTimeout, cancellationToken);
            await firstSettlement.WaitAsync(fixture.OperationTimeout, cancellationToken);

            int completedRenewals = visibilityRenewals.SuccessfulRenewalCount;
            ChangeMessageVisibilityRequest renewal = await visibilityRenewals
                .WaitForSuccessfulRenewalAfter(completedRenewals, fixture.OperationTimeout, cancellationToken);

            Assert.Contains(queueName, renewal.QueueUrl, StringComparison.Ordinal);
            Assert.False(string.IsNullOrWhiteSpace(renewal.ReceiptHandle));
            Assert.Equal(60, renewal.VisibilityTimeout);
            Assert.Equal(1, Volatile.Read(ref heldDeliveryCount));

            releaseHeld.TrySetResult();
            Task heldSettlement = await heldReceive.Task.WaitAsync(fixture.OperationTimeout, cancellationToken);
            await heldSettlement.WaitAsync(fixture.OperationTimeout, cancellationToken);
            Assert.Equal(1, Volatile.Read(ref heldDeliveryCount));
        }
        finally
        {
            releaseHeld.TrySetResult();
            if (started)
                await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
        }
    }

    private static TaskCompletionSource NewObservation() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private static TaskCompletionSource<T> NewObservation<T>() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private class VisibilityRenewalProbe : DispatchProxy
    {
        private readonly Lock _lock = new();
        private readonly List<ChangeMessageVisibilityRequest> _successfulRenewals = [];
        private TaskCompletionSource _renewalRecorded = NewObservation();

        public required IAmazonSQS Inner { get; set; }

        public int SuccessfulRenewalCount
        {
            get
            {
                lock (_lock)
                    return _successfulRenewals.Count;
            }
        }

        public async Task<ChangeMessageVisibilityRequest> WaitForSuccessfulRenewalAfter(
            int observedCount,
            TimeSpan timeout,
            CancellationToken cancellationToken)
        {
            while (true)
            {
                Task recorded;
                lock (_lock)
                {
                    if (_successfulRenewals.Count > observedCount)
                        return _successfulRenewals[observedCount];

                    recorded = _renewalRecorded.Task;
                }

                await recorded.WaitAsync(timeout, cancellationToken);
            }
        }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);

            object? result;
            try
            {
                result = targetMethod.Invoke(Inner, args);
            }
            catch (TargetInvocationException exception) when (exception.InnerException is not null)
            {
                ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
                throw;
            }

            if (targetMethod.Name == nameof(IAmazonSQS.ChangeMessageVisibilityAsync)
                && args is [ChangeMessageVisibilityRequest request, CancellationToken]
                && result is Task<ChangeMessageVisibilityResponse> response)
                return ObserveSuccessfulRenewal(response, request);

            return result;
        }

        private async Task<ChangeMessageVisibilityResponse> ObserveSuccessfulRenewal(
            Task<ChangeMessageVisibilityResponse> response,
            ChangeMessageVisibilityRequest request)
        {
            ChangeMessageVisibilityResponse actual = await response;

            lock (_lock)
            {
                _successfulRenewals.Add(request);
                TaskCompletionSource completed = _renewalRecorded;
                _renewalRecorded = NewObservation();
                completed.TrySetResult();
            }

            return actual;
        }
    }

    private sealed record LockProbe(Guid Id);
}
