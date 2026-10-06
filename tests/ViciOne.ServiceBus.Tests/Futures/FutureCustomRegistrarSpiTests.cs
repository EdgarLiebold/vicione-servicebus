using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Advanced.Registration;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Futures;
using Xunit;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;

namespace ViciOne.ServiceBus.Tests.Futures;

public sealed class FutureCustomRegistrarSpiTests
{
    [Theory]
    [InlineData(0, false)]
    [InlineData(0, true)]
    [InlineData(1, false)]
    [InlineData(1, true)]
    [InlineData(2, false)]
    [InlineData(2, true)]
    [RequirementCoverage("REQ-VSB-FUTURE-REGISTRATION", "custom-registrar-public-definition-spi-preserved-for-companions")]
    public void CompanionDefinitions_PreserveTheActualCustomRegistrarPublicSpi(int registrarShape, bool explicitFaultContract)
    {
        var services = new ServiceCollection();
        var calls = new List<(Type Contract, Type Implementation)>();
        IContainerRegistrar registrar = registrarShape switch
        {
            0 => new StandaloneRegistrar(services, calls),
            1 => new OverrideRegistrar(services, calls),
            2 => new ExplicitRegistrar(services, calls),
            _ => throw new ArgumentOutOfRangeException(nameof(registrarShape)),
        };
        var registration = new SelectedRegistrarBusConfigurator(services, registrar);
        Type expectedContract;
        Type expectedDefinition;
        if (explicitFaultContract)
        {
            registration.AddConsumer<Companion, FutureRequestConsumerDefinition<Companion, Command>>();
            registration.AddFuture<ExplicitFaultFuture,
                RequestConsumerFutureDefinition<ExplicitFaultFuture, Companion, Command, Reply, Failure>>();
            expectedContract = typeof(IFutureDefinition<ExplicitFaultFuture>);
            expectedDefinition = typeof(RequestConsumerFutureDefinition<ExplicitFaultFuture, Companion, Command, Reply, Failure>);
        }
        else
        {
            registration.AddFutureRequestConsumer<RequestConsumerFuture<Command, Reply>, Companion, Command, Reply>();
            expectedContract = typeof(IFutureDefinition<RequestConsumerFuture<Command, Reply>>);
            expectedDefinition = typeof(RequestConsumerFutureDefinition<RequestConsumerFuture<Command, Reply>, Companion, Command, Reply>);
        }

        Assert.Equal(new[]
        {
            (typeof(IConsumerDefinition<Companion>), typeof(FutureRequestConsumerDefinition<Companion, Command>)),
            (expectedContract, expectedDefinition),
        }, calls);
        using ServiceProvider provider = services.BuildServiceProvider(validateScopes: true);
        object definition = provider.GetRequiredService(expectedContract);
        Assert.Equal(expectedDefinition, definition.GetType());
        Assert.NotNull(Assert.IsAssignableFrom<IFutureDefinition>(definition).EndpointDefinition);
        var requestDefinition = Assert.IsAssignableFrom<IFutureRequestDefinition<Command>>(definition);
        ConfigurationException beforeEndpoint = Assert.Throws<ConfigurationException>(() => requestDefinition.RequestAddress);
        Assert.Contains("configured", beforeEndpoint.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(2, calls.Count);
    }

    private sealed class SelectedRegistrarBusConfigurator(IServiceCollection services, IContainerRegistrar registrar)
        : ServiceCollectionBusConfigurator(services, registrar);

    private sealed class OverrideRegistrar(IServiceCollection services, List<(Type, Type)> calls)
        : DependencyInjectionContainerRegistrar(services)
    {
        public override void AddDefinition<T, TDefinition>()
        {
            calls.Add((typeof(T), typeof(TDefinition)));
            base.AddDefinition<T, TDefinition>();
        }
    }

    private sealed class ExplicitRegistrar(IServiceCollection services, List<(Type, Type)> calls)
        : DependencyInjectionContainerRegistrar(services), IContainerRegistrar
    {
        void IContainerRegistrar.AddDefinition<T, TDefinition>()
        {
            calls.Add((typeof(T), typeof(TDefinition)));
            base.AddDefinition<T, TDefinition>();
        }
    }

    private sealed class StandaloneRegistrar(IServiceCollection services, List<(Type, Type)> calls) : IContainerRegistrar
    {
        private readonly DependencyInjectionContainerRegistrar _inner = new(services);

        public void AddDefinition<T, TDefinition>() where T : class, IDefinition where TDefinition : class, T
        {
            calls.Add((typeof(T), typeof(TDefinition)));
            _inner.AddDefinition<T, TDefinition>();
        }
        public void AddEndpointDefinition<T, TDefinition>(IEndpointSettings<IEndpointDefinition<T>>? settings = null)
            where T : class where TDefinition : class, IEndpointDefinition<T> => _inner.AddEndpointDefinition<T, TDefinition>(settings);
        public void RegisterRequestClient<T>(RequestTimeout timeout = default) where T : class => _inner.RegisterRequestClient<T>(timeout);
        public void RegisterRequestClient<T>(Uri destinationAddress, RequestTimeout timeout = default) where T : class => _inner.RegisterRequestClient<T>(destinationAddress, timeout);
        public void RegisterScopedClientFactory() => _inner.RegisterScopedClientFactory();
        public void RegisterEndpointNameFormatter(IEndpointNameFormatter formatter) => _inner.RegisterEndpointNameFormatter(formatter);
        public T GetOrAddRegistration<T>(Type type, Func<Type, T>? factory = default) where T : class, IRegistration => _inner.GetOrAddRegistration(type, factory);
        public IEnumerable<T> GetRegistrations<T>() where T : class, IRegistration => _inner.GetRegistrations<T>();
        public IEnumerable<T> GetRegistrations<T>(IServiceProvider provider) where T : class, IRegistration => _inner.GetRegistrations<T>(provider);
        public bool TryGetRegistration<T>(IServiceProvider provider, Type type, [NotNullWhen(true)] out T? value)
            where T : class, IRegistration => _inner.TryGetRegistration(provider, type, out value);
        public T? GetDefinition<T>(IServiceProvider provider) where T : class, IDefinition => _inner.GetDefinition<T>(provider);
        public IEndpointDefinition<T>? GetEndpointDefinition<T>(IServiceProvider provider) where T : class => _inner.GetEndpointDefinition<T>(provider);
        public IConfigureReceiveEndpoint GetConfigureReceiveEndpoints(IServiceProvider provider) => _inner.GetConfigureReceiveEndpoints(provider);
        public IEndpointNameFormatter GetEndpointNameFormatter(IServiceProvider provider) => _inner.GetEndpointNameFormatter(provider);
    }

    public sealed record Command(Guid CorrelationId) : ICorrelatedBy<Guid>;
    public sealed record Reply(string Value);
    public sealed record Failure(string Value);
    public sealed class ExplicitFaultFuture : Future<Command, Reply, Failure>;
    public sealed class Companion : IConsumer<Command>
    {
        public Task ConsumeAsync(ConsumeContext<Command> context) => Task.CompletedTask;
    }
}
