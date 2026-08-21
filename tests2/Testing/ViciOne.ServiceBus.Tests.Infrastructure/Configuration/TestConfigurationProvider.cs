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
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(basePath);
        ArgumentNullException.ThrowIfNull(environment);

        var builder = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile(SettingsFileName, optional: false, reloadOnChange: false);

        if (includeUserSecrets)
        {
            builder.AddUserSecrets(
                typeof(TestConfigurationProvider).Assembly,
                optional: true,
                reloadOnChange: false);
        }

        builder.AddInMemoryCollection(Normalize(environment));
        _configuration = builder.Build();
    }

    public static TestConfigurationProvider ForCurrentTestRun() =>
        new(AppContext.BaseDirectory, ReadProcessEnvironment(), includeUserSecrets: true);

    public static string? SharedUserSecretsId =>
        typeof(TestConfigurationProvider).Assembly
            .GetCustomAttribute<UserSecretsIdAttribute>()?.UserSecretsId;

    public IReadOnlyList<string> SourceOrder =>
        _configuration.Providers.Select(provider => provider.GetType().Name).ToArray();

    public ViciOneTestOptions GetOptions()
    {
        var options = new ViciOneTestOptions();
        _configuration.Bind(options);
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

    private static IEnumerable<KeyValuePair<string, string?>> Normalize(
        IEnumerable<KeyValuePair<string, string?>> environment) =>
        environment
            .Where(entry => entry.Key.StartsWith(EnvironmentPrefix, StringComparison.OrdinalIgnoreCase))
            .Select(entry => new KeyValuePair<string, string?>(
                entry.Key[EnvironmentPrefix.Length..].Replace(
                    "__",
                    ConfigurationPath.KeyDelimiter,
                    StringComparison.Ordinal),
                entry.Value));

    private static IEnumerable<KeyValuePair<string, string?>> ReadProcessEnvironment()
    {
        foreach (DictionaryEntry entry in Environment.GetEnvironmentVariables())
        {
            yield return new KeyValuePair<string, string?>((string)entry.Key, entry.Value as string);
        }
    }
}
