using System.Text;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Infrastructure.Tests.Configuration;

/// <summary>Tests the one typed and secret-free configuration boundary.</summary>
public sealed class TestConfigurationProviderTests
{
    private static TestConfigurationProvider ProviderWith(params (string Key, string Value)[] environment) =>
        new(
            AppContext.BaseDirectory,
            environment.Select(entry => new KeyValuePair<string, string?>(entry.Key, entry.Value)));

    [Fact]
    public void CheckedInDefaults_AreBoundOntoTypedOptions()
    {
        var options = ProviderWith().GetOptions();
        var localInfrastructure = Assert.IsType<LocalInfrastructureOptions>(options.LocalInfrastructure);

        Assert.Equal(TestProfile.UnitArchitecture, options.Profile);
        Assert.Equal(TimeSpan.FromSeconds(30), options.OperationTimeout);
        Assert.Equal("localhost", localInfrastructure.RabbitMqHost);
        Assert.Equal(5672, localInfrastructure.RabbitMqPort);
    }

    [Theory]
    [InlineData("Profile", "Profile")]
    [InlineData("OperationTimeout", "OperationTimeout")]
    [InlineData("LocalInfrastructure", "LocalInfrastructure")]
    public void MissingRequiredCheckedInSetting_IsRejected(
        string setting,
        string expectedError)
    {
        var source = Path.Combine(AppContext.BaseDirectory, TestConfigurationProvider.SettingsFileName);
        var document = JsonNode.Parse(File.ReadAllText(source))?.AsObject()
            ?? throw new InvalidOperationException($"Could not parse {source}.");

        Assert.True(document.Remove(setting), $"expected {setting} in {source}");

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(document.ToJsonString()));
        var configuration = new ConfigurationBuilder().AddJsonStream(stream).Build();
        var options = TestConfigurationProvider.Bind(configuration);

        Assert.Contains(expectedError, options.ValidateFor());
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
        var localInfrastructure = Assert.IsType<LocalInfrastructureOptions>(options.LocalInfrastructure);

        Assert.Equal("broker.internal", localInfrastructure.RabbitMqHost);
        Assert.Equal(5673, localInfrastructure.RabbitMqPort);
        Assert.Equal("localhost", localInfrastructure.PostgreSqlHost);
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
        var userSecrets = new[]
        {
            new KeyValuePair<string, string?>("Profile", "LocalIntegration"),
            new KeyValuePair<string, string?>("OperationTimeout", "00:00:45"),
        };
        var provider = new TestConfigurationProvider(
            AppContext.BaseDirectory,
            [new KeyValuePair<string, string?>("VICIONE_TESTS__Profile", "External")],
            userSecrets);

        var options = provider.GetOptions();

        Assert.Equal(TestProfile.External, options.Profile);
        Assert.Equal(TimeSpan.FromSeconds(45), options.OperationTimeout);
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
    [InlineData("VICIONE_TESTS__LocalInfrastructure__RabbitMqHost", "", "LocalInfrastructure:RabbitMqHost")]
    [InlineData("VICIONE_TESTS__LocalInfrastructure__RabbitMqPort", "0", "LocalInfrastructure:RabbitMqPort")]
    [InlineData("VICIONE_TESTS__LocalInfrastructure__RabbitMqPort", "65536", "LocalInfrastructure:RabbitMqPort")]
    [InlineData("VICIONE_TESTS__LocalInfrastructure__PostgreSqlHost", "", "LocalInfrastructure:PostgreSqlHost")]
    [InlineData("VICIONE_TESTS__LocalInfrastructure__PostgreSqlPort", "0", "LocalInfrastructure:PostgreSqlPort")]
    [InlineData("VICIONE_TESTS__LocalInfrastructure__PostgreSqlPort", "65536", "LocalInfrastructure:PostgreSqlPort")]
    public void LocalIntegrationProfile_RejectsAnInvalidEndpoint(
        string key,
        string value,
        string expectedError)
    {
        var options = ProviderWith(
            ("VICIONE_TESTS__Profile", "LocalIntegration"),
            (key, value)).GetOptions();

        Assert.Contains(expectedError, options.ValidateFor());
    }

    [Theory]
    [InlineData("VICIONE_TESTS__LocalInfrastructure__RabbitMqPort", "1", "LocalInfrastructure:RabbitMqPort")]
    [InlineData("VICIONE_TESTS__LocalInfrastructure__RabbitMqPort", "65535", "LocalInfrastructure:RabbitMqPort")]
    [InlineData("VICIONE_TESTS__LocalInfrastructure__PostgreSqlPort", "1", "LocalInfrastructure:PostgreSqlPort")]
    [InlineData("VICIONE_TESTS__LocalInfrastructure__PostgreSqlPort", "65535", "LocalInfrastructure:PostgreSqlPort")]
    public void LocalIntegrationProfile_AcceptsPortBoundaries(
        string key,
        string value,
        string errorKey)
    {
        var options = ProviderWith(
            ("VICIONE_TESTS__Profile", "LocalIntegration"),
            (key, value)).GetOptions();

        Assert.DoesNotContain(errorKey, options.ValidateFor());
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
        var options = ProviderWith(
            ("VICIONE_TESTS__Profile", "External"),
            ($"VICIONE_TESTS__ExternalProviders__{provider}__Mode", "Emulator")).GetOptions();

        var errors = options.ValidateFor(provider);

        Assert.Contains($"ExternalProviders:{provider}:Mode", errors);
    }

    [Fact]
    public void ExternalProfile_RejectsANullSelectedProviderGroup()
    {
        var options = ProviderWith(
            ("VICIONE_TESTS__Profile", "External"),
            ("VICIONE_TESTS__ExternalProviders__Azure__Mode", "Emulator")).GetOptions();
        options.ExternalProviders!.Azure = null;

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
            ["ExternalProviders", "LocalInfrastructure", "OperationTimeout", "Profile"],
            PublicPropertyNames<ViciOneTestOptions>());
        Assert.Equal(
            ["PostgreSqlHost", "PostgreSqlPort", "RabbitMqHost", "RabbitMqPort"],
            PublicPropertyNames<LocalInfrastructureOptions>());
        Assert.Equal(
            ["Aws", "Azure"],
            PublicPropertyNames<ExternalProviderOptions>());
        Assert.Equal(
            ["Location", "Mode", "Name", "ResourceGroup", "ResourceNamePrefix", "SubscriptionId"],
            PublicPropertyNames<AzureProviderOptions>());
        Assert.Equal(
            ["Mode", "Name", "Region", "ResourceNamePrefix"],
            PublicPropertyNames<AwsProviderOptions>());
        Assert.Empty(PublicFields<ViciOneTestOptions>());
        Assert.Empty(PublicFields<LocalInfrastructureOptions>());
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
