using Azure;
using Azure.Identity;
using ViciOne.ServiceBus.AzureServiceBus.Topology;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.AzureServiceBus.Tests;

public sealed class ServiceBusHostAndNameValidationTests
{
    private const string SharedAccessConnectionString =
        "Endpoint=sb://my-endpoint.servicebus.windows.net/;SharedAccessKeyName=owner;SharedAccessKey=c2VjcmV0";

    [Theory]
    [InlineData("ConnectionString", "NamedKey")]
    [InlineData("ConnectionString", "Sas")]
    [InlineData("ConnectionString", "Token")]
    [InlineData("NamedKey", "ConnectionString")]
    [InlineData("NamedKey", "Sas")]
    [InlineData("NamedKey", "Token")]
    [InlineData("Sas", "ConnectionString")]
    [InlineData("Sas", "NamedKey")]
    [InlineData("Sas", "Token")]
    [InlineData("Token", "ConnectionString")]
    [InlineData("Token", "NamedKey")]
    [InlineData("Token", "Sas")]
    [RequirementCoverage("REQ-VSB-ASB-HOST-CONFIGURATION", "authentication-modes-remain-exclusive-without-changing-the-selected-credential")]
    public void AuthenticationModes_RejectEveryOtherModeAndPreserveTheSelectedCredential(string firstMode, string secondMode)
    {
        var configurator = new ServiceBusHostConfigurator(new Uri("sb://my-endpoint.servicebus.windows.net/"));
        object selectedCredential = SetAuthentication(configurator, firstMode);
        AssertOnlyAuthentication(configurator, firstMode, selectedCredential);

        ArgumentException exception = Assert.Throws<ArgumentException>(() => SetAuthentication(configurator, secondMode));

        Assert.Contains("Another type of authentication", exception.Message, StringComparison.Ordinal);
        AssertOnlyAuthentication(configurator, firstMode, selectedCredential);
        Assert.Equal(new Uri("sb://my-endpoint.servicebus.windows.net/"), configurator.Settings.ServiceUri);
    }

    [Theory]
    [InlineData("ConnectionString")]
    [InlineData("NamedKey")]
    [InlineData("Sas")]
    [InlineData("Token")]
    [RequirementCoverage("REQ-VSB-ASB-HOST-CONFIGURATION", "same-authentication-mode-can-replace-its-credential")]
    public void AuthenticationModes_AllowReplacingTheSelectedModeWithoutActivatingAnother(string mode)
    {
        var configurator = new ServiceBusHostConfigurator(new Uri("sb://my-endpoint.servicebus.windows.net/"));
        object original = SetAuthentication(configurator, mode);
        AssertOnlyAuthentication(configurator, mode, original);

        object replacement = SetAuthentication(configurator, mode, replacement: true);

        Assert.NotEqual(original, replacement);
        AssertOnlyAuthentication(configurator, mode, replacement);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-HOST-CONFIGURATION", "endpoint-only-connection-string-accepts-explicit-token-credential")]
    public void EndpointOnlyConnectionString_AllowsAnExplicitTokenCredential()
    {
        var configurator = new ServiceBusHostConfigurator("Endpoint=sb://my-endpoint.servicebus.windows.net/");
        var credential = new DefaultAzureCredential();

        Assert.Null(configurator.Settings.ConnectionString);
        configurator.TokenCredential = credential;

        Assert.Same(credential, configurator.Settings.TokenCredential);
        Assert.Equal(new Uri("sb://my-endpoint.servicebus.windows.net/"), configurator.Settings.ServiceUri);
        Assert.Null(configurator.Settings.NamedKeyCredential);
        Assert.Null(configurator.Settings.SasCredential);
    }

    [Theory]
    [InlineData("NamedKey")]
    [InlineData("Sas")]
    [InlineData("Token")]
    [RequirementCoverage("REQ-VSB-ASB-HOST-CONFIGURATION", "endpoint-only-setter-accepts-each-explicit-credential")]
    public void EndpointOnlyConnectionStringSetter_AllowsEachExplicitCredential(string mode)
    {
        var configurator = new ServiceBusHostConfigurator(new Uri("sb://my-endpoint.servicebus.windows.net/"));

        configurator.ConnectionString = "Endpoint=sb://my-endpoint.servicebus.windows.net/";
        Assert.Null(configurator.Settings.ConnectionString);

        object credential = SetAuthentication(configurator, mode);

        AssertOnlyAuthentication(configurator, mode, credential);
        Assert.Equal(new Uri("sb://my-endpoint.servicebus.windows.net/"), configurator.Settings.ServiceUri);
    }

    [Theory]
    [InlineData("NamedKey")]
    [InlineData("Sas")]
    [InlineData("Token")]
    [RequirementCoverage("REQ-VSB-ASB-HOST-CONFIGURATION", "endpoint-only-setter-preserves-existing-explicit-credential")]
    public void EndpointOnlyConnectionStringSetter_PreservesAnExistingExplicitCredential(string mode)
    {
        var configurator = new ServiceBusHostConfigurator(new Uri("sb://my-endpoint.servicebus.windows.net/"));
        object credential = SetAuthentication(configurator, mode);
        AssertOnlyAuthentication(configurator, mode, credential);

        configurator.ConnectionString = "Endpoint=sb://my-endpoint.servicebus.windows.net/";

        AssertOnlyAuthentication(configurator, mode, credential);
        Assert.Equal(new Uri("sb://my-endpoint.servicebus.windows.net/"), configurator.Settings.ServiceUri);
    }

    [Theory]
    [InlineData("Endpoint=sb://my-endpoint.servicebus.windows.net/;SharedAccessKeyName=owner")]
    [InlineData("Endpoint=sb://my-endpoint.servicebus.windows.net/;SharedAccessKey=c2VjcmV0")]
    [InlineData("Endpoint=sb://my-endpoint.servicebus.windows.net/;SharedAccessKeyName=owner;SharedAccessKey=c2VjcmV0;SharedAccessSignature=signature")]
    [RequirementCoverage("REQ-VSB-ASB-HOST-CONFIGURATION", "incomplete-or-mixed-shared-access-credentials-fail-before-authentication")]
    public void ConnectionString_RejectsIncompleteOrMixedSharedAccessCredentials(string connectionString)
    {
        Assert.Throws<FormatException>(() => new ServiceBusHostConfigurator(connectionString));

        var configurator = new ServiceBusHostConfigurator(new Uri("sb://my-endpoint.servicebus.windows.net/"));
        var credential = new DefaultAzureCredential();
        configurator.TokenCredential = credential;

        Assert.Throws<FormatException>(() => configurator.ConnectionString = connectionString);
        AssertOnlyAuthentication(configurator, "Token", credential);
        Assert.Equal(new Uri("sb://my-endpoint.servicebus.windows.net/"), configurator.Settings.ServiceUri);
    }

    [Theory]
    [InlineData("sb://other.servicebus.windows.net/", "sb://my-endpoint.servicebus.windows.net/scope", false)]
    [InlineData("sb://my-endpoint.servicebus.windows.net:5672/", "sb://my-endpoint.servicebus.windows.net:5671/scope", true)]
    [RequirementCoverage("REQ-VSB-ASB-HOST-CONFIGURATION", "connection-string-setter-must-match-host-and-port-without-losing-scope")]
    public void ConnectionStringSetter_RejectsAnotherNamespaceOrPortWithoutChangingTheScopedHost(
        string connectionEndpoint,
        string hostAddress,
        bool emulator)
    {
        var configurator = new ServiceBusHostConfigurator(new Uri(hostAddress));
        string connectionString = $"Endpoint={connectionEndpoint};SharedAccessKeyName=owner;SharedAccessKey=c2VjcmV0"
            + (emulator ? ";UseDevelopmentEmulator=true" : "");

        Assert.Throws<ArgumentException>(() => configurator.ConnectionString = connectionString);

        Assert.Equal(new Uri(hostAddress), configurator.Settings.ServiceUri);
        Assert.Null(configurator.Settings.ConnectionString);
    }

    [Theory]
    [InlineData("ConnectionString", null)]
    [InlineData("ConnectionString", "")]
    [InlineData("ConnectionString", "not-a-connection-string")]
    [InlineData("NamedKey", null)]
    [InlineData("NamedKey", "")]
    [InlineData("NamedKey", "not-a-connection-string")]
    [InlineData("Sas", null)]
    [InlineData("Sas", "")]
    [InlineData("Sas", "not-a-connection-string")]
    [InlineData("Token", null)]
    [InlineData("Token", "")]
    [InlineData("Token", "not-a-connection-string")]
    [RequirementCoverage("REQ-VSB-ASB-HOST-CONFIGURATION", "invalid-connection-string-setter-is-atomic-for-every-authentication-mode")]
    public void ConnectionStringSetter_InvalidInputPreservesEveryExistingAuthenticationMode(string mode, string? value)
    {
        var configurator = new ServiceBusHostConfigurator(new Uri("sb://my-endpoint.servicebus.windows.net/scope"));
        object selectedCredential = SetAuthentication(configurator, mode);
        AssertOnlyAuthentication(configurator, mode, selectedCredential);

        Exception exception = Assert.ThrowsAny<Exception>(() => configurator.ConnectionString = value!);

        Assert.True(exception is ArgumentException or FormatException, exception.ToString());
        AssertOnlyAuthentication(configurator, mode, selectedCredential);
        Assert.Equal(new Uri("sb://my-endpoint.servicebus.windows.net/scope"), configurator.Settings.ServiceUri);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-HOST-CONFIGURATION", "duplicate-endpoints-cannot-split-host-and-sdk-namespace")]
    public void DuplicateEndpoints_RejectBeforeTheHostOrSdkCanSelectDifferentNamespaces()
    {
        const string connectionString =
            "Endpoint=sb://my-endpoint.servicebus.windows.net/;Endpoint=sb://other.servicebus.windows.net/;SharedAccessKeyName=owner;SharedAccessKey=c2VjcmV0";
        var configurator = new ServiceBusHostConfigurator(new Uri("sb://my-endpoint.servicebus.windows.net/scope"));
        var credential = new DefaultAzureCredential();
        configurator.TokenCredential = credential;

        Assert.Throws<FormatException>(() => new ServiceBusHostConfigurator(connectionString));
        Assert.Throws<FormatException>(() => configurator.ConnectionString = connectionString);

        AssertOnlyAuthentication(configurator, "Token", credential);
        Assert.Equal(new Uri("sb://my-endpoint.servicebus.windows.net/scope"), configurator.Settings.ServiceUri);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-HOST-CONFIGURATION", "credential-bearing-string-requires-a-namespace-endpoint")]
    public void CredentialBearingConnectionString_RequiresAnEndpointAtConstruction()
    {
        const string connectionString = "SharedAccessKeyName=owner;SharedAccessKey=c2VjcmV0";

        Assert.Throws<FormatException>(() => new ServiceBusHostConfigurator(connectionString));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-HOST-CONFIGURATION", "entity-bound-connection-string-is-rejected-for-a-bus-host")]
    public void EntityBoundConnectionString_RejectsHostConstructionAndSetterWithoutChangingAuthentication()
    {
        const string connectionString =
            "Endpoint=sb://my-endpoint.servicebus.windows.net/;SharedAccessKeyName=owner;SharedAccessKey=c2VjcmV0;EntityPath=locked";
        var configurator = new ServiceBusHostConfigurator(new Uri("sb://my-endpoint.servicebus.windows.net/scope"));
        var credential = new DefaultAzureCredential();
        configurator.TokenCredential = credential;

        Assert.Throws<FormatException>(() => new ServiceBusHostConfigurator(connectionString));
        Assert.Throws<FormatException>(() => configurator.ConnectionString = connectionString);

        AssertOnlyAuthentication(configurator, "Token", credential);
        Assert.Equal(new Uri("sb://my-endpoint.servicebus.windows.net/scope"), configurator.Settings.ServiceUri);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-HOST-CONFIGURATION", "valid-sas-connection-string-remains-usable")]
    public void SharedAccessSignatureConnectionString_RemainsTheSelectedAuthenticationMode()
    {
        const string connectionString =
            "Endpoint=sb://my-endpoint.servicebus.windows.net/;SharedAccessSignature=SharedAccessSignature sr=unit&sig=unit&se=1";
        var configurator = new ServiceBusHostConfigurator(connectionString);

        AssertOnlyAuthentication(configurator, "ConnectionString", connectionString);
        Assert.Equal(new Uri("sb://my-endpoint.servicebus.windows.net/"), configurator.Settings.ServiceUri);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-HOST-CONFIGURATION", "emulator-connection-string-retains-matching-custom-port")]
    public void EmulatorConnectionString_AcceptsTheSameCustomPortAndPreservesScope()
    {
        const string connectionString =
            "Endpoint=sb://localhost:5672/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=c2VjcmV0;UseDevelopmentEmulator=true";
        var configurator = new ServiceBusHostConfigurator(new Uri("sb://localhost:5672/scope"));

        configurator.ConnectionString = connectionString;

        AssertOnlyAuthentication(configurator, "ConnectionString", connectionString);
        Assert.Equal(new Uri("sb://localhost:5672/scope"), configurator.Settings.ServiceUri);
    }

    [Theory]
    [InlineData("NamedKey", false)]
    [InlineData("NamedKey", true)]
    [InlineData("Sas", false)]
    [InlineData("Sas", true)]
    [InlineData("Token", false)]
    [InlineData("Token", true)]
    [RequirementCoverage("REQ-VSB-ASB-HOST-CONFIGURATION", "null-explicit-credential-cannot-enable-ambient-authentication")]
    public void ExplicitCredentialSetters_RejectNullWithoutClearingTheSelectedMode(string mode, bool alreadyConfigured)
    {
        var configurator = new ServiceBusHostConfigurator(new Uri("sb://my-endpoint.servicebus.windows.net/"));
        object? selectedCredential = alreadyConfigured ? SetAuthentication(configurator, mode) : null;

        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
        {
            switch (mode)
            {
                case "NamedKey":
                    configurator.NamedKeyCredential = null!;
                    break;
                case "Sas":
                    configurator.SasCredential = null!;
                    break;
                case "Token":
                    configurator.TokenCredential = null!;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
            }
        });

        Assert.Equal("value", exception.ParamName);
        if (alreadyConfigured)
            AssertOnlyAuthentication(configurator, mode, selectedCredential!);
        else
        {
            Assert.Null(configurator.Settings.ConnectionString);
            Assert.Null(configurator.Settings.NamedKeyCredential);
            Assert.Null(configurator.Settings.SasCredential);
            Assert.Null(configurator.Settings.TokenCredential);
        }
        Assert.Equal(new Uri("sb://my-endpoint.servicebus.windows.net/"), configurator.Settings.ServiceUri);
    }

    [Theory]
    [InlineData("localhost:5672")]
    [InlineData("localhost")]
    [RequirementCoverage("REQ-VSB-ASB-HOST-CONFIGURATION", "sdk-supported-schemaless-emulator-endpoints-are-normalized")]
    public void EmulatorConnectionString_AcceptsSdkSupportedSchemalessEndpoint(string endpoint)
    {
        string connectionString =
            $"Endpoint={endpoint};SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=c2VjcmV0;UseDevelopmentEmulator=true";
        var configurator = new ServiceBusHostConfigurator(connectionString);

        AssertOnlyAuthentication(configurator, "ConnectionString", connectionString);
        Assert.Equal(new Uri(endpoint == "localhost" ? "sb://localhost/" : "sb://localhost:5672/"), configurator.Settings.ServiceUri);
    }

    [Theory]
    [InlineData("Endpoint=localhost:5672;UseDevelopmentEmulator=true", "sb://localhost:5672/")]
    [InlineData("Endpoint=localhost;UseDevelopmentEmulator=true", "sb://localhost/")]
    [InlineData("Endpoint=sb://localhost:5672/", "sb://localhost:5672/")]
    [RequirementCoverage("REQ-VSB-ASB-HOST-CONFIGURATION", "credentialless-emulator-or-custom-port-cannot-silently-lose-transport-settings")]
    public void CredentiallessEmulatorOrCustomPort_RejectsBeforeTransportSettingsCanBeLost(string connectionString, string hostAddress)
    {
        Assert.Throws<FormatException>(() => new ServiceBusHostConfigurator(connectionString));

        var configurator = new ServiceBusHostConfigurator(new Uri(hostAddress));
        var credential = new DefaultAzureCredential();
        configurator.TokenCredential = credential;

        Assert.Throws<FormatException>(() => configurator.ConnectionString = connectionString);
        AssertOnlyAuthentication(configurator, "Token", credential);
        Assert.Equal(new Uri(hostAddress), configurator.Settings.ServiceUri);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-HOST-CONFIGURATION", "endpoint-only-setter-clears-shared-access-string-before-selecting-a-credential")]
    public void EndpointOnlyConnectionStringSetter_ClearsPreviousSharedAccessAuthentication()
    {
        var configurator = new ServiceBusHostConfigurator(new Uri("sb://my-endpoint.servicebus.windows.net/"));
        configurator.ConnectionString = SharedAccessConnectionString;
        AssertOnlyAuthentication(configurator, "ConnectionString", SharedAccessConnectionString);

        configurator.ConnectionString = "Endpoint=sb://my-endpoint.servicebus.windows.net/";

        Assert.Null(configurator.Settings.ConnectionString);
        Assert.Null(configurator.Settings.NamedKeyCredential);
        Assert.Null(configurator.Settings.SasCredential);
        Assert.Null(configurator.Settings.TokenCredential);

        var replacement = new AzureNamedKeyCredential("successor", "new");
        configurator.NamedKeyCredential = replacement;
        AssertOnlyAuthentication(configurator, "NamedKey", replacement);
    }

    [Theory]
    [InlineData("Endpoint=sb://localhost:5999/;SharedAccessKeyName=owner;SharedAccessKey=c2VjcmV0")]
    [InlineData("Endpoint=sb://localhost:5999/;SharedAccessKeyName=owner;SharedAccessKey=c2VjcmV0;UseDevelopmentEmulator=false")]
    [RequirementCoverage("REQ-VSB-ASB-HOST-CONFIGURATION", "custom-port-shared-access-string-requires-effective-emulator-mode")]
    public void CustomPortSharedAccessString_RejectsMissingOrDisabledEmulatorMode(string connectionString)
    {
        Assert.Throws<FormatException>(() => new ServiceBusHostConfigurator(connectionString));

        var configurator = new ServiceBusHostConfigurator(new Uri("sb://localhost:5999/"));
        Assert.Throws<FormatException>(() => configurator.ConnectionString = connectionString);
        Assert.Null(configurator.Settings.ConnectionString);
        Assert.Equal(new Uri("sb://localhost:5999/"), configurator.Settings.ServiceUri);
    }

    [Theory]
    [InlineData("true;UseDevelopmentEmulator=garbage", true)]
    [InlineData("true;UseDevelopmentEmulator=false", false)]
    [RequirementCoverage("REQ-VSB-ASB-HOST-CONFIGURATION", "duplicate-emulator-flag-follows-sdk-last-valid-value")]
    public void DuplicateEmulatorFlag_UsesTheLastValidBoolean(string flags, bool emulatorRemainsEnabled)
    {
        string connectionString = "Endpoint=localhost;UseDevelopmentEmulator=" + flags;

        if (emulatorRemainsEnabled)
            Assert.Throws<FormatException>(() => new ServiceBusHostConfigurator(connectionString));
        else
        {
            var configurator = new ServiceBusHostConfigurator(connectionString);
            Assert.Null(configurator.Settings.ConnectionString);
            Assert.Equal(new Uri("sb://localhost/"), configurator.Settings.ServiceUri);
        }
    }

    static object SetAuthentication(ServiceBusHostConfigurator configurator, string mode, bool replacement = false)
    {
        switch (mode)
        {
            case "ConnectionString":
                string connectionString = replacement
                    ? "Endpoint=sb://my-endpoint.servicebus.windows.net/;SharedAccessKeyName=successor;SharedAccessKey=bmV3"
                    : SharedAccessConnectionString;
                configurator.ConnectionString = connectionString;
                return connectionString;
            case "NamedKey":
                var namedKey = new AzureNamedKeyCredential(replacement ? "successor" : "owner", replacement ? "new" : "secret");
                configurator.NamedKeyCredential = namedKey;
                return namedKey;
            case "Sas":
                var sas = new AzureSasCredential(replacement
                    ? "SharedAccessSignature sr=unit&sig=successor&se=2"
                    : "SharedAccessSignature sr=unit&sig=unit&se=1");
                configurator.SasCredential = sas;
                return sas;
            case "Token":
                var token = new DefaultAzureCredential();
                configurator.TokenCredential = token;
                return token;
            default:
                throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
        }
    }

    static void AssertOnlyAuthentication(ServiceBusHostConfigurator configurator, string mode, object expected)
    {
        var settings = configurator.Settings;
        switch (mode)
        {
            case "ConnectionString":
                Assert.Equal(Assert.IsType<string>(expected), settings.ConnectionString);
                Assert.Null(settings.NamedKeyCredential);
                Assert.Null(settings.SasCredential);
                Assert.Null(settings.TokenCredential);
                break;
            case "NamedKey":
                Assert.Null(settings.ConnectionString);
                Assert.Same(expected, settings.NamedKeyCredential);
                Assert.Null(settings.SasCredential);
                Assert.Null(settings.TokenCredential);
                break;
            case "Sas":
                Assert.Null(settings.ConnectionString);
                Assert.Null(settings.NamedKeyCredential);
                Assert.Same(expected, settings.SasCredential);
                Assert.Null(settings.TokenCredential);
                break;
            case "Token":
                Assert.Null(settings.ConnectionString);
                Assert.Null(settings.NamedKeyCredential);
                Assert.Null(settings.SasCredential);
                Assert.Same(expected, settings.TokenCredential);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
        }
    }

    [Theory]
    [InlineData("Endpoint=sb://my-endpoint.servicebus.windows.net;SharedAccessKeyName=key;SharedAccessKey=value", "sb://my-endpoint.servicebus.windows.net/")]
    [InlineData("SharedAccessKeyName=key;SharedAccessKey=value;Endpoint=sb://my-endpoint.servicebus.windows.net", "sb://my-endpoint.servicebus.windows.net/")]
    [InlineData("Endpoint=sb://my-endpoint.servicebus.windows.net/someNamespace;SharedAccessKeyName=key;SharedAccessKey=value", "sb://my-endpoint.servicebus.windows.net/someNamespace")]
    [RequirementCoverage("REQ-VSB-ASB-HOST-CONFIGURATION", "connection-string-endpoint-order-and-scope")]
    public void ConnectionString_ProjectsTheExactServiceUri(string connectionString, string expected)
    {
        var configurator = new ServiceBusHostConfigurator(connectionString);

        Assert.Equal(new Uri(expected), configurator.Settings.ServiceUri);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-HOST-CONFIGURATION", "missing-endpoint-returns-null")]
    public void ParseEndpoint_EmptyConnectionStringHasNoEndpoint()
    {
        Assert.Null(ServiceBusHostConfigurator.ParseEndpoint(string.Empty));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-HOST-CONFIGURATION", "null-connection-string-is-rejected")]
    public void ParseEndpoint_RejectsANullConnectionString()
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(
            () => ServiceBusHostConfigurator.ParseEndpoint(null!));

        Assert.Equal("connectionString", exception.ParamName);
    }

    [Theory]
    [InlineData("=value")]
    [InlineData(";=value")]
    [InlineData("  =value")]
    [InlineData("Endpoint=sb://my.servicebus.windows.net;=value")]
    [InlineData("Endpoint=sb://my.servicebus.windows.net;  =value")]
    [RequirementCoverage("REQ-VSB-ASB-HOST-CONFIGURATION", "empty-connection-string-key-is-rejected")]
    public void ParseEndpoint_RejectsAnEmptyConnectionStringKey(string connectionString)
    {
        FormatException exception = Assert.Throws<FormatException>(
            () => ServiceBusHostConfigurator.ParseEndpoint(connectionString));

        Assert.Equal("Invalid connection string", exception.Message);
    }

    [Theory]
    [InlineData("Endpoint=sb://first.servicebus.windows.net/;Endpoint=sb://second.servicebus.windows.net/")]
    [InlineData("endpoint=sb://first.servicebus.windows.net/;ENDPOINT=sb://second.servicebus.windows.net/")]
    [RequirementCoverage("REQ-VSB-ASB-HOST-CONFIGURATION", "public-parser-rejects-duplicate-endpoints")]
    public void ParseEndpoint_RejectsASecondEndpointInsteadOfSelectingEitherNamespace(string connectionString)
    {
        FormatException exception = Assert.Throws<FormatException>(
            () => ServiceBusHostConfigurator.ParseEndpoint(connectionString));

        Assert.Equal("Invalid connection string: duplicate endpoint", exception.Message);
    }

    [Theory]
    [InlineData("Endpoint=sb://namespace.servicebus.windows.net/;malformed")]
    [InlineData("malformed;Endpoint=sb://namespace.servicebus.windows.net/")]
    [RequirementCoverage("REQ-VSB-ASB-HOST-CONFIGURATION", "public-parser-validates-all-segments")]
    public void ParseEndpoint_RejectsMalformedSegmentBeforeOrAfterAValidEndpoint(string connectionString)
    {
        FormatException exception = Assert.Throws<FormatException>(
            () => ServiceBusHostConfigurator.ParseEndpoint(connectionString));

        Assert.Equal("Invalid connection string", exception.Message);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-HOST-CONFIGURATION", "public-parser-preserves-delimiters-and-schemaless-scope")]
    public void ParseEndpoint_LeadingRepeatedAndTrailingDelimitersPreserveSchemalessScopedEndpoint()
    {
        const string connectionString = ";Ignored=value;;Endpoint=localhost:5672/scope;";

        Uri? endpoint = ServiceBusHostConfigurator.ParseEndpoint(connectionString);

        Assert.Equal(new Uri("sb://localhost:5672/scope"), endpoint);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-ENTITY-NAMES", "entity-and-subscription-boundaries")]
    public void EntityAndSubscriptionNames_EnforceProviderLimitsAndCharacters()
    {
        var entity = new ServiceBusEntityNameValidator();
        var subscription = new ServiceBusSubscriptionNameValidator();

        string entityAtLimit = new('e', 260);
        string subscriptionAtLimit = new('s', 50);

        Assert.True(entity.IsValidEntityName(entityAtLimit));
        Assert.True(subscription.IsValidEntityName(subscriptionAtLimit));
        entity.ThrowIfInvalidEntityName(entityAtLimit);
        subscription.ThrowIfInvalidEntityName(subscriptionAtLimit);

        ConfigurationException longEntity = Assert.Throws<ConfigurationException>(
            () => entity.ThrowIfInvalidEntityName(entityAtLimit + "x"));
        ConfigurationException longSubscription = Assert.Throws<ConfigurationException>(
            () => subscription.ThrowIfInvalidEntityName(subscriptionAtLimit + "x"));
        ConfigurationException invalidEntity = Assert.Throws<ConfigurationException>(
            () => entity.ThrowIfInvalidEntityName("invalid#entity"));
        ConfigurationException invalidSubscription = Assert.Throws<ConfigurationException>(
            () => subscription.ThrowIfInvalidEntityName("invalid/subscription"));

        Assert.Contains("260", longEntity.Message, StringComparison.Ordinal);
        Assert.Contains(entityAtLimit + "x", longEntity.Message, StringComparison.Ordinal);
        Assert.Contains("50", longSubscription.Message, StringComparison.Ordinal);
        Assert.Contains(subscriptionAtLimit + "x", longSubscription.Message, StringComparison.Ordinal);
        Assert.Contains("invalid#entity", invalidEntity.Message, StringComparison.Ordinal);
        Assert.Contains("invalid/subscription", invalidSubscription.Message, StringComparison.Ordinal);
    }
}
