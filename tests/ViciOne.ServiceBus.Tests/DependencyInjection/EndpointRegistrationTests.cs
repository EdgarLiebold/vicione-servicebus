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
        var endpoint = new EndpointRegistration<RegisteredType>(owner, null!);

        endpoint.IncludeInConfigureEndpoints = false;

        Assert.False(owner.IncludeInConfigureEndpoints);
        Assert.False(endpoint.IncludeInConfigureEndpoints);

        endpoint.IncludeInConfigureEndpoints = true;

        Assert.True(owner.IncludeInConfigureEndpoints);
        Assert.True(endpoint.IncludeInConfigureEndpoints);
    }

    private sealed class RegistrationOwner : IRegistration
    {
        public Type Type => typeof(RegisteredType);

        public bool IncludeInConfigureEndpoints { get; set; }
    }

    private sealed class RegisteredType;
}
