using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.DependencyInjection.Registration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.DependencyInjection;

public sealed class EndpointRegistrationTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-ENDPOINT-REGISTRATION", "configure-endpoints-inclusion-is-owned-by-the-registration")]
    public void IncludeInConfigureEndpoints_UpdatesTheAuthoritativeRegistration()
    {
        var owner = new RegistrationOwner { IncludeInConfigureEndpoints = true };
        var endpoint = new EndpointRegistration<RegisteredType>(
            owner,
            new DependencyInjectionContainerRegistrar(new ServiceCollection()));

        endpoint.IncludeInConfigureEndpoints = false;

        Assert.False(owner.IncludeInConfigureEndpoints);
        Assert.False(endpoint.IncludeInConfigureEndpoints);

        endpoint.IncludeInConfigureEndpoints = true;

        Assert.True(owner.IncludeInConfigureEndpoints);
        Assert.True(endpoint.IncludeInConfigureEndpoints);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ENDPOINT-REGISTRATION", "constructor-required-arguments")]
    public void Constructor_RejectsMissingCollaborators()
    {
        var owner = new RegistrationOwner();
        var selector = new DependencyInjectionContainerRegistrar(new ServiceCollection());

        Assert.Equal("registration", Assert.Throws<ArgumentNullException>(() =>
            new EndpointRegistration<RegisteredType>(null!, selector)).ParamName);
        Assert.Equal("selector", Assert.Throws<ArgumentNullException>(() =>
            new EndpointRegistration<RegisteredType>(owner, null!)).ParamName);
    }

    private sealed class RegistrationOwner : IRegistration
    {
        public Type Type => typeof(RegisteredType);

        public bool IncludeInConfigureEndpoints { get; set; }
    }

    private sealed class RegisteredType;
}
