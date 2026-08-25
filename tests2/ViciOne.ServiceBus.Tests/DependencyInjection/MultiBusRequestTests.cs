using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.DependencyInjection;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.DependencyInjection;

public sealed class MultiBusRequestTests
{
    private const string HeaderName = "Cross-Bus-Trace";
    private const string HeaderValue = "trace-4e15";

    [Fact]
    [RequirementCoverage("REQ-VSB-MULTIBUS", "validated-definitions-start-distinct-buses")]
    public async Task ConsumerDefinitions_BuildAndStartTwoDistinctValidatedBusInstances()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using ServiceProvider provider = new ServiceCollection()
            .AddViciOneServiceBusTestHarness(configuration =>
                configuration.AddConsumer<DefaultDefinitionConsumer, DefaultConsumerDefinition>())
            .AddViciOneServiceBus<IBusB>(configuration =>
            {
                configuration.AddConsumer<SecondaryDefinitionConsumer, SecondaryConsumerDefinition>();
                configuration.UsingInMemory((context, configurator) =>
                {
                    configurator.Host(new Uri("loopback://localhost/b"));
                    configurator.ConfigureEndpoints(context);
                });
            })
            .BuildServiceProvider(new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true,
            });
        ITestHarness harness = provider.GetTestHarness();
        IBus defaultBus = provider.GetRequiredService<IBus>();
        IBusB secondaryBus = provider.GetRequiredService<IBusB>();

        await provider.StartTestHarness().WaitAsync(timeout, cancellationToken);
        try
        {
            Assert.NotSame(defaultBus, secondaryBus);
            Assert.Equal("loopback", defaultBus.Address.Scheme);
            Assert.Equal("localhost", defaultBus.Address.Host);
            Assert.False(defaultBus.Address.AbsolutePath.StartsWith("/b/", StringComparison.Ordinal));
            Assert.Equal("loopback", secondaryBus.Address.Scheme);
            Assert.Equal("localhost", secondaryBus.Address.Host);
            Assert.StartsWith("/b/", secondaryBus.Address.AbsolutePath, StringComparison.Ordinal);
        }
        finally
        {
            await harness.Stop(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MULTIBUS", "cross-bus-request-scope-and-causation")]
    public async Task ScopedConsumer_PreservesCausationWhileRoutingThroughTheSecondaryBus()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var observation = new CrossBusObservation();
        await using ServiceProvider provider = new ServiceCollection()
            .AddSingleton(observation)
            .AddViciOneServiceBusTestHarness(configuration =>
            {
                configuration.SetTestTimeouts(timeout, timeout);
                configuration.AddRequestClient<DefaultRequest>();
                configuration.AddConsumer<DefaultRequestConsumer>();
            })
            .AddViciOneServiceBus<IBusB>(configuration =>
            {
                configuration.AddRequestClient<SecondaryRequest>(RequestTimeout.After(s: 1));
                configuration.AddConsumer<SecondaryRequestConsumer>();
                configuration.UsingInMemory((context, configurator) =>
                {
                    configurator.Host(new Uri("loopback://localhost/b"));
                    configurator.ConfigureEndpoints(context);
                });
            })
            .BuildServiceProvider(new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true,
            });
        ITestHarness harness = await provider.StartTestHarness().WaitAsync(timeout, cancellationToken);

        try
        {
            Guid correlationId = Guid.Parse("2b35606c-2d37-4dd5-8229-e936a56e99cd");
            IRequestClient<DefaultRequest> client = harness.GetRequestClient<DefaultRequest>();

            Response<DefaultResponse> response = await client.GetResponse<DefaultResponse>(
                new DefaultRequest(correlationId, "Hello"),
                configurator => configurator.UseExecute(context =>
                    context.Headers.Set(HeaderName, HeaderValue)),
                cancellationToken);
            CrossBusRequestObservation inner =
                await observation.InnerRequest.Task.WaitAsync(timeout, cancellationToken);

            Assert.Equal(new DefaultResponse(correlationId, "Key: Hello"), response.Message);
            Assert.Equal(HeaderValue, response.Headers.Get<string>(HeaderName));
            Assert.True(observation.ProvidersWereDistinct);
            Assert.Equal(correlationId, inner.CorrelationId);
            Assert.Equal(HeaderValue, inner.TraceHeader);
            Assert.Equal(observation.OuterInputAddress, inner.SourceAddress);
            Uri destinationAddress = Assert.IsType<Uri>(inner.DestinationAddress);
            Assert.Equal("loopback", destinationAddress.Scheme);
            Assert.Equal("localhost", destinationAddress.Host);
            Assert.StartsWith("/b/", destinationAddress.AbsolutePath, StringComparison.Ordinal);
            Assert.NotNull(inner.RequestId);
            Assert.NotNull(inner.ConversationId);
        }
        finally
        {
            await harness.Stop(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    private static TimeSpan OperationTimeout() => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions()
        .OperationTimeout!.Value;

    public interface IBusB : IBus
    {
    }

    public sealed record DefaultRequest(Guid CorrelationId, string Key) : CorrelatedBy<Guid>;

    public sealed record DefaultResponse(Guid CorrelationId, string Value) : CorrelatedBy<Guid>;

    public sealed record SecondaryRequest(Guid CorrelationId, string Key) : CorrelatedBy<Guid>;

    public sealed record SecondaryResponse(Guid CorrelationId, string Value) : CorrelatedBy<Guid>;

    public sealed record DefinitionMessage(string Value);

    public sealed record CrossBusRequestObservation(
        Guid CorrelationId,
        string? TraceHeader,
        Uri? SourceAddress,
        Uri? DestinationAddress,
        Guid? RequestId,
        Guid? ConversationId);

    public sealed class CrossBusObservation
    {
        public TaskCompletionSource<CrossBusRequestObservation> InnerRequest { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public bool ProvidersWereDistinct { get; set; }

        public Uri? OuterInputAddress { get; set; }
    }

    public sealed class DefaultRequestConsumer : IConsumer<DefaultRequest>
    {
        private readonly IRequestClient<SecondaryRequest> _client;
        private readonly ISendEndpointProvider _defaultProvider;
        private readonly ISendEndpointProvider _secondaryProvider;
        private readonly CrossBusObservation _observation;

        public DefaultRequestConsumer(
            IRequestClient<SecondaryRequest> client,
            Bind<IBus, ISendEndpointProvider> defaultProvider,
            Bind<IBusB, ISendEndpointProvider> secondaryProvider,
            CrossBusObservation observation)
        {
            _client = client;
            _defaultProvider = defaultProvider.Value;
            _secondaryProvider = secondaryProvider.Value;
            _observation = observation;
        }

        public async Task Consume(ConsumeContext<DefaultRequest> context)
        {
            _observation.ProvidersWereDistinct = !ReferenceEquals(_defaultProvider, _secondaryProvider);
            _observation.OuterInputAddress = context.ReceiveContext.InputAddress;
            Response<SecondaryResponse> response = await _client.GetResponse<SecondaryResponse>(
                new SecondaryRequest(context.Message.CorrelationId, context.Message.Key),
                context.CancellationToken);

            await context.RespondAsync(new DefaultResponse(
                response.Message.CorrelationId,
                response.Message.Value));
        }
    }

    public sealed class SecondaryRequestConsumer(CrossBusObservation observation) : IConsumer<SecondaryRequest>
    {
        public async Task Consume(ConsumeContext<SecondaryRequest> context)
        {
            observation.InnerRequest.TrySetResult(new CrossBusRequestObservation(
                context.Message.CorrelationId,
                context.Headers.Get<string>(HeaderName),
                context.SourceAddress,
                context.DestinationAddress,
                context.RequestId,
                context.ConversationId));

            await context.RespondAsync(new SecondaryResponse(
                context.Message.CorrelationId,
                $"Key: {context.Message.Key}"));
        }
    }

    public sealed class DefaultDefinitionConsumer : IConsumer<DefinitionMessage>
    {
        public Task Consume(ConsumeContext<DefinitionMessage> context) => Task.CompletedTask;
    }

    public sealed class DefaultConsumerDefinition : ConsumerDefinition<DefaultDefinitionConsumer>
    {
    }

    public sealed class SecondaryDefinitionConsumer : IConsumer<DefinitionMessage>
    {
        public Task Consume(ConsumeContext<DefinitionMessage> context) => Task.CompletedTask;
    }

    public sealed class SecondaryConsumerDefinition : ConsumerDefinition<SecondaryDefinitionConsumer>
    {
    }
}
