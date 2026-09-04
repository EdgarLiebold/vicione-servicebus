using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ViciOne.ServiceBus.DependencyInjection;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.DependencyInjection;

public sealed class MultiBusScopeIsolationTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-MULTIBUS-SCOPE", "bound-scope-providers-and-setters-are-distinct")]
    public async Task EachBusOwnsDistinctBoundScopeProviderAndSetterInstancesAsync()
    {
        await using ServiceProvider provider = Build(new Reports());
        await using AsyncServiceScope scope = provider.CreateAsyncScope();

        IScopedConsumeContextProvider untyped =
            scope.ServiceProvider.GetRequiredService<IScopedConsumeContextProvider>();
        IScopedConsumeContextProvider alphaProvider = scope.ServiceProvider
            .GetRequiredService<Bind<IBusAlpha, IScopedConsumeContextProvider>>().Value;
        IScopedConsumeContextProvider betaProvider = scope.ServiceProvider
            .GetRequiredService<Bind<IBusBeta, IScopedConsumeContextProvider>>().Value;
        ISetScopedConsumeContext alphaSetter = provider
            .GetRequiredService<Bind<IBusAlpha, ISetScopedConsumeContext>>().Value;
        ISetScopedConsumeContext betaSetter = provider
            .GetRequiredService<Bind<IBusBeta, ISetScopedConsumeContext>>().Value;

        Assert.NotSame(alphaProvider, betaProvider);
        Assert.NotSame(alphaProvider, untyped);
        Assert.NotSame(betaProvider, untyped);
        Assert.NotSame(alphaSetter, betaSetter);
    }

    [Theory]
    [InlineData(BusDirection.Alpha)]
    [InlineData(BusDirection.Beta)]
    [RequirementCoverage("REQ-VSB-MULTIBUS-SCOPE", "consume-context-never-leaks-between-bound-buses")]
    public async Task ActiveConsumeContext_IsVisibleOnlyThroughItsOwningBusAsync(BusDirection direction)
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var reports = new Reports();
        await using ServiceProvider provider = Build(reports);
        IBusAlpha alpha = provider.GetRequiredService<IBusAlpha>();
        IBusBeta beta = provider.GetRequiredService<IBusBeta>();

        await ((IBusControl)alpha).StartAsync(cancellationToken).WaitAsync(timeout, cancellationToken);
        await ((IBusControl)beta).StartAsync(cancellationToken).WaitAsync(timeout, cancellationToken);
        try
        {
            Observation observation;
            if (direction == BusDirection.Alpha)
            {
                await alpha.PublishAsync(new AlphaMessage("alpha-marker"), cancellationToken);
                observation = await reports.Alpha.Task.WaitAsync(timeout, cancellationToken);
                Assert.Equal("alpha-marker", observation.Own);
            }
            else
            {
                await beta.PublishAsync(new BetaMessage("beta-marker"), cancellationToken);
                observation = await reports.Beta.Task.WaitAsync(timeout, cancellationToken);
                Assert.Equal("beta-marker", observation.Own);
            }

            Assert.Equal("nothing", observation.Other);
        }
        finally
        {
            await ((IBusControl)beta).StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
            await ((IBusControl)alpha).StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    private static ServiceProvider Build(Reports reports) => new ServiceCollection()
        .AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance)
        .AddSingleton(typeof(ILogger<>), typeof(NullLogger<>))
        .AddSingleton(reports)
        .AddViciOneServiceBus<IBusAlpha>(configuration =>
        {
            configuration.AddConsumer<AlphaConsumer>();
            configuration.UsingInMemory((context, bus) =>
            {
                bus.Host(new Uri("loopback://alpha/"));
                bus.ConfigureEndpoints(context);
            });
        })
        .AddViciOneServiceBus<IBusBeta>(configuration =>
        {
            configuration.AddConsumer<BetaConsumer>();
            configuration.UsingInMemory((context, bus) =>
            {
                bus.Host(new Uri("loopback://beta/"));
                bus.ConfigureEndpoints(context);
            });
        })
        .BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true,
        });

    private static string Read<T>(IScopedConsumeContextProvider provider)
        where T : class, IMarked
    {
        ConsumeContext? context = provider.GetContext();
        if (context is null)
            return "nothing";

        return context.TryGetMessage<T>(out ConsumeContext<T>? message)
            ? message.Message.Marker
            : $"other:{context.GetType().Name}";
    }

    private static TimeSpan OperationTimeout() => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions()
        .OperationTimeout!.Value;

    public enum BusDirection
    {
        Alpha,
        Beta,
    }

    public interface IBusAlpha : IBus;

    public interface IBusBeta : IBus;

    public interface IMarked
    {
        string Marker { get; }
    }

    public sealed record AlphaMessage(string Marker) : IMarked;

    public sealed record BetaMessage(string Marker) : IMarked;

    public sealed record Observation(string Own, string Other);

    public sealed class Reports
    {
        public TaskCompletionSource<Observation> Alpha { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource<Observation> Beta { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    public sealed class AlphaConsumer(
        Bind<IBusAlpha, IScopedConsumeContextProvider> own,
        Bind<IBusBeta, IScopedConsumeContextProvider> other,
        Reports reports) : IConsumer<AlphaMessage>
    {
        public Task ConsumeAsync(ConsumeContext<AlphaMessage> context)
        {
            reports.Alpha.TrySetResult(new Observation(
                Read<AlphaMessage>(own.Value),
                Read<AlphaMessage>(other.Value)));
            return Task.CompletedTask;
        }
    }

    public sealed class BetaConsumer(
        Bind<IBusBeta, IScopedConsumeContextProvider> own,
        Bind<IBusAlpha, IScopedConsumeContextProvider> other,
        Reports reports) : IConsumer<BetaMessage>
    {
        public Task ConsumeAsync(ConsumeContext<BetaMessage> context)
        {
            reports.Beta.TrySetResult(new Observation(
                Read<BetaMessage>(own.Value),
                Read<BetaMessage>(other.Value)));
            return Task.CompletedTask;
        }
    }
}
