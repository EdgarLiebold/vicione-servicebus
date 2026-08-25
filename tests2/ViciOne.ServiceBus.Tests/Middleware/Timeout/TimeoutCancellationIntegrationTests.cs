using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Middleware.Timeout;

public sealed class TimeoutCancellationIntegrationTests
{
    private static readonly DateTimeOffset StartTime =
        new(2031, 2, 3, 4, 5, 6, TimeSpan.Zero);

    [Fact]
    [RequirementCoverage("REQ-VSB-TIMEOUT-PIPELINE", "fault-and-short-circuit")]
    public async Task PipelineTimeout_PublishesOneFaultAndDoesNotContinueTheHandler()
    {
        TimeSpan operationTimeout = OperationTimeout();
        TimeSpan messageTimeout = TimeSpan.FromMinutes(2);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var timeProvider = new FakeTimeProvider(StartTime);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var continued = false;
        await using ServiceProvider provider = new ServiceCollection()
            .AddViciOneServiceBusTestHarness(configuration =>
            {
                configuration.SetTestTimeouts(operationTimeout, operationTimeout);
                configuration.AddOptions<ViciOneServiceBusHostOptions>()
                    .Configure(options =>
                    {
                        options.ConsumerStopTimeout = TimeSpan.FromMilliseconds(100);
                        options.StopTimeout = TimeSpan.FromSeconds(5);
                    });
                configuration.AddHandler<TimeoutMessage>(async context =>
                {
                    entered.TrySetResult();
                    await Task.Delay(System.Threading.Timeout.InfiniteTimeSpan, context.CancellationToken);
                    continued = true;
                });
                configuration.UsingInMemory((context, endpoint) =>
                {
                    endpoint.UseTimeout(timeout =>
                    {
                        timeout.Timeout = messageTimeout;
                        timeout.TimeProvider = timeProvider;
                    });
                    endpoint.ConfigureEndpoints(context);
                });
            })
            .BuildServiceProvider(new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true,
            });
        ITestHarness harness = await provider.StartTestHarness().WaitAsync(operationTimeout, cancellationToken);

        try
        {
            Task<IPublishedMessage<Fault<TimeoutMessage>>> faultTask = harness.Published
                .SelectAsync<Fault<TimeoutMessage>>(cancellationToken)
                .First();

            await harness.Bus.Publish(new TimeoutMessage("virtual-time"), cancellationToken);
            await entered.Task.WaitAsync(operationTimeout, cancellationToken);
            timeProvider.Advance(messageTimeout - TimeSpan.FromTicks(1));
            Assert.False(faultTask.IsCompleted);

            timeProvider.Advance(TimeSpan.FromTicks(1));
            Fault<TimeoutMessage> fault = (await faultTask.WaitAsync(operationTimeout, cancellationToken)).Context.Message;

            ExceptionInfo exception = Assert.Single(fault.Exceptions);
            Assert.Equal(TypeCache<ConsumerCanceledException>.ShortName, exception.ExceptionType);
            Assert.Contains(messageTimeout.ToString(), exception.Message, StringComparison.Ordinal);
            Assert.Equal(TypeCache<TaskCanceledException>.ShortName, exception.InnerException!.ExceptionType);
            Assert.False(continued);
            Assert.Single(harness.Published.Select<Fault<TimeoutMessage>>(SnapshotOnlyToken()));
        }
        finally
        {
            await harness.Stop(CancellationToken.None).WaitAsync(operationTimeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TIMEOUT-CONFIGURATION", "compiled-snapshot")]
    public async Task BuiltPipeline_IsUnaffectedByLaterConfiguratorMutation()
    {
        TimeSpan operationTimeout = OperationTimeout();
        TimeSpan configuredTimeout = TimeSpan.FromMinutes(2);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var configuredProvider = new FakeTimeProvider(StartTime);
        var laterProvider = new FakeTimeProvider(StartTime);
        ITimeoutConfigurator? retainedConfigurator = null;
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await using ServiceProvider provider = new ServiceCollection()
            .AddViciOneServiceBusTestHarness(configuration =>
            {
                configuration.SetTestTimeouts(operationTimeout, operationTimeout);
                configuration.AddHandler<SnapshotMessage>(async context =>
                {
                    entered.TrySetResult();
                    await Task.Delay(System.Threading.Timeout.InfiniteTimeSpan, context.CancellationToken);
                });
                configuration.UsingInMemory((context, endpoint) =>
                {
                    endpoint.UseTimeout(timeout =>
                    {
                        retainedConfigurator = timeout;
                        timeout.Timeout = configuredTimeout;
                        timeout.TimeProvider = configuredProvider;
                    });
                    endpoint.ConfigureEndpoints(context);
                });
            })
            .BuildServiceProvider(new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true,
            });
        ITestHarness harness = await provider.StartTestHarness().WaitAsync(operationTimeout, cancellationToken);

        try
        {
            Assert.NotNull(retainedConfigurator);
            retainedConfigurator.Timeout = TimeSpan.FromHours(1);
            retainedConfigurator.TimeProvider = laterProvider;
            Task<IPublishedMessage<Fault<SnapshotMessage>>> faultTask = harness.Published
                .SelectAsync<Fault<SnapshotMessage>>(cancellationToken)
                .First();

            await harness.Bus.Publish(new SnapshotMessage("snapshot"), cancellationToken);
            await entered.Task.WaitAsync(operationTimeout, cancellationToken);
            laterProvider.Advance(TimeSpan.FromDays(1));
            Assert.False(faultTask.IsCompleted);

            configuredProvider.Advance(configuredTimeout);
            Fault<SnapshotMessage> fault = (await faultTask.WaitAsync(operationTimeout, cancellationToken)).Context.Message;

            ExceptionInfo exception = Assert.Single(fault.Exceptions);
            Assert.Equal(TypeCache<ConsumerCanceledException>.ShortName, exception.ExceptionType);
            Assert.Contains(configuredTimeout.ToString(), exception.Message, StringComparison.Ordinal);
        }
        finally
        {
            await harness.Stop(CancellationToken.None).WaitAsync(operationTimeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONSUME-CANCELLATION", "transport-stop-is-not-a-fault")]
    public async Task TransportStop_CancelsTheHandlerWithoutPublishingAFault()
    {
        TimeSpan operationTimeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var timeProvider = new FakeTimeProvider(StartTime);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await using ServiceProvider provider = new ServiceCollection()
            .AddViciOneServiceBusTestHarness(configuration =>
            {
                configuration.SetTestTimeouts(operationTimeout, operationTimeout);
                configuration.AddOptions<ViciOneServiceBusHostOptions>()
                    .Configure(options =>
                    {
                        options.ConsumerStopTimeout = TimeSpan.FromMilliseconds(100);
                        options.StopTimeout = TimeSpan.FromSeconds(5);
                    });
                configuration.AddHandler<ShutdownMessage>(async context =>
                {
                    entered.TrySetResult();
                    await Task.Delay(System.Threading.Timeout.InfiniteTimeSpan, context.CancellationToken);
                });
                configuration.UsingInMemory((context, endpoint) =>
                {
                    endpoint.UseTimeout(timeout =>
                    {
                        timeout.Timeout = TimeSpan.FromDays(1);
                        timeout.TimeProvider = timeProvider;
                    });
                    endpoint.ConfigureEndpoints(context);
                });
            })
            .BuildServiceProvider(new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true,
            });
        ITestHarness harness = await provider.StartTestHarness().WaitAsync(operationTimeout, cancellationToken);
        var stopped = false;

        try
        {
            await harness.Bus.Publish(new ShutdownMessage("stop"), cancellationToken);
            await entered.Task.WaitAsync(operationTimeout, cancellationToken);

            await harness.Stop(CancellationToken.None).WaitAsync(operationTimeout, CancellationToken.None);
            stopped = true;

            Assert.Empty(harness.Published.Select<Fault<ShutdownMessage>>(SnapshotOnlyToken()));
        }
        finally
        {
            if (!stopped)
                await harness.Stop(CancellationToken.None).WaitAsync(operationTimeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONSUME-CANCELLATION", "independent-cancellation-is-a-fault")]
    public async Task IndependentHandlerCancellation_PublishesTheExactFault()
    {
        TimeSpan operationTimeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var independent = new CancellationTokenSource();
        independent.Cancel();
        await using ServiceProvider provider = new ServiceCollection()
            .AddViciOneServiceBusTestHarness(configuration =>
            {
                configuration.SetTestTimeouts(operationTimeout, operationTimeout);
                configuration.AddHandler<IndependentCancellationMessage>(
                    (ConsumeContext<IndependentCancellationMessage> _) => Task.FromCanceled(independent.Token));
            })
            .BuildServiceProvider(new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true,
            });
        ITestHarness harness = await provider.StartTestHarness().WaitAsync(operationTimeout, cancellationToken);

        try
        {
            Task<IPublishedMessage<Fault<IndependentCancellationMessage>>> faultTask = harness.Published
                .SelectAsync<Fault<IndependentCancellationMessage>>(cancellationToken)
                .First();

            await harness.Bus.Publish(new IndependentCancellationMessage("independent"), cancellationToken);
            Fault<IndependentCancellationMessage> fault =
                (await faultTask.WaitAsync(operationTimeout, cancellationToken)).Context.Message;

            ExceptionInfo exception = Assert.Single(fault.Exceptions);
            Assert.Equal(TypeCache<TaskCanceledException>.ShortName, exception.ExceptionType);
            Assert.Equal("A task was canceled.", exception.Message);
            Assert.Single(harness.Published.Select<Fault<IndependentCancellationMessage>>(SnapshotOnlyToken()));
        }
        finally
        {
            await harness.Stop(CancellationToken.None).WaitAsync(operationTimeout, CancellationToken.None);
        }
    }

    private static CancellationToken SnapshotOnlyToken() => new(canceled: true);

    private static TimeSpan OperationTimeout() => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions()
        .OperationTimeout!.Value;

    public sealed record TimeoutMessage(string Value);

    public sealed record ShutdownMessage(string Value);

    public sealed record IndependentCancellationMessage(string Value);

    public sealed record SnapshotMessage(string Value);
}
