using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.DependencyInjection;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports;
using Xunit;

namespace ViciOne.ServiceBus.Tests.DependencyInjection;

public sealed class MultiBusRequestTests
{
    private const string HeaderName = "Cross-Bus-Trace";
    private const string HeaderValue = "trace-4e15";

    [Fact]
    [RequirementCoverage("REQ-VSB-MULTIBUS", "same-consumer-names-and-endpoint-callbacks-are-bus-owned")]
    public async Task SameConsumerOnTwoBuses_KeepsNamesAndPerBusCallbacksIsolatedAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var counts = new EndpointConfigurationCounts();
        await using ServiceProvider provider = new ServiceCollection()
            .AddSingleton(counts)
            .AddSingleton<IConfigureReceiveEndpoint>(new CountingGlobalEndpointConfiguration(counts))
            .AddViciOneServiceBusTestHarness(configuration =>
            {
                configuration.SetTestTimeouts(timeout, timeout);
                configuration.AddConsumer<SharedConsumer>()
                    .Endpoint(endpoint => endpoint.Name = "first-bus-queue-name");
                configuration.AddConfigureEndpointsCallback((_, _, _) =>
                    Interlocked.Increment(ref counts.DefaultBus));
            })
            .AddViciOneServiceBus<IBusB>(configuration =>
            {
                configuration.AddConsumer<SharedConsumer>()
                    .Endpoint(endpoint => endpoint.Name = "other-bus-queue-name");
                configuration.AddConfigureEndpointsCallback((_, _, _) =>
                    Interlocked.Increment(ref counts.SecondaryBus));
                configuration.UsingInMemory((context, bus) =>
                {
                    bus.Host(new Uri("loopback://localhost/other-bus"));
                    bus.ConfigureEndpoints(context);
                });
            })
            .BuildServiceProvider(validateScopes: true);
        ITestHarness harness = await provider.StartTestHarnessAsync(cancellationToken: TestContext.Current.CancellationToken).WaitAsync(timeout, cancellationToken);

        try
        {
            var message = new SharedMessage(NewId.NextGuid());
            await harness.Bus.PublishAsync(message, cancellationToken);
            IReceivedMessage<SharedMessage> received = await harness.Consumed
                .SelectAsync<SharedMessage>(cancellationToken)
                .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken).WaitAsync(timeout, cancellationToken);

            Assert.Equal(message, received.Context.Message);
            Assert.Equal("first-bus-queue-name", received.Context.Advanced().ReceiveContext.InputAddress.AbsolutePath.Trim('/'));
            Assert.Equal(1, Volatile.Read(ref counts.DefaultBus));
            Assert.Equal(1, Volatile.Read(ref counts.SecondaryBus));
            Assert.Equal(2, Volatile.Read(ref counts.Global));
        }
        finally
        {
            await harness.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MULTIBUS", "validated-definitions-start-distinct-buses")]
    public async Task ConsumerDefinitions_BuildAndStartTwoDistinctValidatedBusInstancesAsync()
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

        await provider.StartTestHarnessAsync(cancellationToken: TestContext.Current.CancellationToken).WaitAsync(timeout, cancellationToken);
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
            await harness.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MULTIBUS", "cross-bus-request-scope-and-causation")]
    public async Task ScopedConsumer_PreservesCausationWhileRoutingThroughTheSecondaryBusAsync()
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
        ITestHarness harness = await provider.StartTestHarnessAsync(cancellationToken: TestContext.Current.CancellationToken).WaitAsync(timeout, cancellationToken);

        try
        {
            Guid correlationId = Guid.Parse("2b35606c-2d37-4dd5-8229-e936a56e99cd");
            IRequestClient<DefaultRequest> client = harness.GetRequestClient<DefaultRequest>();

            Response<DefaultResponse> response = await client.Advanced().GetResponseAsync<DefaultResponse>(
                new DefaultRequest(correlationId, "Hello"),
                callback: configurator => configurator.UseExecute(context =>
                    context.Headers.Set(HeaderName, HeaderValue)),
                cancellationToken: cancellationToken);
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
            await harness.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MULTIBUS", "default-custom-and-dynamic-buses-own-clients-instances-and-cross-bus-delivery")]
    public async Task ThreeBusRegistration_ResolvesEveryOwnerClientAndCrossBusDeliveryExactlyAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var observation = new ThreeBusObservation();
        await using ServiceProvider provider = new ServiceCollection()
            .AddSingleton(observation)
            .AddViciOneServiceBusTestHarness(configuration =>
            {
                configuration.SetTestTimeouts(timeout, timeout);
                configuration.AddConsumer<DefaultOwnedRequestConsumer>();
                configuration.AddRequestClient<DefaultOwnedRequest>();
            })
            .AddViciOneServiceBus<IBusB, CustomBusB>(configuration =>
            {
                configuration.AddConsumer<BusBOwnedRequestConsumer>();
                configuration.AddConsumer<CrossBusOriginConsumer>();
                configuration.AddRequestClient<BusBOwnedRequest>();
                configuration.UsingInMemory((context, bus) =>
                {
                    bus.Host(new Uri("loopback://localhost/three-bus-b"));
                    bus.ConfigureEndpoints(context);
                });
            })
            .AddViciOneServiceBus<IBusC>(configuration =>
            {
                configuration.AddConsumer<BusCOwnedRequestConsumer>();
                configuration.AddConsumer<CrossBusDeliveredConsumer>();
                configuration.AddRequestClient<BusCOwnedRequest>();
                configuration.UsingInMemory((context, bus) =>
                {
                    bus.Host(new Uri("loopback://localhost/three-bus-c"));
                    bus.ConfigureEndpoints(context);
                });
            })
            .BuildServiceProvider(new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true,
            });
        ITestHarness harness = await provider.StartTestHarnessAsync(cancellationToken: TestContext.Current.CancellationToken).WaitAsync(timeout, cancellationToken);

        try
        {
            IBus defaultBus = provider.GetRequiredService<IBus>();
            IBusB busB = provider.GetRequiredService<IBusB>();
            IBusC busC = provider.GetRequiredService<IBusC>();
            IBusInstance<IBusB> instanceB = provider.GetRequiredService<IBusInstance<IBusB>>();
            IBusInstance<IBusC> instanceC = provider.GetRequiredService<IBusInstance<IBusC>>();
            var defaultRequest = new DefaultOwnedRequest(NewId.NextGuid());
            var requestB = new BusBOwnedRequest(NewId.NextGuid());
            var requestC = new BusCOwnedRequest(NewId.NextGuid());
            var crossBus = new CrossBusOrigin(NewId.NextGuid());
            await using AsyncServiceScope scope = provider.CreateAsyncScope();

            Response<OwnedResponse> defaultResponse = await scope.ServiceProvider
                .GetRequiredService<IRequestClient<DefaultOwnedRequest>>()
                .GetResponseAsync<OwnedResponse>(defaultRequest, cancellationToken)
                .WaitAsync(timeout, cancellationToken);
            Response<OwnedResponse> responseB = await scope.ServiceProvider
                .GetRequiredService<Bind<IBusB, IRequestClient<BusBOwnedRequest>>>()
                .Value
                .GetResponseAsync<OwnedResponse>(requestB, cancellationToken)
                .WaitAsync(timeout, cancellationToken);
            Response<OwnedResponse> responseC = await scope.ServiceProvider
                .GetRequiredService<Bind<IBusC, IRequestClient<BusCOwnedRequest>>>()
                .Value
                .GetResponseAsync<OwnedResponse>(requestC, cancellationToken)
                .WaitAsync(timeout, cancellationToken);
            await busB.PublishAsync(crossBus, cancellationToken);
            CrossBusDelivered delivered = await observation.Delivered.Task.WaitAsync(timeout, cancellationToken);

            Assert.NotSame(defaultBus, busB);
            Assert.NotSame(defaultBus, busC);
            Assert.NotSame(busB, busC);
            Assert.Same(busB, instanceB.Bus);
            Assert.Same(busC, instanceC.Bus);
            Assert.Equal(new OwnedResponse(defaultRequest.CorrelationId, "default"), defaultResponse.Message);
            Assert.Equal(new OwnedResponse(requestB.CorrelationId, "bus-b"), responseB.Message);
            Assert.Equal(new OwnedResponse(requestC.CorrelationId, "bus-c"), responseC.Message);
            Assert.Equal(new CrossBusDelivered(crossBus.CorrelationId, busB.Address, busC.Address), delivered);
        }
        finally
        {
            await harness.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MULTIBUS", "same-request-contract-registrations-are-owner-bound")]
    public void SameRequestContractOnTwoBuses_RegistersOneClientPerOwner()
    {
        var services = new ServiceCollection();

        services
            .AddViciOneServiceBusTestHarness(configuration =>
                configuration.AddRequestClient<SharedOwnedRequest>())
            .AddViciOneServiceBus<IBusB>(configuration =>
            {
                configuration.AddRequestClient<SharedOwnedRequest>();
                configuration.UsingInMemory((_, _) => { });
            });

        Assert.Single(services, descriptor => descriptor.ServiceType == typeof(IRequestClient<SharedOwnedRequest>));
        Assert.Single(services, descriptor =>
            descriptor.ServiceType == typeof(Bind<IBusB, IRequestClient<SharedOwnedRequest>>));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MULTIBUS", "duplicate-request-client-rejected-within-owner")]
    public void DuplicateRequestClientOnOneBus_IsRejectedDuringConfiguration()
    {
        var services = new ServiceCollection();

        ConfigurationException exception = Assert.Throws<ConfigurationException>(() =>
            services.AddViciOneServiceBusTestHarness(configuration =>
            {
                configuration.AddRequestClient<SharedOwnedRequest>();
                configuration.AddRequestClient<SharedOwnedRequest>();
            }));

        Assert.Contains(nameof(SharedOwnedRequest), exception.Message, StringComparison.Ordinal);
        Assert.Contains("already configured for this bus owner", exception.Message, StringComparison.Ordinal);
    }

    private static TimeSpan OperationTimeout() => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions()
        .OperationTimeout!.Value;

    public interface IBusB : IBus
    {
    }

    public interface IBusC : IBus
    {
    }

    public sealed class CustomBusB(IBusControl busControl) : BusInstance<IBusB>(busControl), IBusB;

    public sealed record DefaultRequest(Guid CorrelationId, string Key) : CorrelatedBy<Guid>;

    public sealed record DefaultResponse(Guid CorrelationId, string Value) : CorrelatedBy<Guid>;

    public sealed record SecondaryRequest(Guid CorrelationId, string Key) : CorrelatedBy<Guid>;

    public sealed record SecondaryResponse(Guid CorrelationId, string Value) : CorrelatedBy<Guid>;

    public sealed record DefinitionMessage(string Value);

    public sealed record DefaultOwnedRequest(Guid CorrelationId) : CorrelatedBy<Guid>;

    public sealed record BusBOwnedRequest(Guid CorrelationId) : CorrelatedBy<Guid>;

    public sealed record BusCOwnedRequest(Guid CorrelationId) : CorrelatedBy<Guid>;

    public sealed record OwnedResponse(Guid CorrelationId, string Owner) : CorrelatedBy<Guid>;

    public sealed record CrossBusOrigin(Guid CorrelationId) : CorrelatedBy<Guid>;

    public sealed record CrossBusDelivered(Guid CorrelationId, Uri SourceBus, Uri DestinationBus) : CorrelatedBy<Guid>;

    public sealed record SharedMessage(Guid CorrelationId) : CorrelatedBy<Guid>;

    public sealed record SharedOwnedRequest(Guid CorrelationId) : CorrelatedBy<Guid>;

    public sealed class SharedConsumer : IConsumer<SharedMessage>
    {
        public Task ConsumeAsync(ConsumeContext<SharedMessage> context) => Task.CompletedTask;
    }

    public sealed class EndpointConfigurationCounts
    {
        public int DefaultBus;
        public int SecondaryBus;
        public int Global;
    }

    public sealed class ThreeBusObservation
    {
        public TaskCompletionSource<CrossBusDelivered> Delivered { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    public sealed class DefaultOwnedRequestConsumer : IConsumer<DefaultOwnedRequest>
    {
        public Task ConsumeAsync(ConsumeContext<DefaultOwnedRequest> context) =>
            context.RespondAsync(new OwnedResponse(context.Message.CorrelationId, "default"));
    }

    public sealed class BusBOwnedRequestConsumer : IConsumer<BusBOwnedRequest>
    {
        public Task ConsumeAsync(ConsumeContext<BusBOwnedRequest> context) =>
            context.RespondAsync(new OwnedResponse(context.Message.CorrelationId, "bus-b"));
    }

    public sealed class BusCOwnedRequestConsumer : IConsumer<BusCOwnedRequest>
    {
        public Task ConsumeAsync(ConsumeContext<BusCOwnedRequest> context) =>
            context.RespondAsync(new OwnedResponse(context.Message.CorrelationId, "bus-c"));
    }

    public sealed class CrossBusOriginConsumer(IBusB sourceBus, IBusC destinationBus) : IConsumer<CrossBusOrigin>
    {
        public Task ConsumeAsync(ConsumeContext<CrossBusOrigin> context) => destinationBus.PublishAsync(
            new CrossBusDelivered(context.Message.CorrelationId, sourceBus.Address, destinationBus.Address),
            context.CancellationToken);
    }

    public sealed class CrossBusDeliveredConsumer(ThreeBusObservation observation) : IConsumer<CrossBusDelivered>
    {
        public Task ConsumeAsync(ConsumeContext<CrossBusDelivered> context)
        {
            observation.Delivered.TrySetResult(context.Message);
            return Task.CompletedTask;
        }
    }

    public sealed class CountingGlobalEndpointConfiguration(EndpointConfigurationCounts counts) :
        IConfigureReceiveEndpoint
    {
        public void Configure(string? name, IReceiveEndpointConfigurator configurator) =>
            Interlocked.Increment(ref counts.Global);
    }

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
            Bind<IBusB, IRequestClient<SecondaryRequest>> client,
            Bind<IBus, ISendEndpointProvider> defaultProvider,
            Bind<IBusB, ISendEndpointProvider> secondaryProvider,
            CrossBusObservation observation)
        {
            _client = client.Value;
            _defaultProvider = defaultProvider.Value;
            _secondaryProvider = secondaryProvider.Value;
            _observation = observation;
        }

        public async Task ConsumeAsync(ConsumeContext<DefaultRequest> context)
        {
            _observation.ProvidersWereDistinct = !ReferenceEquals(_defaultProvider, _secondaryProvider);
            _observation.OuterInputAddress = context.Advanced().ReceiveContext.InputAddress;
            Response<SecondaryResponse> response = await _client.GetResponseAsync<SecondaryResponse>(
                new SecondaryRequest(context.Message.CorrelationId, context.Message.Key),
                context.CancellationToken);

            await context.RespondAsync(new DefaultResponse(
                response.Message.CorrelationId,
                response.Message.Value));
        }
    }

    public sealed class SecondaryRequestConsumer(CrossBusObservation observation) : IConsumer<SecondaryRequest>
    {
        public async Task ConsumeAsync(ConsumeContext<SecondaryRequest> context)
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
        public Task ConsumeAsync(ConsumeContext<DefinitionMessage> context) => Task.CompletedTask;
    }

    public sealed class DefaultConsumerDefinition : ConsumerDefinition<DefaultDefinitionConsumer>
    {
    }

    public sealed class SecondaryDefinitionConsumer : IConsumer<DefinitionMessage>
    {
        public Task ConsumeAsync(ConsumeContext<DefinitionMessage> context) => Task.CompletedTask;
    }

    public sealed class SecondaryConsumerDefinition : ConsumerDefinition<SecondaryDefinitionConsumer>
    {
    }
}
