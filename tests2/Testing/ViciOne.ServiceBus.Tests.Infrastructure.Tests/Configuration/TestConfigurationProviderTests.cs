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
            environment
                .GroupBy(entry => entry.Key, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.Last())
                .Select(entry => new KeyValuePair<string, string?>(entry.Key, entry.Value)));

    [Fact]
    public void CheckedInDefaults_AreBoundOntoTypedOptions()
    {
        var options = ProviderWith().GetOptions();
        var localInfrastructure = Assert.IsType<LocalInfrastructureOptions>(options.LocalInfrastructure);

        Assert.Equal(TestProfile.UnitArchitecture, options.Profile);
        Assert.Equal(TimeSpan.FromSeconds(30), options.OperationTimeout);
        RabbitMqLocalOptions rabbitMq = Assert.IsType<RabbitMqLocalOptions>(localInfrastructure.RabbitMq);
        ActiveMqLocalOptions activeMq = Assert.IsType<ActiveMqLocalOptions>(localInfrastructure.ActiveMq);
        ArtemisLocalOptions artemis = Assert.IsType<ArtemisLocalOptions>(localInfrastructure.Artemis);
        PostgreSqlLocalOptions postgreSql = Assert.IsType<PostgreSqlLocalOptions>(localInfrastructure.PostgreSql);
        SqlServerLocalOptions sqlServer = Assert.IsType<SqlServerLocalOptions>(localInfrastructure.SqlServer);
        AzureTableLocalOptions azureTable = Assert.IsType<AzureTableLocalOptions>(localInfrastructure.AzureTable);
        LocalStackLocalOptions localStack = Assert.IsType<LocalStackLocalOptions>(localInfrastructure.LocalStack);
        Assert.Equal("localhost", rabbitMq.Host);
        Assert.Equal(5672, rabbitMq.Port);
        Assert.Equal("localhost", activeMq.Host);
        Assert.Equal(61616, activeMq.OpenWirePort);
        Assert.Equal(5672, activeMq.AmqpPort);
        Assert.Equal(8161, activeMq.JolokiaPort);
        Assert.Equal("localhost", artemis.Host);
        Assert.Equal(61616, artemis.Port);
        Assert.Equal(8161, artemis.JolokiaPort);
        Assert.Equal("localhost", postgreSql.Host);
        Assert.Equal(5432, postgreSql.Port);
        Assert.Equal("postgres", postgreSql.Database);
        Assert.Equal("localhost", sqlServer.Host);
        Assert.Equal(1433, sqlServer.Port);
        Assert.Equal("master", sqlServer.Database);
        Assert.Equal("localhost", azureTable.Host);
        Assert.Equal(10002, azureTable.Port);
        Assert.Equal("localhost", localStack.Host);
        Assert.Equal(4566, localStack.Port);
        Assert.Equal("eu-central-1", localStack.Region);
        Assert.Equal("000000000000", localStack.AccountId);
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
            ("VICIONE_TESTS__LocalInfrastructure__RabbitMq__Host", "broker.internal"),
            ("VICIONE_TESTS__LocalInfrastructure__RabbitMq__Port", "5673")).GetOptions();
        var localInfrastructure = Assert.IsType<LocalInfrastructureOptions>(options.LocalInfrastructure);
        RabbitMqLocalOptions rabbitMq = Assert.IsType<RabbitMqLocalOptions>(localInfrastructure.RabbitMq);
        PostgreSqlLocalOptions postgreSql = Assert.IsType<PostgreSqlLocalOptions>(localInfrastructure.PostgreSql);

        Assert.Equal("broker.internal", rabbitMq.Host);
        Assert.Equal(5673, rabbitMq.Port);
        Assert.Equal("localhost", postgreSql.Host);
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
    [InlineData("VICIONE_TESTS__LocalInfrastructure__PostgreSql__Host", "", "LocalInfrastructure:PostgreSql:Host")]
    [InlineData("VICIONE_TESTS__LocalInfrastructure__PostgreSql__Port", "0", "LocalInfrastructure:PostgreSql:Port")]
    [InlineData("VICIONE_TESTS__LocalInfrastructure__PostgreSql__Port", "65536", "LocalInfrastructure:PostgreSql:Port")]
    [InlineData("VICIONE_TESTS__LocalInfrastructure__PostgreSql__Database", "", "LocalInfrastructure:PostgreSql:Database")]
    [InlineData("VICIONE_TESTS__LocalInfrastructure__PostgreSql__UserName", "", "LocalInfrastructure:PostgreSql:UserName")]
    [InlineData("VICIONE_TESTS__LocalInfrastructure__PostgreSql__Password", "", "LocalInfrastructure:PostgreSql:Password")]
    public void LocalPostgreSqlSelection_RejectsAnInvalidSetting(
        string key,
        string value,
        string expectedError)
    {
        var options = ProviderWith(
            ("VICIONE_TESTS__Profile", "LocalIntegration"),
            ("VICIONE_TESTS__LocalInfrastructure__PostgreSql__UserName", "run-user"),
            ("VICIONE_TESTS__LocalInfrastructure__PostgreSql__Password", "run-secret"),
            (key, value)).GetOptions();

        Assert.Contains(expectedError, options.ValidateForLocal(LocalTestResource.PostgreSql));
    }

    [Theory]
    [InlineData("1")]
    [InlineData("65535")]
    public void LocalPostgreSqlSelection_AcceptsPortBoundaries(string port)
    {
        var options = ProviderWith(
            ("VICIONE_TESTS__Profile", "LocalIntegration"),
            ("VICIONE_TESTS__LocalInfrastructure__PostgreSql__Port", port),
            ("VICIONE_TESTS__LocalInfrastructure__PostgreSql__UserName", "run-user"),
            ("VICIONE_TESTS__LocalInfrastructure__PostgreSql__Password", "run-secret")).GetOptions();

        Assert.DoesNotContain(
            "LocalInfrastructure:PostgreSql:Port",
            options.ValidateForLocal(LocalTestResource.PostgreSql));
    }

    [Theory]
    [InlineData("VICIONE_TESTS__LocalInfrastructure__SqlServer__Host", "", "LocalInfrastructure:SqlServer:Host")]
    [InlineData("VICIONE_TESTS__LocalInfrastructure__SqlServer__Port", "0", "LocalInfrastructure:SqlServer:Port")]
    [InlineData("VICIONE_TESTS__LocalInfrastructure__SqlServer__Port", "65536", "LocalInfrastructure:SqlServer:Port")]
    [InlineData("VICIONE_TESTS__LocalInfrastructure__SqlServer__Database", "", "LocalInfrastructure:SqlServer:Database")]
    [InlineData("VICIONE_TESTS__LocalInfrastructure__SqlServer__UserName", "", "LocalInfrastructure:SqlServer:UserName")]
    [InlineData("VICIONE_TESTS__LocalInfrastructure__SqlServer__Password", "", "LocalInfrastructure:SqlServer:Password")]
    public void LocalSqlServerSelection_RejectsAnInvalidSetting(
        string key,
        string value,
        string expectedError)
    {
        var options = ProviderWith(
            ("VICIONE_TESTS__Profile", "LocalIntegration"),
            ("VICIONE_TESTS__LocalInfrastructure__SqlServer__UserName", "run-user"),
            ("VICIONE_TESTS__LocalInfrastructure__SqlServer__Password", "run-secret"),
            (key, value)).GetOptions();

        Assert.Contains(expectedError, options.ValidateForLocal(LocalTestResource.SqlServer));
    }

    [Theory]
    [InlineData("1")]
    [InlineData("65535")]
    public void LocalSqlServerSelection_AcceptsPortBoundaries(string port)
    {
        var options = ProviderWith(
            ("VICIONE_TESTS__Profile", "LocalIntegration"),
            ("VICIONE_TESTS__LocalInfrastructure__SqlServer__Port", port),
            ("VICIONE_TESTS__LocalInfrastructure__SqlServer__UserName", "run-user"),
            ("VICIONE_TESTS__LocalInfrastructure__SqlServer__Password", "run-secret")).GetOptions();

        Assert.DoesNotContain(
            "LocalInfrastructure:SqlServer:Port",
            options.ValidateForLocal(LocalTestResource.SqlServer));
    }

    [Theory]
    [InlineData("VICIONE_TESTS__LocalInfrastructure__LocalStack__Host", "", "LocalInfrastructure:LocalStack:Host")]
    [InlineData("VICIONE_TESTS__LocalInfrastructure__LocalStack__Port", "0", "LocalInfrastructure:LocalStack:Port")]
    [InlineData("VICIONE_TESTS__LocalInfrastructure__LocalStack__Region", "", "LocalInfrastructure:LocalStack:Region")]
    [InlineData("VICIONE_TESTS__LocalInfrastructure__LocalStack__AccountId", "not-an-account", "LocalInfrastructure:LocalStack:AccountId")]
    public void LocalStackSelection_RejectsAnInvalidCoordinate(
        string key,
        string value,
        string expectedError)
    {
        var options = ProviderWith(
            ("VICIONE_TESTS__Profile", "LocalIntegration"),
            (key, value)).GetOptions();

        Assert.Contains(expectedError, options.ValidateForLocal(LocalTestResource.LocalStack));
    }

    [Theory]
    [InlineData("VICIONE_SERVICEBUS_LOCALSTACK_HOST", "127.0.0.1", "127.0.0.1")]
    [InlineData("VICIONE_SERVICEBUS_LOCALSTACK_PORT", "34567", "34567")]
    [InlineData("VICIONE_SERVICEBUS_LOCALSTACK_REGION", "eu-west-1", "eu-west-1")]
    [InlineData("VICIONE_SERVICEBUS_LOCALSTACK_ACCOUNT_ID", "123456789012", "123456789012")]
    public void CanonicalLocalStackVariables_MapIntoTheSingleTypedConfiguration(
        string key,
        string value,
        string expected)
    {
        LocalStackLocalOptions localStack = Assert.IsType<LocalStackLocalOptions>(
            ProviderWith((key, value)).GetOptions().LocalInfrastructure?.LocalStack);

        string actual = key switch
        {
            "VICIONE_SERVICEBUS_LOCALSTACK_HOST" => localStack.Host!,
            "VICIONE_SERVICEBUS_LOCALSTACK_PORT" => localStack.Port!.Value.ToString(),
            "VICIONE_SERVICEBUS_LOCALSTACK_REGION" => localStack.Region!,
            "VICIONE_SERVICEBUS_LOCALSTACK_ACCOUNT_ID" => localStack.AccountId!,
            _ => throw new InvalidOperationException(key),
        };

        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("VICIONE_SERVICEBUS_AMQ_HOST", "127.0.0.1", "127.0.0.1")]
    [InlineData("VICIONE_SERVICEBUS_AMQ_OPENWIRE_PORT", "36161", "36161")]
    [InlineData("VICIONE_SERVICEBUS_AMQ_AMQP_PORT", "35672", "35672")]
    [InlineData("VICIONE_SERVICEBUS_AMQ_JOLOKIA_PORT", "38161", "38161")]
    [InlineData("VICIONE_SERVICEBUS_AMQ_USER", "run-user", "run-user")]
    [InlineData("VICIONE_SERVICEBUS_AMQ_PASS", "run-secret", "run-secret")]
    public void CanonicalActiveMqVariables_MapIntoTheSingleTypedConfiguration(
        string key,
        string value,
        string expected)
    {
        ActiveMqLocalOptions activeMq = Assert.IsType<ActiveMqLocalOptions>(
            ProviderWith((key, value)).GetOptions().LocalInfrastructure?.ActiveMq);

        string actual = key switch
        {
            "VICIONE_SERVICEBUS_AMQ_HOST" => activeMq.Host!,
            "VICIONE_SERVICEBUS_AMQ_OPENWIRE_PORT" => activeMq.OpenWirePort!.Value.ToString(),
            "VICIONE_SERVICEBUS_AMQ_AMQP_PORT" => activeMq.AmqpPort!.Value.ToString(),
            "VICIONE_SERVICEBUS_AMQ_JOLOKIA_PORT" => activeMq.JolokiaPort!.Value.ToString(),
            "VICIONE_SERVICEBUS_AMQ_USER" => activeMq.UserName!,
            "VICIONE_SERVICEBUS_AMQ_PASS" => activeMq.Password!,
            _ => throw new InvalidOperationException(key),
        };

        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("VICIONE_SERVICEBUS_ARTEMIS_HOST", "127.0.0.1", "127.0.0.1")]
    [InlineData("VICIONE_SERVICEBUS_ARTEMIS_PORT", "46161", "46161")]
    [InlineData("VICIONE_SERVICEBUS_ARTEMIS_JOLOKIA_PORT", "48161", "48161")]
    [InlineData("VICIONE_SERVICEBUS_ARTEMIS_USER", "run-user", "run-user")]
    [InlineData("VICIONE_SERVICEBUS_ARTEMIS_PASS", "run-secret", "run-secret")]
    public void CanonicalArtemisVariables_MapIntoTheSingleTypedConfiguration(
        string key,
        string value,
        string expected)
    {
        ArtemisLocalOptions artemis = Assert.IsType<ArtemisLocalOptions>(
            ProviderWith((key, value)).GetOptions().LocalInfrastructure?.Artemis);

        string actual = key switch
        {
            "VICIONE_SERVICEBUS_ARTEMIS_HOST" => artemis.Host!,
            "VICIONE_SERVICEBUS_ARTEMIS_PORT" => artemis.Port!.Value.ToString(),
            "VICIONE_SERVICEBUS_ARTEMIS_JOLOKIA_PORT" => artemis.JolokiaPort!.Value.ToString(),
            "VICIONE_SERVICEBUS_ARTEMIS_USER" => artemis.UserName!,
            "VICIONE_SERVICEBUS_ARTEMIS_PASS" => artemis.Password!,
            _ => throw new InvalidOperationException(key),
        };

        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(LocalTestResource.ActiveMq, "LocalInfrastructure:ActiveMq:UserName")]
    [InlineData(LocalTestResource.ActiveMq, "LocalInfrastructure:ActiveMq:Password")]
    [InlineData(LocalTestResource.Artemis, "LocalInfrastructure:Artemis:UserName")]
    [InlineData(LocalTestResource.Artemis, "LocalInfrastructure:Artemis:Password")]
    public void BrokerSelection_RejectsMissingRunCredentials(
        LocalTestResource resource,
        string expectedError)
    {
        ViciOneTestOptions options = ProviderWith(
            ("VICIONE_TESTS__Profile", "LocalIntegration")).GetOptions();

        Assert.Contains(expectedError, options.ValidateForLocal(resource));
    }

    [Theory]
    [InlineData("VICIONE_SERVICEBUS_PG_HOST", "db.internal", "db.internal")]
    [InlineData("VICIONE_SERVICEBUS_PG_PORT", "55432", "55432")]
    [InlineData("VICIONE_SERVICEBUS_PG_DATABASE", "journal", "journal")]
    [InlineData("VICIONE_SERVICEBUS_PG_USER", "run-user", "run-user")]
    [InlineData("VICIONE_SERVICEBUS_PG_PASS", "run-secret", "run-secret")]
    public void CanonicalFixtureVariables_MapIntoTheSingleTypedConfiguration(
        string key,
        string value,
        string expected)
    {
        PostgreSqlLocalOptions postgreSql = Assert.IsType<PostgreSqlLocalOptions>(
            ProviderWith((key, value)).GetOptions().LocalInfrastructure?.PostgreSql);

        string actual = key switch
        {
            "VICIONE_SERVICEBUS_PG_HOST" => postgreSql.Host!,
            "VICIONE_SERVICEBUS_PG_PORT" => postgreSql.Port!.Value.ToString(),
            "VICIONE_SERVICEBUS_PG_DATABASE" => postgreSql.Database!,
            "VICIONE_SERVICEBUS_PG_USER" => postgreSql.UserName!,
            "VICIONE_SERVICEBUS_PG_PASS" => postgreSql.Password!,
            _ => throw new InvalidOperationException(key),
        };
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("VICIONE_SERVICEBUS_MSSQL_HOST", "db.internal", "db.internal")]
    [InlineData("VICIONE_SERVICEBUS_MSSQL_PORT", "31433", "31433")]
    [InlineData("VICIONE_SERVICEBUS_MSSQL_DATABASE", "master", "master")]
    [InlineData("VICIONE_SERVICEBUS_MSSQL_USER", "run-user", "run-user")]
    [InlineData("VICIONE_SERVICEBUS_MSSQL_PASS", "run-secret", "run-secret")]
    public void CanonicalSqlServerVariables_MapIntoTheSingleTypedConfiguration(
        string key,
        string value,
        string expected)
    {
        SqlServerLocalOptions sqlServer = Assert.IsType<SqlServerLocalOptions>(
            ProviderWith((key, value)).GetOptions().LocalInfrastructure?.SqlServer);

        string actual = key switch
        {
            "VICIONE_SERVICEBUS_MSSQL_HOST" => sqlServer.Host!,
            "VICIONE_SERVICEBUS_MSSQL_PORT" => sqlServer.Port!.Value.ToString(),
            "VICIONE_SERVICEBUS_MSSQL_DATABASE" => sqlServer.Database!,
            "VICIONE_SERVICEBUS_MSSQL_USER" => sqlServer.UserName!,
            "VICIONE_SERVICEBUS_MSSQL_PASS" => sqlServer.Password!,
            _ => throw new InvalidOperationException(key),
        };
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ExplicitPrefixedSetting_OverridesTheCanonicalFixtureProjection()
    {
        PostgreSqlLocalOptions postgreSql = Assert.IsType<PostgreSqlLocalOptions>(ProviderWith(
            ("VICIONE_SERVICEBUS_PG_HOST", "fixture.internal"),
            ("VICIONE_TESTS__LocalInfrastructure__PostgreSql__Host", "explicit.internal"))
            .GetOptions().LocalInfrastructure?.PostgreSql);

        Assert.Equal("explicit.internal", postgreSql.Host);
    }

    [Fact]
    public void LocalSelection_RequiresTheMatchingProfileAndAResource()
    {
        ViciOneTestOptions unit = ProviderWith().GetOptions();
        ViciOneTestOptions local = ProviderWith(("VICIONE_TESTS__Profile", "LocalIntegration")).GetOptions();

        Assert.Contains(nameof(ViciOneTestOptions.Profile), unit.ValidateForLocal(LocalTestResource.PostgreSql));
        Assert.Contains("LocalInfrastructure:Selection", local.ValidateForLocal());
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
            ["ActiveMq", "Artemis", "AzureTable", "LocalStack", "PostgreSql", "RabbitMq", "SqlServer"],
            PublicPropertyNames<LocalInfrastructureOptions>());
        Assert.Equal(
            ["Host", "Password", "Port", "UserName"],
            PublicPropertyNames<RabbitMqLocalOptions>());
        Assert.Equal(
            ["AmqpPort", "Host", "JolokiaPort", "OpenWirePort", "Password", "UserName"],
            PublicPropertyNames<ActiveMqLocalOptions>());
        Assert.Equal(
            ["Host", "JolokiaPort", "Password", "Port", "UserName"],
            PublicPropertyNames<ArtemisLocalOptions>());
        Assert.Equal(
            ["Database", "Host", "Password", "Port", "UserName"],
            PublicPropertyNames<PostgreSqlLocalOptions>());
        Assert.Equal(
            ["Database", "Host", "Password", "Port", "UserName"],
            PublicPropertyNames<SqlServerLocalOptions>());
        Assert.Equal(
            ["AccountKey", "AccountName", "Host", "Port"],
            PublicPropertyNames<AzureTableLocalOptions>());
        Assert.Equal(
            ["AccountId", "Host", "Port", "Region"],
            PublicPropertyNames<LocalStackLocalOptions>());
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
        Assert.Empty(PublicFields<RabbitMqLocalOptions>());
        Assert.Empty(PublicFields<ActiveMqLocalOptions>());
        Assert.Empty(PublicFields<ArtemisLocalOptions>());
        Assert.Empty(PublicFields<PostgreSqlLocalOptions>());
        Assert.Empty(PublicFields<SqlServerLocalOptions>());
        Assert.Empty(PublicFields<AzureTableLocalOptions>());
        Assert.Empty(PublicFields<LocalStackLocalOptions>());
        Assert.Empty(PublicFields<AzureProviderOptions>());
        Assert.Empty(PublicFields<AwsProviderOptions>());
        Assert.Empty(PublicFields<ExternalProviderOptions>());
    }

    [Fact]
    public void GetValidatedLocalOptions_FailsBeforeExecutionWithoutCredentials()
    {
        var provider = ProviderWith(("VICIONE_TESTS__Profile", "LocalIntegration"));

        var failure = Assert.Throws<InvalidOperationException>(
            () => provider.GetValidatedLocalOptions(LocalTestResource.PostgreSql));

        Assert.Contains("LocalInfrastructure:PostgreSql:UserName", failure.Message);
        Assert.Contains("LocalInfrastructure:PostgreSql:Password", failure.Message);
        Assert.Contains("Never commit credentials", failure.Message);
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
