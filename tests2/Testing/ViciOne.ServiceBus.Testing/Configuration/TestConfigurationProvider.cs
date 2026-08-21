using System.Collections;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.UserSecrets;

namespace ViciOne.ServiceBus.Testing.Configuration;

/// <summary>
/// The one owner of test configuration.
/// </summary>
/// <remarks>
/// Three sources, in ascending precedence: the checked-in secret-free <c>testsettings.json</c>
/// beside the test artifact, the single User Secrets store of the test tree, and the
/// <c>VICIONE_TESTS__</c> environment namespace.
/// <para>
/// This is the only type in the tree that reads process environment variables, and the only one
/// that names a configuration key. Tests receive <see cref="ViciOneTestOptions"/>; they cannot
/// invent a key, so a key cannot be misspelled on one side and silently read nothing.
/// </para>
/// <para>
/// The environment is a constructor argument rather than something read statically. That is what
/// lets a test exercise precedence and nested binding without mutating the process it runs in, so
/// two tests can run beside each other without one observing the other's variable.
/// </para>
/// </remarks>
public sealed class TestConfigurationProvider
{
    /// <summary>Prefix of every environment key this tree recognises.</summary>
    public const string EnvironmentPrefix = "VICIONE_TESTS__";

    /// <summary>Name of the checked-in, secret-free defaults file.</summary>
    public const string SettingsFileName = "testsettings.json";

    private readonly IConfigurationRoot _configuration;

    /// <summary>
    /// Builds a provider over an explicit base path and an explicit environment.
    /// </summary>
    /// <param name="basePath">Directory holding <see cref="SettingsFileName"/>.</param>
    /// <param name="environment">
    /// Environment entries to consider. Entries are interpreted exactly as the Microsoft environment
    /// provider interprets them: only keys carrying <see cref="EnvironmentPrefix"/> are read, the
    /// prefix is removed, and the remaining double underscores become the configuration path
    /// separator - so <c>VICIONE_TESTS__LocalInfrastructure__RabbitMqHost</c> binds to
    /// <c>LocalInfrastructure:RabbitMqHost</c>.
    /// </param>
    /// <param name="includeUserSecrets">
    /// Whether to layer the shared User Secrets store between the file and the environment. Tests
    /// exercising precedence switch it off so that a developer's local store cannot change what
    /// they assert.
    /// </param>
    public TestConfigurationProvider(
        string basePath,
        IEnumerable<KeyValuePair<string, string?>> environment,
        bool includeUserSecrets = false)
    {
        ArgumentNullException.ThrowIfNull(basePath);
        ArgumentNullException.ThrowIfNull(environment);

        var builder = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile(SettingsFileName, optional: false, reloadOnChange: false);

        if (includeUserSecrets)
        {
            // Always this assembly, never the entry assembly. The shared store id is declared once
            // centrally in tests2/Directory.Build.props, so resolving it from the type that owns
            // configuration gives every test project the same store rather than one store each.
            builder.AddUserSecrets(typeof(TestConfigurationProvider).Assembly, optional: true, reloadOnChange: false);
        }

        builder.AddInMemoryCollection(Normalise(environment));

        _configuration = builder.Build();
    }

    /// <summary>
    /// Builds a provider over the running test artifact, the shared User Secrets store and the real
    /// process environment.
    /// </summary>
    public static TestConfigurationProvider ForCurrentTestRun() =>
        new(AppContext.BaseDirectory, ReadProcessEnvironment(), includeUserSecrets: true);

    /// <summary>The id of the single User Secrets store this tree uses.</summary>
    public static string? SharedUserSecretsId =>
        typeof(TestConfigurationProvider).Assembly
            .GetCustomAttribute<UserSecretsIdAttribute>()?.UserSecretsId;

    /// <summary>
    /// The configuration sources in the order they are layered, lowest precedence first.
    /// </summary>
    /// <remarks>
    /// Exposed so the layering can be proven without writing into a developer's real User Secrets
    /// store. Verifying precedence by putting a value into that store would mutate shared state
    /// outside the repository and would make the result depend on the machine the suite runs on.
    /// </remarks>
    public IReadOnlyList<string> SourceOrder =>
        _configuration.Providers.Select(provider => provider.GetType().Name).ToArray();

    /// <summary>Binds the configured values onto the typed options contract.</summary>
    public ViciOneTestOptions GetOptions()
    {
        var options = new ViciOneTestOptions();
        _configuration.Bind(options);
        return options;
    }

    /// <summary>
    /// Binds the options and fails when the common profile settings, or the provider groups this
    /// run explicitly declares it needs, are incomplete. Credentials are not checked here; they are
    /// proven by the provider's own fail-closed preflight against the official vendor chain.
    /// </summary>
    /// <exception cref="InvalidOperationException">The run is incompletely configured.</exception>
    public ViciOneTestOptions GetValidatedOptions(
        params Func<ExternalProviderOptions, IExternalProviderConfiguration>[] required)
    {
        var options = GetOptions();
        var missing = options.ValidateFor(required);

        if (missing.Count > 0)
        {
            throw new InvalidOperationException(
                $"Profile '{options.Profile}' is missing required test configuration: " +
                string.Join(", ", missing) +
                $". Supply it through the shared User Secrets store or the {EnvironmentPrefix} " +
                $"environment namespace; it never belongs in {SettingsFileName}.");
        }

        return options;
    }

    /// <summary>
    /// Applies the Microsoft environment provider's own key semantics to an arbitrary environment.
    /// </summary>
    private static IEnumerable<KeyValuePair<string, string?>> Normalise(
        IEnumerable<KeyValuePair<string, string?>> environment) =>
        environment
            .Where(entry => entry.Key.StartsWith(EnvironmentPrefix, StringComparison.OrdinalIgnoreCase))
            .Select(entry => new KeyValuePair<string, string?>(
                entry.Key[EnvironmentPrefix.Length..].Replace("__", ConfigurationPath.KeyDelimiter, StringComparison.Ordinal),
                entry.Value));

    private static IEnumerable<KeyValuePair<string, string?>> ReadProcessEnvironment()
    {
        foreach (DictionaryEntry entry in Environment.GetEnvironmentVariables())
        {
            yield return new KeyValuePair<string, string?>((string)entry.Key, entry.Value as string);
        }
    }
}
