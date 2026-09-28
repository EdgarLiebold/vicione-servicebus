using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.DependencyInjection;

public sealed class MessageScopeHandlerIntegrationTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-CONTAINER-SCOPE", "message-scope-handler-isolated-and-released-after-success-and-failure")]
    public async Task MessageScope_RawHandlerOwnsAndReleasesSeparateScopesAfterSuccessAndFailureAsync()
    {
        TimeSpan timeout = TestConfigurationProvider.ForCurrentTestRun()
            .GetValidatedOptions().OperationTimeout!.Value;
        CancellationToken token = TestContext.Current.CancellationToken;
        var observation = new ScopeObservation();
        await using ServiceProvider provider = new ServiceCollection()
            .AddScoped<ScopeMarker>()
            .AddSingleton(observation)
            .AddViciOneServiceBusTestHarness(configuration =>
            {
                configuration.SetTestTimeouts(timeout, timeout);
                configuration.UsingInMemory((context, bus) =>
                {
                    bus.ReceiveEndpoint("message-scope-handler", endpoint =>
                    {
                        endpoint.UseMessageScope(context);
                        endpoint.Handler<ScopedCommand>(async consume =>
                        {
                            ScopeMarker marker = consume.GetPayload<IServiceScope>()
                                .ServiceProvider.GetRequiredService<ScopeMarker>();
                            ScopeAttempt attempt = observation.Record(consume.Message.CorrelationId, new ScopeVisit(
                                consume.Message.CorrelationId, marker, consume.GetPayload<IServiceScope>(),
                                consume.GetPayload<IServiceProvider>()));
                            await attempt.Release.Task.WaitAsync(consume.CancellationToken);
                            if (consume.Message.Fail)
                                throw new ExpectedScopeFailure();
                        });
                    });
                });
            })
            .BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        ITestHarness harness = await provider.StartTestHarnessAsync(cancellationToken: token).WaitAsync(timeout, token);
        ScopeVisit failed;

        try
        {
            ISendEndpoint endpoint = await harness.Bus.GetSendEndpointAsync(
                new Uri("queue:message-scope-handler"), token).WaitAsync(timeout, token);

            ScopeVisit first = await SendAndObserveAsync(endpoint, observation, fail: false, timeout, token);
            failed = await SendAndObserveAsync(endpoint, observation, fail: true, timeout, token);
            ScopeVisit third = await SendAndObserveAsync(endpoint, observation, fail: false, timeout, token);

            Assert.Same(first.Scope.ServiceProvider, first.Provider);
            Assert.Same(failed.Scope.ServiceProvider, failed.Provider);
            Assert.Same(third.Scope.ServiceProvider, third.Provider);
            Assert.NotSame(provider, first.Provider);
            Assert.NotSame(first.Scope, failed.Scope);
            Assert.NotSame(failed.Scope, third.Scope);
            Assert.NotSame(first.Marker, failed.Marker);
            Assert.NotSame(failed.Marker, third.Marker);
            Assert.Equal(1, first.Marker.DisposeCount);
            Assert.Equal(1, failed.Marker.DisposeCount);
            Assert.Equal(1, third.Marker.DisposeCount);
        }
        finally
        {
            await harness.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }

        IPublishedMessage<Fault<ScopedCommand>> fault = Assert.Single(
            harness.Published.Snapshot<Fault<ScopedCommand>>());
        Assert.Equal(failed.CorrelationId, fault.Context.Message.Message.CorrelationId);
        ExceptionInfo exception = Assert.Single(fault.Context.Message.Exceptions);
        Assert.Equal(TypeCache<ExpectedScopeFailure>.ShortName, exception.ExceptionType);
    }

    private static async Task<ScopeVisit> SendAndObserveAsync(
        ISendEndpoint endpoint, ScopeObservation observation, bool fail,
        TimeSpan timeout, CancellationToken token)
    {
        Guid id = NewId.NextGuid();
        ScopeAttempt attempt = observation.Expect(id);
        ScopeVisit visit;
        try
        {
            await endpoint.SendAsync(new ScopedCommand(id, fail), token);
            visit = await attempt.Entered.Task.WaitAsync(timeout, token);
            Assert.False(visit.Marker.Disposed.IsCompleted);
            Assert.Equal(0, visit.Marker.DisposeCount);
        }
        finally
        {
            attempt.Release.TrySetResult();
        }
        await visit.Marker.Disposed.WaitAsync(timeout, token);
        return visit;
    }

    public sealed record ScopedCommand(Guid CorrelationId, bool Fail) : ICorrelatedBy<Guid>;

    public sealed record ScopeVisit(Guid CorrelationId, ScopeMarker Marker, IServiceScope Scope, IServiceProvider Provider);

    public sealed class ScopeObservation
    {
        readonly ConcurrentDictionary<Guid, ScopeAttempt> _visits = new();

        public ScopeAttempt Expect(Guid id)
        {
            var attempt = new ScopeAttempt();
            Assert.True(_visits.TryAdd(id, attempt));
            return attempt;
        }

        public ScopeAttempt Record(Guid id, ScopeVisit visit)
        {
            ScopeAttempt attempt = _visits[id];
            attempt.Entered.TrySetResult(visit);
            return attempt;
        }
    }

    public sealed class ScopeAttempt
    {
        public TaskCompletionSource<ScopeVisit> Entered { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    public sealed class ScopeMarker : IAsyncDisposable
    {
        int _disposeCount;

        public Task Disposed => _disposed.Task;
        public int DisposeCount => Volatile.Read(ref _disposeCount);

        readonly TaskCompletionSource _disposed = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public ValueTask DisposeAsync()
        {
            Interlocked.Increment(ref _disposeCount);
            _disposed.TrySetResult();
            return default;
        }
    }

    public sealed class ExpectedScopeFailure : Exception;
}
