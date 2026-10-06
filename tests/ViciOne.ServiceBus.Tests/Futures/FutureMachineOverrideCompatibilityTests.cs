using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Advanced.Registration;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Futures;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;
using Command = ViciOne.ServiceBus.Tests.Futures.FutureSameTypeBusOwnershipTests.Command;
using Reply = ViciOne.ServiceBus.Tests.Futures.FutureSameTypeBusOwnershipTests.Reply;

namespace ViciOne.ServiceBus.Tests.Futures;

public sealed class FutureMachineOverrideCompatibilityTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [RequirementCoverage("REQ-VSB-FUTURE-REGISTRATION", "explicit-builtin-machine-instance-factory-and-provider-rebinding-preserved")]
    public async Task ExplicitBuiltinOverrides_RemainTheConfiguredMachineAndServeTheirOwnCompanionAsync(int mode)
    {
        Uri hostAddress = new($"loopback://localhost/future-override-{NewId.NextGuid():N}");
        var observations = new RouteObservations();
        var manualDefinition = new FutureRequestConsumerDefinition<ManualCompanion, Command>();
        string manualEndpointName = "manual-companion";
        int factoryCalls = 0;
        ServiceProvider? origin = null;
        try
        {
            RequestConsumerFuture<Command, Reply> selected;
            if (mode == 2)
            {
                var originServices = new ServiceCollection();
                originServices.AddSingleton(new RouteObservations());
                originServices.AddViciOneServiceBus(registration =>
                {
                    registration.Limits(MessageLimits.Conservative);
                    registration.SetInMemorySagaRepositoryProvider();
                    registration.AddFutureRequestConsumer<RequestConsumerFuture<Command, Reply>, OriginCompanion, Command, Reply>();
                    registration.UsingInMemory((context, bus) =>
                    {
                        bus.Host(hostAddress);
                        bus.ConfigureEndpoints(context);
                    });
                });
                origin = originServices.BuildServiceProvider(validateScopes: true);
                Assert.NotNull(origin.GetRequiredService<IBus>().Address);
                selected = origin.GetRequiredService<RequestConsumerFuture<Command, Reply>>();
                manualEndpointName = origin.GetRequiredService<IConsumerDefinition<OriginCompanion>>()
                    .GetEndpointName(DefaultEndpointNameFormatter.Instance);
            }
            else
            {
                selected = new RequestConsumerFuture<Command, Reply>(
                    new RequestConsumerFutureDefinition<RequestConsumerFuture<Command, Reply>, ManualCompanion, Command, Reply>(manualDefinition));
            }

            var services = new ServiceCollection();
            services.AddSingleton(observations);
            if (mode == 1)
                services.AddSingleton<RequestConsumerFuture<Command, Reply>>(_ =>
                {
                    Interlocked.Increment(ref factoryCalls);
                    return selected;
                });
            else
                services.AddSingleton(selected);
            services.AddViciOneServiceBus(registration =>
            {
                registration.Limits(MessageLimits.Conservative);
                registration.SetInMemorySagaRepositoryProvider();
                registration.AddConsumer<ManualCompanion>();
                registration.AddFutureRequestConsumer<RequestConsumerFuture<Command, Reply>, RegisteredCompanion, Command, Reply>();
                registration.UsingInMemory((context, bus) =>
                {
                    bus.Host(hostAddress);
                    bus.ReceiveEndpoint(manualEndpointName, endpoint =>
                        context.ConfigureConsumer<ManualCompanion>(endpoint, consumer =>
                            ((IConsumerDefinition<ManualCompanion>)manualDefinition).Configure(endpoint, consumer, context)));
                    bus.ConfigureEndpoints(context);
                });
            });
            await using ServiceProvider provider = services.BuildServiceProvider(validateScopes: true);
            IBusControl bus = Assert.IsAssignableFrom<IBusControl>(provider.GetRequiredService<IBus>());
            Assert.Same(selected, provider.GetRequiredService<RequestConsumerFuture<Command, Reply>>());
            var futureDefinition = provider.GetRequiredService<IFutureDefinition<RequestConsumerFuture<Command, Reply>>>();
            Uri futureEndpoint = new(bus.Address, futureDefinition.GetEndpointName(DefaultEndpointNameFormatter.Instance));
            using var operations = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
            operations.CancelAfter(Timeout);
            Guid id = NewId.NextGuid();
            try
            {
                await bus.StartAsync(operations.Token);
                IRequestClient<Command> client = bus.CreateRequestClient<Command>(futureEndpoint, new RequestTimeout(Timeout));
                using RequestHandle<Command> request = client.Create(new Command(id), cancellationToken: operations.Token);
                Task<Response<Reply>> response = request.GetResponseAsync<Reply>(cancellationToken: operations.Token);
                await Task.WhenAll(request.Message, response);
                Assert.Equal(new Reply(id, "manual"), (await response).Message);
                Assert.Equal(1, observations.Manual);
                Assert.Equal(0, observations.Registered);
                Assert.Equal(mode == 1 ? 1 : 0, factoryCalls);
                Assert.Same(selected, provider.GetRequiredService<RequestConsumerFuture<Command, Reply>>());
            }
            finally
            {
                operations.Cancel();
                await bus.StopAsync(CancellationToken.None).WaitAsync(Timeout, CancellationToken.None);
            }
        }
        finally
        {
            if (origin != null)
                await origin.DisposeAsync();
        }
    }

    public sealed class RouteObservations
    {
        public int Manual;
        public int Registered;
    }

    public sealed class ManualCompanion(RouteObservations observations) : IConsumer<Command>
    {
        public Task ConsumeAsync(ConsumeContext<Command> context)
        {
            Interlocked.Increment(ref observations.Manual);
            return context.Advanced().RespondAsync(new Reply(context.Message.CorrelationId, "manual"));
        }
    }

    public sealed class RegisteredCompanion(RouteObservations observations) : IConsumer<Command>
    {
        public Task ConsumeAsync(ConsumeContext<Command> context)
        {
            Interlocked.Increment(ref observations.Registered);
            return context.Advanced().RespondAsync(new Reply(context.Message.CorrelationId, "registered"));
        }
    }

    public sealed class OriginCompanion : IConsumer<Command>
    {
        public Task ConsumeAsync(ConsumeContext<Command> context) =>
            context.Advanced().RespondAsync(new Reply(context.Message.CorrelationId, "origin"));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-FUTURE-REGISTRATION", "wrapped-generated-machine-preserves-selected-bus-companion-ownership")]
    public async Task SavedGeneratedFactoryWrapper_RetainsEachTypedBusCompanionOwnershipAsync()
    {
        string unique = NewId.NextGuid().ToString("N");
        var services = new ServiceCollection();
        var observations = new FutureSameTypeBusOwnershipTests.CompanionObservations();
        services.AddSingleton(observations);
        services.AddViciOneServiceBus(registration =>
        {
            registration.Limits(MessageLimits.Conservative);
            registration.UsingInMemory((_, _) => { });
        });
        services.AddViciOneServiceBus<FutureSameTypeBusOwnershipTests.IFirstFutureBus>(registration =>
        {
            registration.Limits(MessageLimits.Conservative);
            registration.SetInMemorySagaRepositoryProvider();
            registration.AddFutureRequestConsumer<RequestConsumerFuture<Command, Reply>,
                FutureSameTypeBusOwnershipTests.FirstCompanion, Command, Reply>();
            registration.UsingInMemory((context, bus) =>
            {
                bus.Host(new Uri($"loopback://localhost/wrapped-future-first-{unique}"));
                bus.ConfigureEndpoints(context);
            });
        });
        services.AddViciOneServiceBus<FutureSameTypeBusOwnershipTests.ISecondFutureBus>(registration =>
        {
            registration.Limits(MessageLimits.Conservative);
            registration.SetInMemorySagaRepositoryProvider();
            registration.AddFutureRequestConsumer<RequestConsumerFuture<Command, Reply>,
                FutureSameTypeBusOwnershipTests.SecondCompanion, Command, Reply>();
            registration.UsingInMemory((context, bus) =>
            {
                bus.Host(new Uri($"loopback://localhost/wrapped-future-second-{unique}"));
                bus.ConfigureEndpoints(context);
            });
        });
        ServiceDescriptor automatic = Assert.Single(services, descriptor =>
            descriptor.ServiceType == typeof(RequestConsumerFuture<Command, Reply>) && !descriptor.IsKeyedService);
        Func<IServiceProvider, object> savedFactory = Assert.IsAssignableFrom<Func<IServiceProvider, object>>(
            automatic.ImplementationFactory);
        int wrapperCalls = 0;
        RequestConsumerFuture<Command, Reply>? selected = null;
        services.AddSingleton<RequestConsumerFuture<Command, Reply>>(provider =>
        {
            Interlocked.Increment(ref wrapperCalls);
            selected = Assert.IsType<RequestConsumerFuture<Command, Reply>>(savedFactory(provider));
            return selected;
        });
        await using ServiceProvider provider = services.BuildServiceProvider(validateScopes: true);
        IBusControl first = Assert.IsAssignableFrom<IBusControl>(
            provider.GetRequiredService<FutureSameTypeBusOwnershipTests.IFirstFutureBus>());
        IBusControl second = Assert.IsAssignableFrom<IBusControl>(
            provider.GetRequiredService<FutureSameTypeBusOwnershipTests.ISecondFutureBus>());
        Assert.NotNull(selected);
        Assert.Same(selected, provider.GetRequiredService<RequestConsumerFuture<Command, Reply>>());
        Assert.Equal(1, wrapperCalls);
        var firstDefinition = provider.GetRequiredService<Bind<FutureSameTypeBusOwnershipTests.IFirstFutureBus,
            IFutureDefinition<RequestConsumerFuture<Command, Reply>>>>().Value;
        var secondDefinition = provider.GetRequiredService<Bind<FutureSameTypeBusOwnershipTests.ISecondFutureBus,
            IFutureDefinition<RequestConsumerFuture<Command, Reply>>>>().Value;
        Uri firstEndpoint = new(first.Address, firstDefinition.GetEndpointName(DefaultEndpointNameFormatter.Instance));
        Uri secondEndpoint = new(second.Address, secondDefinition.GetEndpointName(DefaultEndpointNameFormatter.Instance));
        Assert.NotEqual(firstEndpoint, secondEndpoint);
        using var operations = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        operations.CancelAfter(Timeout);
        Guid firstId = NewId.NextGuid(), secondId = NewId.NextGuid();
        try
        {
            await first.StartAsync(operations.Token);
            await second.StartAsync(operations.Token);
            Assert.Equal(new Reply(firstId, "first"), await SendWrappedRequestAsync(first, firstEndpoint,
                new Command(firstId), operations.Token));
            Assert.Equal(new[] { new Reply(firstId, "first") }, observations.Messages.ToArray());
            Assert.Equal(new Reply(secondId, "second"), await SendWrappedRequestAsync(second, secondEndpoint,
                new Command(secondId), operations.Token));
            Assert.Equal(new[] { new Reply(firstId, "first"), new Reply(secondId, "second") }, observations.Messages.ToArray());
            Assert.Equal(1, wrapperCalls);
            Assert.Same(selected, provider.GetRequiredService<RequestConsumerFuture<Command, Reply>>());
        }
        finally
        {
            operations.Cancel();
            await Task.WhenAll(first.StopAsync(CancellationToken.None), second.StopAsync(CancellationToken.None))
                .WaitAsync(Timeout, CancellationToken.None);
        }
    }

    private static async Task<Reply> SendWrappedRequestAsync(IBus bus, Uri endpoint, Command command,
        CancellationToken cancellationToken)
    {
        IRequestClient<Command> client = bus.CreateRequestClient<Command>(endpoint, new RequestTimeout(Timeout));
        using RequestHandle<Command> request = client.Create(command, cancellationToken: cancellationToken);
        Task<Response<Reply>> response = request.GetResponseAsync<Reply>(cancellationToken: cancellationToken);
        await Task.WhenAll(request.Message, response);
        return (await response).Message;
    }
}
