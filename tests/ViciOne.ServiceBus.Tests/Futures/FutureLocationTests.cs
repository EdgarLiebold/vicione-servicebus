using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Futures;

public sealed class FutureLocationTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-FUTURE-LOCATION-ROUNDTRIP", "endpoint-normalization")]
    public void UriRoundTrip_PreservesIdAndNormalizesTheEndpointAddress()
    {
        var id = Guid.Parse("ab242cad-7f28-4380-93f4-bd667daf6caf");
        var source = new FutureLocation(id, new Uri("loopback://localhost/input-queue"));

        Uri location = source;
        var returned = new FutureLocation(location);

        Assert.Equal("queue", location.Scheme);
        Assert.Equal("input-queue", location.AbsolutePath);
        Assert.Equal(id, returned.Id);
        Assert.Equal(new Uri("queue:input-queue"), returned.Address);
    }

    [Theory]
    [InlineData("queue:input-queue")]
    [InlineData("queue:input-queue?id=")]
    [InlineData("queue:input-queue?id=not-a-valid-future-id")]
    [InlineData("queue:input-queue?id=ybndrfg8ejkmcpqxot1uwisza3&id=ybndrfg8ejkmcpqxot1uwisza3")]
    [InlineData("input-queue?id=relative")]
    [RequirementCoverage("REQ-VSB-FUTURE-LOCATION-VALIDATION", "invalid-id-query")]
    public void InvalidLocationUris_AreRejectedWithAStableFormatError(string value)
    {
        var location = new Uri(value, UriKind.RelativeOrAbsolute);

        var exception = Assert.Throws<FormatException>(() => new FutureLocation(location));

        Assert.Equal($"Location format invalid: {location}", exception.Message);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-FUTURE-LOCATION-VALIDATION", "null-input")]
    public void Constructors_RejectNullLocations()
    {
        var id = Guid.Parse("ab242cad-7f28-4380-93f4-bd667daf6caf");

        var locationException = Assert.Throws<ArgumentNullException>(() => new FutureLocation(null!));
        var addressException = Assert.Throws<ArgumentNullException>(() => new FutureLocation(id, null!));

        Assert.Equal("location", locationException.ParamName);
        Assert.Equal("address", addressException.ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-FUTURE-LOCATION-VALIDATION", "nonempty-future-identifier")]
    public void Constructor_RejectsAnEmptyFutureIdentifier()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            new FutureLocation(Guid.Empty, new Uri("loopback://localhost/input-queue")));

        Assert.Equal("id", exception.ParamName);
    }

    [Theory]
    [InlineData("loopback://localhost/", "Address must contain an endpoint name.")]
    [InlineData("input-queue", "Address must be an absolute URI.")]
    [RequirementCoverage("REQ-VSB-FUTURE-LOCATION-VALIDATION", "invalid-address")]
    public void InvalidEndpointAddresses_AreRejected(string value, string reason)
    {
        var id = Guid.Parse("ab242cad-7f28-4380-93f4-bd667daf6caf");
        var address = new Uri(value, UriKind.RelativeOrAbsolute);

        var exception = Assert.Throws<ArgumentException>(() => new FutureLocation(id, address));

        Assert.Equal("address", exception.ParamName);
        Assert.StartsWith(reason, exception.Message, StringComparison.Ordinal);
    }
}
