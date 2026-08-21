namespace ViciOne.ServiceBus.Testing.Configuration;

/// <summary>
/// The typed view of the test environment, and the only place where configuration key names exist.
/// </summary>
/// <remarks>
/// Tests receive this object. They never name a configuration key themselves, because a free-form
/// key is a name nobody validates: it can be misspelled, renamed on one side only, or invented by a
/// test that then silently reads nothing. Binding through properties means the compiler owns the
/// names.
/// </remarks>
public sealed class ViciOneTestOptions
{
    /// <summary>Name of the execution profile this run belongs to.</summary>
    public string Profile { get; set; } = TestProfiles.UnitArchitecture;

    /// <summary>Upper bound for a single test operation.</summary>
    public TimeSpan OperationTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>Endpoints of locally hosted infrastructure. Never carries credentials.</summary>
    public LocalInfrastructureOptions LocalInfrastructure { get; set; } = new();

    /// <summary>
    /// Non-secret provider configuration for real external services.
    /// </summary>
    /// <remarks>
    /// Deliberately carries no credential. Azure authenticates through the official
    /// <c>Azure.Identity</c> chain and AWS through the official AWS SDK provider chain, including
    /// short-lived workload and OIDC identities in CI and the supported developer stores locally.
    /// Copying a long-lived key into this model would replace those chains with a ViciOne-specific
    /// one and would put a durable secret into a place that is meant to be checkable.
    /// </remarks>
    public ExternalProviderOptions ExternalProviders { get; set; } = new();

    /// <summary>
    /// Names the common profile settings that are missing or unusable.
    /// </summary>
    /// <remarks>
    /// Only what every profile needs regardless of where it runs. Credentials are deliberately not
    /// checked here: <c>External</c> is an execution profile, not a synonym for one vendor, and it
    /// will cover Azure, AWS and further real transports. Demanding one vendor's fields from every
    /// external run would fail an AWS-only run for a missing Azure tenant it never uses.
    /// </remarks>
    public IReadOnlyList<string> Validate()
    {
        var missing = new List<string>();

        if (string.IsNullOrWhiteSpace(Profile))
        {
            missing.Add(nameof(Profile));
        }

        if (OperationTimeout <= TimeSpan.Zero)
        {
            missing.Add(nameof(OperationTimeout));
        }

        return missing;
    }

    /// <summary>
    /// Names what is missing for the common profile settings plus the provider groups a run
    /// explicitly declares it needs.
    /// </summary>
    /// <remarks>
    /// The caller selects its groups through a typed accessor, so the requirement is compiler-checked
    /// and no string key is involved. A run that needs only AWS selects only AWS and is never asked
    /// for Azure resource configuration; a run that selects Azure against real resources cannot start
    /// while the resource settings it needs to provision or address them are blank. Credentials are
    /// not part of this check - they are proven by the provider's own preflight against the official
    /// chain.
    /// </remarks>
    public IReadOnlyList<string> ValidateFor(
        params Func<ExternalProviderOptions, IExternalProviderConfiguration>[] required)
    {
        ArgumentNullException.ThrowIfNull(required);

        var missing = new List<string>(Validate());

        foreach (var select in required)
        {
            var group = select(ExternalProviders);

            missing.AddRange(group.MissingSettings()
                .Select(setting => $"{nameof(ExternalProviders)}:{group.Name}:{setting}"));
        }

        return missing;
    }
}

/// <summary>The three execution profiles of the native test tree.</summary>
public static class TestProfiles
{
    /// <summary>Fast tests that need no external infrastructure.</summary>
    public const string UnitArchitecture = "UnitArchitecture";

    /// <summary>Tests against locally hosted infrastructure.</summary>
    public const string LocalIntegration = "LocalIntegration";

    /// <summary>Tests against real external services of any vendor.</summary>
    public const string External = "External";
}

/// <summary>
/// Addresses of local infrastructure. Credentials are absent on purpose: local service credentials
/// are generated per run, so there is nothing here for a checked-in file to leak.
/// </summary>
public sealed class LocalInfrastructureOptions
{
    public string RabbitMqHost { get; set; } = "localhost";

    public int RabbitMqPort { get; set; } = 5672;

    public string PostgreSqlHost { get; set; } = "localhost";

    public int PostgreSqlPort { get; set; } = 5432;
}

/// <summary>How a provider group is exercised.</summary>
public enum ExternalResourceMode
{
    /// <summary>Against a local emulator. Proves only what the emulator supports.</summary>
    Emulator = 0,

    /// <summary>Against real, short-lived cloud resources.</summary>
    Real = 1,
}

/// <summary>
/// One vendor's non-secret test configuration, able to report what it is missing.
/// </summary>
public interface IExternalProviderConfiguration
{
    /// <summary>Name of this group inside the typed configuration path.</summary>
    string Name { get; }

    /// <summary>How this provider is exercised.</summary>
    ExternalResourceMode Mode { get; }

    /// <summary>Names the non-secret settings this group needs for its mode and does not have.</summary>
    IEnumerable<string> MissingSettings();
}

/// <summary>
/// Non-secret provider configuration, one typed group per vendor.
/// </summary>
/// <remarks>
/// Nothing here is a credential. What lives here is what a test needs in order to address or
/// provision a resource - region, subscription, resource group, naming prefix, and whether the run
/// targets an emulator or real resources. The vendor SDK stays the credential owner.
/// </remarks>
public sealed class ExternalProviderOptions
{
    public AzureProviderOptions Azure { get; set; } = new();

    public AwsProviderOptions Aws { get; set; } = new();
}

/// <summary>Azure resource configuration. Identity comes from the Azure.Identity chain.</summary>
public sealed class AzureProviderOptions : IExternalProviderConfiguration
{
    /// <inheritdoc />
    public ExternalResourceMode Mode { get; set; } = ExternalResourceMode.Emulator;

    /// <summary>Subscription that carries the short-lived test resources.</summary>
    public string SubscriptionId { get; set; } = string.Empty;

    /// <summary>Resource group the run provisions into and tears down afterwards.</summary>
    public string ResourceGroup { get; set; } = string.Empty;

    /// <summary>Azure location for provisioned resources.</summary>
    public string Location { get; set; } = string.Empty;

    /// <summary>Prefix that makes a run's resources identifiable and separable.</summary>
    public string ResourceNamePrefix { get; set; } = string.Empty;

    /// <inheritdoc />
    public string Name => "Azure";

    /// <inheritdoc />
    public IEnumerable<string> MissingSettings()
    {
        // An emulator run addresses nothing in a subscription, so demanding provisioning settings
        // there would fail a run that is correctly configured for what it actually does.
        if (Mode != ExternalResourceMode.Real)
        {
            yield break;
        }

        if (string.IsNullOrWhiteSpace(SubscriptionId))
        {
            yield return nameof(SubscriptionId);
        }

        if (string.IsNullOrWhiteSpace(ResourceGroup))
        {
            yield return nameof(ResourceGroup);
        }

        if (string.IsNullOrWhiteSpace(Location))
        {
            yield return nameof(Location);
        }

        if (string.IsNullOrWhiteSpace(ResourceNamePrefix))
        {
            yield return nameof(ResourceNamePrefix);
        }
    }
}

/// <summary>AWS resource configuration. Identity comes from the AWS SDK provider chain.</summary>
public sealed class AwsProviderOptions : IExternalProviderConfiguration
{
    /// <inheritdoc />
    public ExternalResourceMode Mode { get; set; } = ExternalResourceMode.Emulator;

    /// <summary>Region the run addresses.</summary>
    public string Region { get; set; } = string.Empty;

    /// <summary>Prefix that makes a run's resources identifiable and separable.</summary>
    public string ResourceNamePrefix { get; set; } = string.Empty;

    /// <inheritdoc />
    public string Name => "Aws";

    /// <inheritdoc />
    public IEnumerable<string> MissingSettings()
    {
        if (Mode != ExternalResourceMode.Real)
        {
            yield break;
        }

        if (string.IsNullOrWhiteSpace(Region))
        {
            yield return nameof(Region);
        }

        if (string.IsNullOrWhiteSpace(ResourceNamePrefix))
        {
            yield return nameof(ResourceNamePrefix);
        }
    }
}

/// <summary>
/// The boundary a provider fixture crosses before its first external test.
/// </summary>
/// <remarks>
/// F1a defines the boundary and implements no cloud test. The contract it fixes is fail-closed:
/// an implementation proves access through the vendor's official chain and throws when it cannot.
/// Missing chain or access evidence may never be reported as a skip or as success - that is exactly
/// how an external obligation silently stops being executed while the profile still looks green.
/// </remarks>
public interface IExternalAccessPreflight
{
    /// <summary>Provider group this preflight belongs to.</summary>
    string ProviderName { get; }

    /// <summary>
    /// Proves that the official credential chain resolves and the configured resources are
    /// reachable.
    /// </summary>
    /// <exception cref="InvalidOperationException">Access could not be proven.</exception>
    Task EnsureAccessAsync(CancellationToken cancellationToken);
}
