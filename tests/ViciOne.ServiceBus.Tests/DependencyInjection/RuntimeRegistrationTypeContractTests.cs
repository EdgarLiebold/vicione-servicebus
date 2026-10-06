using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.DependencyInjection;

public sealed class RuntimeRegistrationTypeContractTests
{
    [Theory]
    [InlineData(0, "consumerType")]
    [InlineData(1, "endpointDefinitionType")]
    [InlineData(2, "endpointDefinitionType")]
    [RequirementCoverage("REQ-VSB-DI-PUBLIC-BOUNDARIES", "runtime-configurator-consumer-and-endpoint-implementation-must-be-closed")]
    public void RuntimeRegistration_RejectsOpenImplementationsBeforeMutationAndAcceptsClosedControls(int route, string parameterName)
    {
        // The implementation is open, although its message/component interface is closed.
        var services = new ServiceCollection()
            .AddSingleton("unrelated-registration-marker")
            .AddScoped<List<Guid>>();
        var configurator = route == 0 ? new ServiceCollectionBusConfigurator(services) : null;
        ServiceDescriptor[] before = services.ToArray();

        ArgumentException failure = Assert.Throws<ArgumentException>(() =>
        {
            if (route == 0)
                RegistrationConfiguratorExtensions.AddConsumer(configurator!, typeof(OpenConsumer<>));
            else if (route == 1)
                services.RegisterEndpoint(typeof(OpenEndpointDefinition<>));
            else
                services.RegisterEndpoint(new DependencyInjectionContainerRegistrar(services), typeof(OpenEndpointDefinition<>));
        });

        Assert.Equal(parameterName, failure.ParamName);
        Assert.Equal(before.Length, services.Count);
        for (int i = 0; i < before.Length; i++)
            Assert.Same(before[i], services[i]);

        if (route == 0)
        {
            Assert.NotNull(RegistrationConfiguratorExtensions.AddConsumer(configurator!, typeof(OpenConsumer<int>)));
            ServiceDescriptor descriptor = Assert.Single(services, item => item.ServiceType == typeof(OpenConsumer<int>));
            Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
        }
        else
        {
            IEndpointRegistration first = route == 1
                ? services.RegisterEndpoint(typeof(OpenEndpointDefinition<int>))
                : services.RegisterEndpoint(new DependencyInjectionContainerRegistrar(services), typeof(OpenEndpointDefinition<int>));
            IEndpointRegistration duplicate = route == 1
                ? services.RegisterEndpoint(typeof(OpenEndpointDefinition<int>))
                : services.RegisterEndpoint(new DependencyInjectionContainerRegistrar(services), typeof(OpenEndpointDefinition<int>));
            Assert.Equal(typeof(Message), first.Type);
            Assert.Same(first, duplicate);
            using ServiceProvider provider = services.BuildServiceProvider();
            Assert.IsType<OpenEndpointDefinition<int>>(provider.GetRequiredService<IEndpointDefinition<Message>>());
        }
    }

    public sealed class OpenConsumer<TUnused> : IConsumer<Message>
    {
        public Task ConsumeAsync(ConsumeContext<Message> context) => Task.CompletedTask;
    }

    public sealed class OpenEndpointDefinition<TUnused> : DefaultEndpointDefinition, IEndpointDefinition<Message>
    {
        public override string GetEndpointName(IEndpointNameFormatter formatter) => "runtime-type-contract";
    }

    public sealed record Message;
}
