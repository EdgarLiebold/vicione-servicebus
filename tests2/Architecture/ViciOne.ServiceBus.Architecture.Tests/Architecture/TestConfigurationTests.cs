using ViciOne.ServiceBus.Testing.Configuration;
using Xunit;

namespace ViciOne.ServiceBus.Architecture.Tests.Architecture;

/// <summary>
/// The typed configuration owner: layering, nested binding and profile-dependent validation.
/// </summary>
/// <remarks>
/// Every case injects its environment, so no test mutates the process it runs in and two of them
/// can run beside each other without one observing the other's variable.
/// </remarks>
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

        Assert.Equal(TestProfiles.UnitArchitecture, options.Profile);
        Assert.Equal(TimeSpan.FromSeconds(30), options.OperationTimeout);
        Assert.Equal("localhost", options.LocalInfrastructure.RabbitMqHost);
        Assert.Equal(5672, options.LocalInfrastructure.RabbitMqPort);
    }

    [Fact]
    public void PrefixedEnvironmentEntry_OverridesTheCheckedInDefault()
    {
        var options = ProviderWith(("VICIONE_TESTS__Profile", TestProfiles.LocalIntegration)).GetOptions();

        Assert.Equal(TestProfiles.LocalIntegration, options.Profile);
    }

    [Fact]
    public void DoubleUnderscore_BindsToANestedOption()
    {
        // The key correction: stripping the prefix is not enough. Without translating the remaining
        // double underscore into the configuration path separator this key binds to nothing, and the
        // checked-in default silently wins while the run looks configured.
        var options = ProviderWith(("VICIONE_TESTS__LocalInfrastructure__RabbitMqHost", "broker.internal")).GetOptions();

        Assert.Equal("broker.internal", options.LocalInfrastructure.RabbitMqHost);
        // A neighbouring value must survive the override rather than be reset by it.
        Assert.Equal(5672, options.LocalInfrastructure.RabbitMqPort);
    }

    [Fact]
    public void DoubleUnderscore_BindsTheDocumentedPortVariable()
    {
        // Exactly the form the documentation tells an operator to export. The value also has to
        // survive binding as an int, so a translation that produced the wrong path would leave the
        // checked-in 5672 in place and the run would silently talk to the wrong port.
        var options = ProviderWith(("VICIONE_TESTS__LocalInfrastructure__RabbitMqPort", "5673")).GetOptions();

        Assert.Equal(5673, options.LocalInfrastructure.RabbitMqPort);
        Assert.Equal("localhost", options.LocalInfrastructure.RabbitMqHost);
    }

    [Fact]
    public void DoubleUnderscore_BindsTwoLevelsDeep()
    {
        var options = ProviderWith(("VICIONE_TESTS__ExternalProviders__Azure__SubscriptionId", "sub-1")).GetOptions();

        Assert.Equal("sub-1", options.ExternalProviders.Azure.SubscriptionId);
    }

    [Fact]
    public void UnprefixedEnvironmentEntry_IsIgnored()
    {
        // Without the prefix filter an unrelated ambient variable named Profile would steer the run.
        var options = ProviderWith(("Profile", TestProfiles.External)).GetOptions();

        Assert.Equal(TestProfiles.UnitArchitecture, options.Profile);
    }

    [Fact]
    public void Sources_AreLayeredFileThenUserSecretsThenEnvironment()
    {
        // Proven on the provider order rather than by writing into a real User Secrets store, which
        // would mutate state outside the repository and make the result machine-dependent. The
        // observable half of the same ordering is asserted by the override tests above.
        var withoutSecrets = ProviderWith().SourceOrder;
        var withSecrets = new TestConfigurationProvider(
            AppContext.BaseDirectory, [], includeUserSecrets: true).SourceOrder;

        Assert.Equal(["JsonConfigurationProvider", "MemoryConfigurationProvider"], withoutSecrets);
        Assert.Equal(
            ["JsonConfigurationProvider", "JsonConfigurationProvider", "MemoryConfigurationProvider"],
            withSecrets);
    }

    [Fact]
    public void SharedUserSecretsStore_IsTheOneDeclaredForTheTree()
    {
        // The id is declared once centrally. Reading it from the assembly that owns configuration is
        // what makes every test project resolve the same store.
        Assert.Equal("vicione-servicebus-native-tests", TestConfigurationProvider.SharedUserSecretsId);
    }

    [Fact]
    public void CommonValidation_AsksForNoProviderSettingAtAll()
    {
        // External is an execution profile, not a vendor. Common validation covers what every
        // profile needs; provider settings are only ever demanded by a run that says which provider
        // it uses.
        var external = ProviderWith(("VICIONE_TESTS__Profile", TestProfiles.External)).GetOptions();

        Assert.Empty(external.Validate());
    }

    [Fact]
    public void TypedContract_CarriesNoCredentialProperty()
    {
        // The vendor SDKs own credentials: Azure through the Azure.Identity chain, AWS through the
        // AWS SDK provider chain, both with short-lived workload or OIDC identities in CI. A secret
        // copied into this model would replace those chains with a ViciOne-specific one and would
        // put a durable credential where configuration is meant to be inspectable.
        var azure = typeof(AzureProviderOptions).GetProperties().Select(property => property.Name).ToArray();
        var aws = typeof(AwsProviderOptions).GetProperties().Select(property => property.Name).ToArray();

        foreach (var forbidden in new[] { "TenantId", "ClientId", "ClientSecret", "AccessKeyId", "SecretAccessKey", "Password", "ConnectionString" })
        {
            Assert.DoesNotContain(forbidden, azure);
            Assert.DoesNotContain(forbidden, aws);
        }
    }

    [Fact]
    public void EmulatorMode_NeedsNoProvisioningSettings()
    {
        // An emulator run addresses nothing in a subscription. Demanding provisioning settings there
        // would fail a run that is correctly configured for what it actually does.
        var options = ProviderWith(("VICIONE_TESTS__Profile", TestProfiles.External)).GetOptions();

        Assert.Equal(ExternalResourceMode.Emulator, options.ExternalProviders.Azure.Mode);
        Assert.Empty(options.ValidateFor(external => external.Azure));
    }

    [Fact]
    public void AwsOnlyRun_DoesNotRequireAzureResourceConfiguration()
    {
        // The case a blanket vendor rule gets wrong: a run that never touches Azure was failed for a
        // missing Azure setting it never uses.
        var options = ProviderWith(
            ("VICIONE_TESTS__Profile", TestProfiles.External),
            ("VICIONE_TESTS__ExternalProviders__Aws__Mode", "Real"),
            ("VICIONE_TESTS__ExternalProviders__Aws__Region", "eu-central-1"),
            ("VICIONE_TESTS__ExternalProviders__Aws__ResourceNamePrefix", "run-1")).GetOptions();

        Assert.Empty(options.ValidateFor(external => external.Aws));
    }

    [Fact]
    public void AzureOnlyRun_DoesNotRequireAwsResourceConfiguration()
    {
        var options = ProviderWith(
            ("VICIONE_TESTS__Profile", TestProfiles.External),
            ("VICIONE_TESTS__ExternalProviders__Azure__Mode", "Real"),
            ("VICIONE_TESTS__ExternalProviders__Azure__SubscriptionId", "sub-1"),
            ("VICIONE_TESTS__ExternalProviders__Azure__ResourceGroup", "rg-1"),
            ("VICIONE_TESTS__ExternalProviders__Azure__Location", "westeurope"),
            ("VICIONE_TESTS__ExternalProviders__Azure__ResourceNamePrefix", "run-1")).GetOptions();

        Assert.Empty(options.ValidateFor(external => external.Azure));
    }

    [Fact]
    public void RealAzureRun_NamesEveryMissingResourceSetting()
    {
        var options = ProviderWith(
            ("VICIONE_TESTS__Profile", TestProfiles.External),
            ("VICIONE_TESTS__ExternalProviders__Azure__Mode", "Real")).GetOptions();

        Assert.Equal(
            [
                "ExternalProviders:Azure:SubscriptionId",
                "ExternalProviders:Azure:ResourceGroup",
                "ExternalProviders:Azure:Location",
                "ExternalProviders:Azure:ResourceNamePrefix",
            ],
            options.ValidateFor(external => external.Azure));
    }

    [Fact]
    public void RealAzureRun_NamesOnlyWhatIsActuallyMissing()
    {
        var options = ProviderWith(
            ("VICIONE_TESTS__Profile", TestProfiles.External),
            ("VICIONE_TESTS__ExternalProviders__Azure__Mode", "Real"),
            ("VICIONE_TESTS__ExternalProviders__Azure__SubscriptionId", "sub-1"),
            ("VICIONE_TESTS__ExternalProviders__Azure__ResourceGroup", "rg-1"),
            ("VICIONE_TESTS__ExternalProviders__Azure__Location", "westeurope")).GetOptions();

        Assert.Equal(["ExternalProviders:Azure:ResourceNamePrefix"], options.ValidateFor(external => external.Azure));
    }

    [Fact]
    public void SelectingTwoGroups_ReportsBothVendorsSeparately()
    {
        var options = ProviderWith(
            ("VICIONE_TESTS__Profile", TestProfiles.External),
            ("VICIONE_TESTS__ExternalProviders__Azure__Mode", "Real"),
            ("VICIONE_TESTS__ExternalProviders__Aws__Mode", "Real")).GetOptions();

        var missing = options.ValidateFor(external => external.Azure, external => external.Aws);

        Assert.Contains("ExternalProviders:Azure:SubscriptionId", missing);
        Assert.Contains("ExternalProviders:Aws:Region", missing);
    }

    [Fact]
    public void GetValidatedOptions_FailsBeforeExecutionNamingTheMissingSettings()
    {
        var provider = ProviderWith(
            ("VICIONE_TESTS__Profile", TestProfiles.External),
            ("VICIONE_TESTS__ExternalProviders__Azure__Mode", "Real"));

        var failure = Assert.Throws<InvalidOperationException>(
            () => provider.GetValidatedOptions(external => external.Azure));

        Assert.Contains("ExternalProviders:Azure:SubscriptionId", failure.Message);
        Assert.Contains(TestProfiles.External, failure.Message);
    }

    [Fact]
    public void PreflightBoundary_IsFailClosedByContract()
    {
        // F1a fixes the boundary and implements no cloud test. What matters here is the shape: the
        // preflight reports access by completing or by throwing, so a provider cannot signal
        // "no access" as a skip or as success and quietly drop an external obligation.
        var method = typeof(IExternalAccessPreflight).GetMethod(nameof(IExternalAccessPreflight.EnsureAccessAsync));

        Assert.NotNull(method);
        Assert.Equal(typeof(Task), method!.ReturnType);
    }
}
