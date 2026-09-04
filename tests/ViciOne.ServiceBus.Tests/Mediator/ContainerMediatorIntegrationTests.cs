using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Mediator;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Mediator;

public sealed class ContainerMediatorIntegrationTests
{
    [Theory]
    [InlineData(MediatorBusRoute.Publish, false)]
    [InlineData(MediatorBusRoute.Send, false)]
    [InlineData(MediatorBusRoute.Publish, true)]
    [InlineData(MediatorBusRoute.Send, true)]
    [RequirementCoverage("REQ-VSB-CONTAINER-MEDIATOR", "bus-source-address-for-publish-send-and-custom-base")]
    public async Task MediatorBusBridge_UsesTheExactDefaultOrConfiguredMediatorSourceAsync(
        MediatorBusRoute route,
        bool customBaseAddress)
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        Uri? baseAddress = customBaseAddress
            ? new Uri("loopback://localhost/container-mediator-base")
            : null;
        var services = new ServiceCollection();
        services.AddViciOneServiceBusTestHarness(configuration =>
        {
            configuration.SetTestTimeouts(timeout, timeout);
            configuration.AddConsumer<MediatorBusIngressConsumer>()
                .Endpoint(endpoint => endpoint.Name = "container-mediator-ingress");
        });
        if (baseAddress is null)
            services.AddMediator(configuration => AddMediatorRoute(configuration, route));
        else
            services.AddMediator(baseAddress, configuration => AddMediatorRoute(configuration, route));

        await using ServiceProvider provider = services.BuildServiceProvider(validateScopes: true);
        ITestHarness harness = await provider.StartTestHarnessAsync(cancellationToken: TestContext.Current.CancellationToken).WaitAsync(timeout, cancellationToken);

        try
        {
            await using AsyncServiceScope scope = provider.CreateAsyncScope();
            IScopedMediator mediator = scope.ServiceProvider.GetRequiredService<IScopedMediator>();
            var command = new MediatorBridgeCommand(NewId.NextGuid(), route);

            await mediator.SendAsync(command, cancellationToken).WaitAsync(timeout, cancellationToken);
            IReceivedMessage<MediatorBridgeMessage> received = await harness.Consumed
                .SelectAsync<MediatorBridgeMessage>(cancellationToken)
                .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken).WaitAsync(timeout, cancellationToken);

            Assert.Equal(command.CorrelationId, received.Context.Message.CorrelationId);
            Assert.Equal(route, received.Context.Message.Route);
            Assert.Equal(
                baseAddress is null
                    ? new Uri("loopback://localhost/mediator")
                    : new Uri($"{baseAddress.AbsoluteUri.TrimEnd('/')}/mediator"),
                received.Context.SourceAddress);
            if (route == MediatorBusRoute.Publish)
                Assert.True(await harness.Published.AnyAsync<MediatorBridgeMessage>(cancellationToken));
            else
                Assert.True(await harness.Sent.AnyAsync<MediatorBridgeMessage>(cancellationToken));
        }
        finally
        {
            await harness.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONTAINER-MEDIATOR", "bus-publication-does-not-inherit-mediator-headers")]
    public async Task MediatorConsumerPublishingOnBus_DoesNotInventAnInitiatorAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using ServiceProvider provider = new ServiceCollection()
            .AddViciOneServiceBusTestHarness(configuration => configuration.SetTestTimeouts(timeout, timeout))
            .AddMediator(configuration => configuration.AddConsumer<BusPublishingMediatorConsumer>())
            .BuildServiceProvider(validateScopes: true);
        ITestHarness harness = await provider.StartTestHarnessAsync(cancellationToken: TestContext.Current.CancellationToken).WaitAsync(timeout, cancellationToken);

        try
        {
            await using AsyncServiceScope scope = provider.CreateAsyncScope();
            IScopedMediator mediator = scope.ServiceProvider.GetRequiredService<IScopedMediator>();
            var command = new MediatorBusCommand(NewId.NextGuid(), "expected");

            await mediator.SendAsync(command, cancellationToken).WaitAsync(timeout, cancellationToken);
            IPublishedMessage<MediatorBusEvent> published = await harness.Published
                .SelectAsync<MediatorBusEvent>(cancellationToken)
                .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken).WaitAsync(timeout, cancellationToken);

            Assert.Equal(new MediatorBusEvent(command.CorrelationId, command.Value), published.Context.Message);
            Assert.Null(published.Context.InitiatorId);
        }
        finally
        {
            await harness.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONTAINER-MEDIATOR", "nested-request-metadata-and-response-filter")]
    public async Task NestedMediatorRequest_PreservesCausationAndRunsTheResponseFilterAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using ServiceProvider provider = new ServiceCollection()
            .AddMediator(configuration =>
            {
                configuration.AddConsumer<OuterMediatorRequestConsumer>();
                configuration.AddConsumer<InnerMediatorRequestConsumer>();
                configuration.AddRequestClient<OuterMediatorRequest>();
                configuration.AddRequestClient<InnerMediatorRequest>();
                configuration.ConfigureMediator((context, mediator) =>
                    mediator.UseSendFilter(typeof(UppercaseMediatorResponseFilter<>), context));
            })
            .BuildServiceProvider(validateScopes: true);
        await using AsyncServiceScope scope = provider.CreateAsyncScope();
        IRequestClient<OuterMediatorRequest> client = scope.ServiceProvider
            .GetRequiredService<IRequestClient<OuterMediatorRequest>>();
        Guid correlationId = NewId.NextGuid();

        Response<OuterMediatorResponse> response = await client.GetResponseAsync<OuterMediatorResponse>(
            new OuterMediatorRequest(correlationId, "world"),
            cancellationToken).WaitAsync(timeout, cancellationToken);

        Assert.Equal("HELLO, WORLD", response.Message.Value);
        Assert.Equal(correlationId, response.Message.OriginalInitiatorId);
        Assert.Equal(response.ConversationId, response.Message.OriginalConversationId);
        Assert.Equal(correlationId, response.InitiatorId);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONTAINER-MEDIATOR", "published-message-initiates-container-saga")]
    public async Task MediatorConsumerPublication_InitiatesTheRegisteredSagaInstanceAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using ServiceProvider provider = new ServiceCollection()
            .AddMediator(configuration =>
            {
                configuration.AddConsumer<SagaStartingMediatorConsumer>();
                configuration.AddSaga<MediatorOrderSaga>().InMemoryRepository();
            })
            .BuildServiceProvider(validateScopes: true);
        await using AsyncServiceScope scope = provider.CreateAsyncScope();
        IScopedMediator mediator = scope.ServiceProvider.GetRequiredService<IScopedMediator>();
        ILoadSagaRepository<MediatorOrderSaga> repository = provider.GetRequiredService<ILoadSagaRepository<MediatorOrderSaga>>();
        Guid correlationId = NewId.NextGuid();

        await mediator.SendAsync(new StartMediatorOrder(correlationId, "90210"), cancellationToken)
            .WaitAsync(timeout, cancellationToken);
        Guid? found = await repository.ShouldContainSagaAsync(correlationId, timeout, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(correlationId, found);
    }

    private static TimeSpan OperationTimeout() => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions().OperationTimeout!.Value;

    private static void AddMediatorRoute(IMediatorRegistrationConfigurator configuration, MediatorBusRoute route)
    {
        if (route == MediatorBusRoute.Publish)
            configuration.AddConsumer<MediatorPublishingBridgeConsumer>();
        else
            configuration.AddConsumer<MediatorSendingBridgeConsumer>();
    }

    public enum MediatorBusRoute
    {
        Publish,
        Send,
    }

    public sealed record MediatorBridgeCommand(Guid CorrelationId, MediatorBusRoute Route) : CorrelatedBy<Guid>;
    public sealed record MediatorBridgeMessage(Guid CorrelationId, MediatorBusRoute Route) : CorrelatedBy<Guid>;

    public sealed class MediatorPublishingBridgeConsumer(IPublishEndpoint publishEndpoint) :
        IConsumer<MediatorBridgeCommand>
    {
        public Task ConsumeAsync(ConsumeContext<MediatorBridgeCommand> context) => publishEndpoint.PublishAsync(
            new MediatorBridgeMessage(context.Message.CorrelationId, context.Message.Route),
            context.CancellationToken);
    }

    public sealed class MediatorSendingBridgeConsumer(ISendEndpointProvider sendEndpointProvider) :
        IConsumer<MediatorBridgeCommand>
    {
        public async Task ConsumeAsync(ConsumeContext<MediatorBridgeCommand> context)
        {
            ISendEndpoint endpoint = await sendEndpointProvider.GetSendEndpointAsync(
                new Uri("queue:container-mediator-ingress"));
            await endpoint.SendAsync(
                new MediatorBridgeMessage(context.Message.CorrelationId, context.Message.Route),
                context.CancellationToken);
        }
    }

    public sealed class MediatorBusIngressConsumer : IConsumer<MediatorBridgeMessage>
    {
        public Task ConsumeAsync(ConsumeContext<MediatorBridgeMessage> context) => Task.CompletedTask;
    }

    public sealed record MediatorBusCommand(Guid CorrelationId, string Value) : CorrelatedBy<Guid>;
    public sealed record MediatorBusEvent(Guid CorrelationId, string Value) : CorrelatedBy<Guid>;

    public sealed class BusPublishingMediatorConsumer(IBus bus) : IConsumer<MediatorBusCommand>
    {
        public Task ConsumeAsync(ConsumeContext<MediatorBusCommand> context) => bus.PublishAsync(
            new MediatorBusEvent(context.Message.CorrelationId, context.Message.Value),
            context.CancellationToken);
    }

    public sealed record OuterMediatorRequest(Guid CorrelationId, string Value) : CorrelatedBy<Guid>;
    public sealed record InnerMediatorRequest(Guid CorrelationId, string Value) : CorrelatedBy<Guid>;
    public sealed class InnerMediatorResponse(
        Guid correlationId,
        Guid originalConversationId,
        Guid originalInitiatorId,
        string value) : CorrelatedBy<Guid>
    {
        public Guid CorrelationId { get; } = correlationId;
        public Guid OriginalConversationId { get; } = originalConversationId;
        public Guid OriginalInitiatorId { get; } = originalInitiatorId;
        public string Value { get; set; } = value;
    }
    public sealed record OuterMediatorResponse(
        Guid CorrelationId,
        Guid OriginalConversationId,
        Guid OriginalInitiatorId,
        string Value) : CorrelatedBy<Guid>;

    public sealed class OuterMediatorRequestConsumer(IRequestClient<InnerMediatorRequest> client) :
        IConsumer<OuterMediatorRequest>
    {
        public async Task ConsumeAsync(ConsumeContext<OuterMediatorRequest> context)
        {
            Response<InnerMediatorResponse> response = await client.GetResponseAsync<InnerMediatorResponse>(
                new InnerMediatorRequest(context.Message.CorrelationId, context.Message.Value),
                context.CancellationToken);
            await context.RespondAsync(new OuterMediatorResponse(
                response.Message.CorrelationId,
                response.Message.OriginalConversationId,
                response.Message.OriginalInitiatorId,
                response.Message.Value));
        }
    }

    public sealed class InnerMediatorRequestConsumer : IConsumer<InnerMediatorRequest>
    {
        public Task ConsumeAsync(ConsumeContext<InnerMediatorRequest> context) => context.RespondAsync(
            new InnerMediatorResponse(
                context.Message.CorrelationId,
                context.ConversationId!.Value,
                context.InitiatorId!.Value,
                $"Hello, {context.Message.Value}"));
    }

    public sealed class UppercaseMediatorResponseFilter<T> : IFilter<SendContext<T>> where T : class
    {
        public Task SendAsync(SendContext<T> context, IPipe<SendContext<T>> next)
        {
            if (context.Message is InnerMediatorResponse inner)
                inner.Value = inner.Value.ToUpperInvariant();
            return next.SendAsync(context);
        }

        public void Probe(ProbeContext context) => context.CreateFilterScope("uppercaseMediatorResponse");
    }

    public sealed record StartMediatorOrder(Guid CorrelationId, string OrderNumber) : CorrelatedBy<Guid>;
    public sealed record MediatorOrderSubmitted(Guid CorrelationId, string OrderNumber) : CorrelatedBy<Guid>;

    public sealed class SagaStartingMediatorConsumer : IConsumer<StartMediatorOrder>
    {
        public Task ConsumeAsync(ConsumeContext<StartMediatorOrder> context) => context.Advanced().PublishAsync(
            new MediatorOrderSubmitted(context.Message.CorrelationId, context.Message.OrderNumber),
            context.CancellationToken);
    }

    public sealed class MediatorOrderSaga : ISaga, InitiatedBy<MediatorOrderSubmitted>
    {
        public Guid CorrelationId { get; set; }
        public string? OrderNumber { get; set; }

        public Task ConsumeAsync(ConsumeContext<MediatorOrderSubmitted> context)
        {
            OrderNumber = context.Message.OrderNumber;
            return Task.CompletedTask;
        }
    }
}
