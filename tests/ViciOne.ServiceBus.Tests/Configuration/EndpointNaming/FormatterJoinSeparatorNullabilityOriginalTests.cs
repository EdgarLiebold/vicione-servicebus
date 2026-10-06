using System.Reflection;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Configuration.EndpointNaming;

public sealed class FormatterJoinSeparatorNullabilityOriginalTests
{
    [Theory]
    [InlineData(ConstructorShape.ProtectedDefault, "ProbeMessage")]
    [InlineData(ConstructorShape.BooleanWithoutNamespace, "ProbeMessage")]
    [InlineData(ConstructorShape.BooleanWithNamespace,
        "ViciOneServiceBusTestsConfigurationEndpointNamingFormatterJoinSeparatorNullabilityOriginalTestsProbeMessage")]
    [InlineData(ConstructorShape.PrefixOnly, "DevProbeMessage")]
    [InlineData(ConstructorShape.PrefixWithNamespace,
        "DevViciOneServiceBusTestsConfigurationEndpointNamingFormatterJoinSeparatorNullabilityOriginalTestsProbeMessage")]
    [RequirementCoverage("REQ-VSB-ENDPOINT-NAME-JOIN-SEPARATOR-NULLABILITY", "supported-null-default-and-emitted-read-contract")]
    public void SupportedConstructor_NullSeparatorMatchesFormattingAndEmittedReadContract(
        ConstructorShape shape,
        string expectedName)
    {
        ProbeFormatter formatter = shape switch
        {
            ConstructorShape.ProtectedDefault => new ProbeFormatter(),
            ConstructorShape.BooleanWithoutNamespace => new ProbeFormatter(false),
            ConstructorShape.BooleanWithNamespace => new ProbeFormatter(true),
            ConstructorShape.PrefixOnly => new ProbeFormatter("Dev"),
            ConstructorShape.PrefixWithNamespace => new ProbeFormatter("Dev", true),
            _ => throw new ArgumentOutOfRangeException(nameof(shape), shape, null),
        };

        Assert.Null(formatter.ReadJoinSeparator());
        Assert.Equal(expectedName, formatter.Message<ProbeMessage>());

        PropertyInfo property = OriginalProtectedSeparator();
        NullabilityInfo info = new NullabilityInfoContext().Create(property);

        // The real getter and public formatting route above establish the supported null value.
        Assert.Equal(NullabilityState.Nullable, info.ReadState);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ENDPOINT-NAME-JOIN-SEPARATOR-NULLABILITY", "explicit-separator-preserves-namespace-and-prefix")]
    public void ExplicitSeparator_PreservesActualGetterNamespaceAndPrefix()
    {
        var formatter = new ProbeFormatter("__", "Dev-", true);

        Assert.Equal("__", formatter.ReadJoinSeparator());
        Assert.Equal(
            "Dev-ViciOne__ServiceBus__Tests__Configuration__EndpointNaming__FormatterJoinSeparatorNullabilityOriginalTests__ProbeMessage",
            formatter.Message<ProbeMessage>());
        Assert.Equal(typeof(string), new NullabilityInfoContext().Create(OriginalProtectedSeparator()).Type);
    }

    private static PropertyInfo OriginalProtectedSeparator()
    {
        PropertyInfo? property = typeof(DefaultEndpointNameFormatter).GetProperty(
            "JoinSeparator", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);

        Assert.NotNull(property);
        Assert.Equal(typeof(DefaultEndpointNameFormatter), property.DeclaringType);
        Assert.Equal(typeof(string), property.PropertyType);
        Assert.NotNull(property.GetMethod);
        Assert.True(property.GetMethod.IsFamily);
        return property;
    }

    public enum ConstructorShape
    {
        ProtectedDefault,
        BooleanWithoutNamespace,
        BooleanWithNamespace,
        PrefixOnly,
        PrefixWithNamespace,
    }

    private sealed class ProbeFormatter : DefaultEndpointNameFormatter
    {
        public ProbeFormatter() : base()
        {
        }

        public ProbeFormatter(bool includeNamespace) : base(includeNamespace)
        {
        }

        public ProbeFormatter(string prefix) : base(prefix)
        {
        }

        public ProbeFormatter(string prefix, bool includeNamespace) : base(prefix, includeNamespace)
        {
        }

        public ProbeFormatter(string joinSeparator, string prefix, bool includeNamespace)
            : base(joinSeparator, prefix, includeNamespace)
        {
        }

        public string? ReadJoinSeparator() => JoinSeparator;
    }

    private sealed record ProbeMessage;
}
