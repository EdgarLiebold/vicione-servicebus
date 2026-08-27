using System.Collections;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.UserSecrets;

namespace ViciOne.ServiceBus.Tests.Infrastructure.Configuration;

/// <summary>Owns the single typed configuration pipeline of the native test tree.</summary>
public sealed class TestConfigurationProvider
{
    public const string EnvironmentPrefix = "VICIONE_TESTS__";
    public const string SettingsFileName = "testsettings.json";

    private readonly IConfigurationRoot _configuration;

    public TestConfigurationProvider(
        string basePath,
        IEnumerable<KeyValuePair<string, string?>> environment,
        bool includeUserSecrets = false)
        : this(
            basePath,
            environment,
            builder =>
            {
                if (includeUserSecrets)
                {
                    builder.AddUserSecrets(
                        typeof(TestConfigurationProvider).Assembly,
                        optional: true,
                        reloadOnChange: false);
                }
            })
    {
    }

    /// <summary>
    /// Supplies a deterministic in-memory equivalent of the User Secrets layer for precedence
    /// tests. It is internal so production fixtures cannot replace the one configuration pipeline.
    /// </summary>
    internal TestConfigurationProvider(
        string basePath,
        IEnumerable<KeyValuePair<string, string?>> environment,
        IEnumerable<KeyValuePair<string, string?>> userSecretValues)
        : this(
            basePath,
            environment,
            builder => builder.AddInMemoryCollection(
                userSecretValues ?? throw new ArgumentNullException(nameof(userSecretValues))))
    {
    }

    private TestConfigurationProvider(
        string basePath,
        IEnumerable<KeyValuePair<string, string?>> environment,
        Action<IConfigurationBuilder> addUserSecretsLayer)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(basePath);
        ArgumentNullException.ThrowIfNull(environment);
        ArgumentNullException.ThrowIfNull(addUserSecretsLayer);

        KeyValuePair<string, string?>[] environmentValues = environment.ToArray();
        var builder = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile(SettingsFileName, optional: false, reloadOnChange: false);

        addUserSecretsLayer(builder);
        builder.AddInMemoryCollection(NormalizeFixtureEnvironment(environmentValues));
        builder.AddInMemoryCollection(NormalizePrefixedEnvironment(environmentValues));
        _configuration = builder.Build();
    }

    public static TestConfigurationProvider ForCurrentTestRun() =>
        new(AppContext.BaseDirectory, ReadProcessEnvironment(), includeUserSecrets: true);

    public static string? SharedUserSecretsId =>
        typeof(TestConfigurationProvider).Assembly
            .GetCustomAttribute<UserSecretsIdAttribute>()?.UserSecretsId;

    public ViciOneTestOptions GetOptions()
        => Bind(_configuration);

    /// <summary>Binds one configuration graph without adding a second source pipeline.</summary>
    internal static ViciOneTestOptions Bind(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var options = new ViciOneTestOptions();
        configuration.Bind(options);
        return options;
    }

    public ViciOneTestOptions GetValidatedOptions(params ExternalProvider[] requiredProviders)
    {
        var options = GetOptions();
        var errors = options.ValidateFor(requiredProviders);

        if (errors.Count > 0)
        {
            throw new InvalidOperationException(
                $"Profile '{options.Profile}' has invalid test configuration: " +
                string.Join(", ", errors) +
                $". Put non-secret resource settings in the shared User Secrets store or the " +
                $"{EnvironmentPrefix} namespace. Credentials remain owned by Azure.Identity and " +
                $"the AWS SDK provider chains and never belong in {SettingsFileName}.");
        }

        return options;
    }

    public ViciOneTestOptions GetValidatedLocalOptions(params LocalTestResource[] requiredResources)
    {
        var options = GetOptions();
        var errors = options.ValidateForLocal(requiredResources);

        if (errors.Count > 0)
        {
            throw new InvalidOperationException(
                $"Profile '{options.Profile}' has invalid local test configuration: " +
                string.Join(", ", errors) +
                $". Put endpoint settings and credentials in the shared User Secrets store or let " +
                $"the canonical fixture runner supply them. Never commit credentials to {SettingsFileName}.");
        }

        return options;
    }

    private static IEnumerable<KeyValuePair<string, string?>> NormalizePrefixedEnvironment(
        IEnumerable<KeyValuePair<string, string?>> environment) =>
        environment
            .Where(entry => entry.Key.StartsWith(EnvironmentPrefix, StringComparison.OrdinalIgnoreCase))
            .Select(entry => new KeyValuePair<string, string?>(
                entry.Key[EnvironmentPrefix.Length..].Replace(
                    "__",
                    ConfigurationPath.KeyDelimiter,
                    StringComparison.Ordinal),
                entry.Value));

    private static IEnumerable<KeyValuePair<string, string?>> NormalizeFixtureEnvironment(
        IEnumerable<KeyValuePair<string, string?>> environment)
    {
        var mappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["VICIONE_SERVICEBUS_RMQ_HOST"] = "LocalInfrastructure:RabbitMq:Host",
            ["VICIONE_SERVICEBUS_RMQ_PORT"] = "LocalInfrastructure:RabbitMq:Port",
            ["VICIONE_SERVICEBUS_RMQ_USER"] = "LocalInfrastructure:RabbitMq:UserName",
            ["VICIONE_SERVICEBUS_RMQ_PASS"] = "LocalInfrastructure:RabbitMq:Password",
            ["VICIONE_SERVICEBUS_PG_HOST"] = "LocalInfrastructure:PostgreSql:Host",
            ["VICIONE_SERVICEBUS_PG_PORT"] = "LocalInfrastructure:PostgreSql:Port",
            ["VICIONE_SERVICEBUS_PG_DATABASE"] = "LocalInfrastructure:PostgreSql:Database",
            ["VICIONE_SERVICEBUS_PG_USER"] = "LocalInfrastructure:PostgreSql:UserName",
            ["VICIONE_SERVICEBUS_PG_PASS"] = "LocalInfrastructure:PostgreSql:Password",
            ["VICIONE_SERVICEBUS_AZURITE_HOST"] = "LocalInfrastructure:AzureTable:Host",
            ["VICIONE_SERVICEBUS_AZURITE_TABLE_PORT"] = "LocalInfrastructure:AzureTable:Port",
            ["VICIONE_SERVICEBUS_AZURITE_ACCOUNT"] = "LocalInfrastructure:AzureTable:AccountName",
            ["VICIONE_SERVICEBUS_AZURITE_KEY"] = "LocalInfrastructure:AzureTable:AccountKey",
            ["VICIONE_SERVICEBUS_LOCALSTACK_HOST"] = "LocalInfrastructure:LocalStack:Host",
            ["VICIONE_SERVICEBUS_LOCALSTACK_PORT"] = "LocalInfrastructure:LocalStack:Port",
            ["VICIONE_SERVICEBUS_LOCALSTACK_REGION"] = "LocalInfrastructure:LocalStack:Region",
            ["VICIONE_SERVICEBUS_LOCALSTACK_ACCOUNT_ID"] = "LocalInfrastructure:LocalStack:AccountId",
        };

        foreach (KeyValuePair<string, string?> entry in environment)
        {
            if (mappings.TryGetValue(entry.Key, out string? configurationKey))
                yield return new KeyValuePair<string, string?>(configurationKey, entry.Value);
        }
    }

    private static IEnumerable<KeyValuePair<string, string?>> ReadProcessEnvironment()
    {
        foreach (DictionaryEntry entry in Environment.GetEnvironmentVariables())
        {
            yield return new KeyValuePair<string, string?>((string)entry.Key, entry.Value as string);
        }
    }
}
