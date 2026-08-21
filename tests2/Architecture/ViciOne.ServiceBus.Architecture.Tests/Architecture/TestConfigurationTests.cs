using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using Xunit;

namespace ViciOne.ServiceBus.Architecture.Tests.Architecture;

/// <summary>Tests the one typed and secret-free configuration boundary.</summary>
public sealed class TestConfigurationTests
{
    private static TestConfigurationProvider ProviderWith(params (string Key, string Value)[] environment) =>
        new(
            AppContext.BaseDirectory,
            environment.Select(entry => new KeyValuePair<string, string?>(entry.Key, entry.Value)));

    [Fact]
    public void CheckedInDefaults_AreBoundOntoTypedOptions()
    {
        var options = ProviderWith().GetOptions();

        Assert.Equal(TestProfile.UnitArchitecture, options.Profile);
        Assert.Equal(TimeSpan.FromSeconds(30), options.OperationTimeout);
        Assert.Equal("localhost", options.LocalInfrastructure.RabbitMqHost);
        Assert.Equal(5672, options.LocalInfrastructure.RabbitMqPort);
    }

    [Fact]
    public void PrefixedEnvironmentEntry_OverridesTheCheckedInDefault()
    {
        var options = ProviderWith(("VICIONE_TESTS__Profile", "LocalIntegration")).GetOptions();

        Assert.Equal(TestProfile.LocalIntegration, options.Profile);
    }

    [Fact]
    public void DoubleUnderscore_BindsNestedValuesWithoutResettingNeighbours()
    {
        var options = ProviderWith(
            ("VICIONE_TESTS__LocalInfrastructure__RabbitMqHost", "broker.internal"),
            ("VICIONE_TESTS__LocalInfrastructure__RabbitMqPort", "5673")).GetOptions();

        Assert.Equal("broker.internal", options.LocalInfrastructure.RabbitMqHost);
        Assert.Equal(5673, options.LocalInfrastructure.RabbitMqPort);
        Assert.Equal("localhost", options.LocalInfrastructure.PostgreSqlHost);
    }

    [Fact]
    public void UnprefixedEnvironmentEntry_IsIgnored()
    {
        var options = ProviderWith(("Profile", "External")).GetOptions();

        Assert.Equal(TestProfile.UnitArchitecture, options.Profile);
    }

    [Fact]
    public void Sources_AreLayeredFileThenUserSecretsThenEnvironment()
    {
        var withoutSecrets = ProviderWith().SourceOrder;
        var withSecrets = new TestConfigurationProvider(
            AppContext.BaseDirectory, [], includeUserSecrets: true).SourceOrder;

        Assert.Equal(["JsonConfigurationProvider", "MemoryConfigurationProvider"], withoutSecrets);
        Assert.Equal(
            ["JsonConfigurationProvider", "JsonConfigurationProvider", "MemoryConfigurationProvider"],
            withSecrets);
    }

    [Fact]
    public void SharedUserSecretsStore_IsDeclaredOnceForTheTree() =>
        Assert.Equal("vicione-servicebus-native-tests", TestConfigurationProvider.SharedUserSecretsId);

    [Fact]
    public void UnitProfile_DefaultsAreValidWithoutInfrastructureSelectors()
    {
        var options = ProviderWith().GetOptions();

        Assert.Empty(options.ValidateFor());
    }

    [Fact]
    public void UnknownProfile_IsRejected()
    {
        var options = ProviderWith(("VICIONE_TESTS__Profile", "99")).GetOptions();

        Assert.Contains(nameof(ViciOneTestOptions.Profile), options.ValidateFor());
    }

    [Fact]
    public void NonPositiveTimeout_IsRejected()
    {
        var options = ProviderWith(("VICIONE_TESTS__OperationTimeout", "00:00:00")).GetOptions();

        Assert.Contains(nameof(ViciOneTestOptions.OperationTimeout), options.ValidateFor());
    }

    [Theory]
    [InlineData("VICIONE_TESTS__LocalInfrastructure__RabbitMqHost", "")]
    [InlineData("VICIONE_TESTS__LocalInfrastructure__RabbitMqPort", "0")]
    [InlineData("VICIONE_TESTS__LocalInfrastructure__RabbitMqPort", "65536")]
    [InlineData("VICIONE_TESTS__LocalInfrastructure__PostgreSqlHost", "")]
    [InlineData("VICIONE_TESTS__LocalInfrastructure__PostgreSqlPort", "0")]
    [InlineData("VICIONE_TESTS__LocalInfrastructure__PostgreSqlPort", "65536")]
    public void LocalIntegrationProfile_RejectsAnInvalidEndpoint(string key, string value)
    {
        var options = ProviderWith(
            ("VICIONE_TESTS__Profile", "LocalIntegration"),
            (key, value)).GetOptions();

        Assert.NotEmpty(options.ValidateFor());
    }

    [Fact]
    public void NonExternalProfile_RejectsAnExternalProviderSelection()
    {
        var options = ProviderWith().GetOptions();

        Assert.Contains(nameof(ViciOneTestOptions.ExternalProviders),
            options.ValidateFor(ExternalProvider.Azure));
    }

    [Fact]
    public void ExternalProfile_RequiresAtLeastOneProviderSelection()
    {
        var options = ProviderWith(("VICIONE_TESTS__Profile", "External")).GetOptions();

        Assert.Contains("ExternalProviders:Selection", options.ValidateFor());
    }

    [Fact]
    public void ExternalProfile_RejectsAnUnknownProviderSelection()
    {
        var options = ProviderWith(("VICIONE_TESTS__Profile", "External")).GetOptions();

        Assert.Contains("ExternalProviders:Selection", options.ValidateFor((ExternalProvider)99));
    }

    [Theory]
    [InlineData(ExternalProvider.Azure)]
    [InlineData(ExternalProvider.Aws)]
    public void ExternalProfile_RejectsEmulatorMode(ExternalProvider provider)
    {
        var options = ProviderWith(("VICIONE_TESTS__Profile", "External")).GetOptions();

        var errors = options.ValidateFor(provider);

        Assert.Contains($"ExternalProviders:{provider}:Mode", errors);
    }

    [Fact]
    public void ExternalProfile_RejectsANullSelectedProviderGroup()
    {
        var options = ProviderWith(("VICIONE_TESTS__Profile", "External")).GetOptions();
        options.ExternalProviders.Azure = null!;

        Assert.Contains("ExternalProviders:Azure", options.ValidateFor(ExternalProvider.Azure));
    }

    [Fact]
    public void AwsOnlyRun_DoesNotRequireAzureResourceConfiguration()
    {
        var options = ProviderWith(
            ("VICIONE_TESTS__Profile", "External"),
            ("VICIONE_TESTS__ExternalProviders__Aws__Mode", "Real"),
            ("VICIONE_TESTS__ExternalProviders__Aws__Region", "eu-central-1"),
            ("VICIONE_TESTS__ExternalProviders__Aws__ResourceNamePrefix", "run-1")).GetOptions();

        Assert.Empty(options.ValidateFor(ExternalProvider.Aws));
    }

    [Fact]
    public void AzureOnlyRun_DoesNotRequireAwsResourceConfiguration()
    {
        var options = ProviderWith(
            ("VICIONE_TESTS__Profile", "External"),
            ("VICIONE_TESTS__ExternalProviders__Azure__Mode", "Real"),
            ("VICIONE_TESTS__ExternalProviders__Azure__SubscriptionId", "sub-1"),
            ("VICIONE_TESTS__ExternalProviders__Azure__ResourceGroup", "rg-1"),
            ("VICIONE_TESTS__ExternalProviders__Azure__Location", "westeurope"),
            ("VICIONE_TESTS__ExternalProviders__Azure__ResourceNamePrefix", "run-1")).GetOptions();

        Assert.Empty(options.ValidateFor(ExternalProvider.Azure));
    }

    [Fact]
    public void RealAzureRun_NamesEveryMissingResourceSetting()
    {
        var options = ProviderWith(
            ("VICIONE_TESTS__Profile", "External"),
            ("VICIONE_TESTS__ExternalProviders__Azure__Mode", "Real")).GetOptions();

        Assert.Equal(
            [
                "ExternalProviders:Azure:SubscriptionId",
                "ExternalProviders:Azure:ResourceGroup",
                "ExternalProviders:Azure:Location",
                "ExternalProviders:Azure:ResourceNamePrefix",
            ],
            options.ValidateFor(ExternalProvider.Azure));
    }

    [Fact]
    public void DuplicateProviderSelection_IsRejected()
    {
        var options = ProviderWith(
            ("VICIONE_TESTS__Profile", "External"),
            ("VICIONE_TESTS__ExternalProviders__Aws__Mode", "Real")).GetOptions();

        Assert.Contains(
            "ExternalProviders:Aws:DuplicateSelection",
            options.ValidateFor(ExternalProvider.Aws, ExternalProvider.Aws));
    }

    [Fact]
    public void ProviderContracts_ExposeOnlyTheApprovedNonSecretProperties()
    {
        Assert.Equal(
            ["Location", "Mode", "Name", "ResourceGroup", "ResourceNamePrefix", "SubscriptionId"],
            PublicPropertyNames<AzureProviderOptions>());
        Assert.Equal(
            ["Mode", "Name", "Region", "ResourceNamePrefix"],
            PublicPropertyNames<AwsProviderOptions>());
        Assert.Empty(PublicFields<AzureProviderOptions>());
        Assert.Empty(PublicFields<AwsProviderOptions>());
        Assert.Empty(PublicFields<ExternalProviderOptions>());
    }

    [Fact]
    public void GetValidatedOptions_FailsBeforeExecutionAndNamesTheProblem()
    {
        var provider = ProviderWith(
            ("VICIONE_TESTS__Profile", "External"),
            ("VICIONE_TESTS__ExternalProviders__Azure__Mode", "Real"));

        var failure = Assert.Throws<InvalidOperationException>(
            () => provider.GetValidatedOptions(ExternalProvider.Azure));

        Assert.Contains("ExternalProviders:Azure:SubscriptionId", failure.Message);
        Assert.Contains("External", failure.Message);
        Assert.Contains("Azure.Identity", failure.Message);
    }

    [Fact]
    public void ExternalPreflightBoundary_ExposesAnAsynchronousAccessCheck()
    {
        var method = typeof(IExternalAccessPreflight).GetMethod(
            nameof(IExternalAccessPreflight.EnsureAccessAsync));

        Assert.NotNull(method);
        Assert.Equal(typeof(Task), method!.ReturnType);
        Assert.Equal([typeof(CancellationToken)],
            method.GetParameters().Select(parameter => parameter.ParameterType).ToArray());
    }

    private static string[] PublicPropertyNames<T>() =>
        typeof(T).GetProperties()
            .Select(property => property.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

    private static string[] PublicFields<T>() =>
        typeof(T).GetFields()
            .Select(field => field.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
}
