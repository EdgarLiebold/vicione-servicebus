using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Courier.Contracts;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Middleware;

public sealed class ScopedCompensationFilterIntegrationTests
{
    private const string ExecuteQueue = "scope-compensation-execute";
    private const string CompensateQueue = "scope-compensation-compensate";
    private const string FailureQueue = "scope-compensation-failure";

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-TENANT-SCOPE", "compensation-filter-shares-one-scope-with-activity-and-isolates-deliveries")]
    public async Task CompensationFilter_ProvidesTheActivityScopeAndIsolatesSuccessiveSlipsAsync(bool closedFilter)
    {
        TimeSpan timeout = TestConfigurationProvider.ForCurrentTestRun()
            .GetValidatedOptions().OperationTimeout!.Value;
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var observation = new ScopeObservation();
        await using ServiceProvider provider = new ServiceCollection()
            .AddScoped<ScopeState>()
            .AddSingleton(observation)
            .AddViciOneServiceBusTestHarness(configuration =>
            {
                configuration.SetTestTimeouts(timeout, timeout);
                configuration.AddActivity<ScopedActivity, ScopeArguments, ScopeLog>()
                    .ExecuteEndpoint(endpoint => endpoint.Name = ExecuteQueue)
                    .CompensateEndpoint(endpoint => endpoint.Name = CompensateQueue);
                configuration.AddExecuteActivity<FailingActivity, FailureArguments>()
                    .Endpoint(endpoint => endpoint.Name = FailureQueue);
                configuration.AddConfigureEndpointsCallback((context, _, endpoint) =>
                {
                    if (closedFilter)
                        endpoint.UseCompensateActivityFilter<ClosedCompensationFilter>(context);
                    else
                        endpoint.UseCompensateActivityFilter(typeof(OpenCompensationFilter<>), context);
                });
                configuration.UsingInMemory((context, bus) => bus.ConfigureEndpoints(context));
            })
            .BuildServiceProvider(validateScopes: true);
        ITestHarness harness = await provider.StartTestHarnessAsync(cancellationToken: cancellationToken)
            .WaitAsync(timeout, cancellationToken);

        try
        {
            Guid first = NewId.NextGuid();
            Guid second = NewId.NextGuid();
            foreach (Guid trackingNumber in new[] { first, second })
            {
                var builder = new RoutingSlipBuilder(trackingNumber);
                builder.AddActivity("Scoped", new Uri($"queue:{ExecuteQueue}"), new ScopeArguments(trackingNumber));
                builder.AddActivity("Failing", new Uri($"queue:{FailureQueue}"), new FailureArguments(trackingNumber));

                await harness.Bus.ExecuteAsync(builder.Build(), cancellationToken);
                IPublishedMessage<IRoutingSlipFaulted> faulted = await harness.Published
                    .SelectAsync<IRoutingSlipFaulted>(
                        message => message.Context.Message.TrackingNumber == trackingNumber,
                        cancellationToken)
                    .FirstObservedAsync(cancellationToken: cancellationToken)
                    .WaitAsync(timeout, cancellationToken);
                Assert.Equal(trackingNumber, faulted.Context.Message.TrackingNumber);
                Assert.Equal("planned-failure", Assert.Single(faulted.Context.Message.ActivityExceptions).ExceptionInfo.Message);
            }

            ScopeSnapshot[] snapshots = observation.Snapshots.ToArray();
            Assert.Equal(6, snapshots.Length);
            ScopeSnapshot firstCompensation = AssertSlip(first, snapshots);
            ScopeSnapshot secondCompensation = AssertSlip(second, snapshots);
            Assert.NotEqual(firstCompensation.ScopeId, secondCompensation.ScopeId);
        }
        finally
        {
            await harness.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }

        Guid[] usedScopes = observation.Snapshots.Select(static snapshot => snapshot.ScopeId).Distinct().Order().ToArray();
        Guid[] disposedScopes = observation.DisposedScopeIds.ToArray();
        Assert.Equal(4, usedScopes.Length);
        Assert.Equal(usedScopes, disposedScopes.Order().ToArray());
    }

    private static ScopeSnapshot AssertSlip(Guid trackingNumber, ScopeSnapshot[] snapshots)
    {
        ScopeSnapshot execution = Assert.Single(snapshots, item =>
            item.TrackingNumber == trackingNumber && item.Stage == "execute");
        ScopeSnapshot filter = Assert.Single(snapshots, item =>
            item.TrackingNumber == trackingNumber && item.Stage == "filter");
        ScopeSnapshot compensation = Assert.Single(snapshots, item =>
            item.TrackingNumber == trackingNumber && item.Stage == "compensate");

        Assert.NotEqual(execution.ScopeId, filter.ScopeId);
        Assert.Equal(filter.ScopeId, compensation.ScopeId);
        Assert.Equal(trackingNumber, filter.Owner);
        Assert.Equal(trackingNumber, compensation.Owner);
        return compensation;
    }

    public sealed record ScopeArguments(Guid TrackingNumber);

    public sealed record FailureArguments(Guid TrackingNumber);

    public sealed record ScopeLog(Guid TrackingNumber);

    public sealed record ScopeSnapshot(string Stage, Guid TrackingNumber, Guid ScopeId, Guid Owner);

    public sealed class ScopeState(ScopeObservation observation) : IDisposable
    {
        public Guid ScopeId { get; } = Guid.NewGuid();

        public Guid Owner { get; set; }

        public void Dispose() => observation.DisposedScopeIds.Enqueue(ScopeId);
    }

    public sealed class ScopeObservation
    {
        public ConcurrentQueue<ScopeSnapshot> Snapshots { get; } = new();

        public ConcurrentQueue<Guid> DisposedScopeIds { get; } = new();
    }

    public sealed class ScopedActivity(ScopeState state, ScopeObservation observation)
        : IActivity<ScopeArguments, ScopeLog>
    {
        public Task<ExecutionResult> ExecuteAsync(ExecuteContext<ScopeArguments> context)
        {
            observation.Snapshots.Enqueue(new ScopeSnapshot(
                "execute", context.TrackingNumber, state.ScopeId, state.Owner));
            return Task.FromResult(context.Completed(new ScopeLog(context.TrackingNumber)));
        }

        public Task<CompensationResult> CompensateAsync(CompensateContext<ScopeLog> context)
        {
            observation.Snapshots.Enqueue(new ScopeSnapshot(
                "compensate", context.TrackingNumber, state.ScopeId, state.Owner));
            return Task.FromResult(context.Compensated());
        }
    }

    public sealed class FailingActivity : IExecuteActivity<FailureArguments>
    {
        public Task<ExecutionResult> ExecuteAsync(ExecuteContext<FailureArguments> context) =>
            throw new PlannedFailureException("planned-failure");
    }

    public class OpenCompensationFilter<T>(ScopeState state, ScopeObservation observation)
        : IFilter<CompensateContext<T>>
        where T : class
    {
        public Task SendAsync(CompensateContext<T> context, IPipe<CompensateContext<T>> next)
        {
            state.Owner = context.TrackingNumber;
            observation.Snapshots.Enqueue(new ScopeSnapshot(
                "filter", context.TrackingNumber, state.ScopeId, state.Owner));
            return next.SendAsync(context);
        }

        public void Probe(ProbeContext context) => context.CreateFilterScope("compensationScope");
    }

    public sealed class ClosedCompensationFilter(ScopeState state, ScopeObservation observation)
        : OpenCompensationFilter<ScopeLog>(state, observation);

    private sealed class PlannedFailureException(string message) : Exception(message);
}
