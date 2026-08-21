using System.Diagnostics;

namespace ViciOne.ServiceBus.Tests.Infrastructure.Configuration;

/// <summary>The execution profile selected for a native test run.</summary>
public enum TestProfile
{
    /// <summary>Hermetic unit and architecture tests.</summary>
    UnitArchitecture = 0,

    /// <summary>Tests against infrastructure created on the local machine.</summary>
    LocalIntegration = 1,

    /// <summary>Tests against real external provider resources.</summary>
    External = 2,
}

/// <summary>The typed, secret-free configuration contract of the native test tree.</summary>
public sealed class ViciOneTestOptions
{
    public TestProfile? Profile { get; set; }

    public TimeSpan? OperationTimeout { get; set; }

    public LocalInfrastructureOptions? LocalInfrastructure { get; set; }

    /// <remarks>
    /// This contains resource coordinates only. Azure credentials remain owned by Azure.Identity;
    /// AWS credentials remain owned by the AWS SDK credential provider chain.
    /// </remarks>
    public ExternalProviderOptions? ExternalProviders { get; set; }

    /// <summary>Validates the selected profile and all explicitly selected provider groups.</summary>
    public IReadOnlyList<string> ValidateFor(params ExternalProvider[] requiredProviders)
    {
        ArgumentNullException.ThrowIfNull(requiredProviders);

        var errors = new List<string>();

        if (Profile is null || !Enum.IsDefined(Profile.Value))
        {
            errors.Add(nameof(Profile));
        }

        if (OperationTimeout is null || OperationTimeout <= TimeSpan.Zero)
        {
            errors.Add(nameof(OperationTimeout));
        }

        var localInfrastructure = LocalInfrastructure;

        if (localInfrastructure is null)
        {
            errors.Add(nameof(LocalInfrastructure));
        }
        else
        {
            ValidateLocalInfrastructure(localInfrastructure, errors);
        }

        if (Profile != TestProfile.External)
        {
            if (requiredProviders.Length > 0)
            {
                errors.Add(nameof(ExternalProviders));
            }

            return errors;
        }

        if (requiredProviders.Length == 0)
        {
            errors.Add($"{nameof(ExternalProviders)}:Selection");
            return errors;
        }

        var selectedProviders = new HashSet<ExternalProvider>();

        foreach (var provider in requiredProviders)
        {
            if (!Enum.IsDefined(provider))
            {
                errors.Add($"{nameof(ExternalProviders)}:Selection");
                continue;
            }

            if (!selectedProviders.Add(provider))
            {
                errors.Add($"{nameof(ExternalProviders)}:{provider}:DuplicateSelection");
            }
        }

        var externalProviders = ExternalProviders;

        if (externalProviders is null)
        {
            errors.Add(nameof(ExternalProviders));
            return errors;
        }

        foreach (var provider in selectedProviders)
        {
            IExternalProviderConfiguration? group = provider switch
            {
                ExternalProvider.Azure => externalProviders.Azure,
                ExternalProvider.Aws => externalProviders.Aws,
                _ => throw new UnreachableException(),
            };

            if (group is null)
            {
                errors.Add($"{nameof(ExternalProviders)}:{provider}");
                continue;
            }

            if (group.Mode is null ||
                !Enum.IsDefined(group.Mode.Value) ||
                group.Mode != ExternalResourceMode.Real)
            {
                errors.Add($"{nameof(ExternalProviders)}:{group.Name}:{nameof(group.Mode)}");
                continue;
            }

            errors.AddRange(group.MissingSettings()
                .Select(setting => $"{nameof(ExternalProviders)}:{group.Name}:{setting}"));
        }

        return errors;
    }

    private static void ValidateLocalInfrastructure(
        LocalInfrastructureOptions local,
        ICollection<string> errors)
    {
        if (string.IsNullOrWhiteSpace(local.RabbitMqHost))
        {
            errors.Add($"{nameof(LocalInfrastructure)}:{nameof(local.RabbitMqHost)}");
        }

        if (local.RabbitMqPort is null or < 1 or > 65535)
        {
            errors.Add($"{nameof(LocalInfrastructure)}:{nameof(local.RabbitMqPort)}");
        }

        if (string.IsNullOrWhiteSpace(local.PostgreSqlHost))
        {
            errors.Add($"{nameof(LocalInfrastructure)}:{nameof(local.PostgreSqlHost)}");
        }

        if (local.PostgreSqlPort is null or < 1 or > 65535)
        {
            errors.Add($"{nameof(LocalInfrastructure)}:{nameof(local.PostgreSqlPort)}");
        }
    }
}

/// <summary>External providers currently supported by the test configuration contract.</summary>
public enum ExternalProvider
{
    Azure = 0,
    Aws = 1,
}

/// <summary>Addresses of per-run local infrastructure; credentials are generated by fixtures.</summary>
public sealed class LocalInfrastructureOptions
{
    public string? RabbitMqHost { get; set; }

    public int? RabbitMqPort { get; set; }

    public string? PostgreSqlHost { get; set; }

    public int? PostgreSqlPort { get; set; }
}

/// <summary>How a provider group is exercised.</summary>
public enum ExternalResourceMode
{
    Emulator = 0,
    Real = 1,
}

/// <summary>Non-secret configuration required by one external provider.</summary>
public interface IExternalProviderConfiguration
{
    string Name { get; }

    ExternalResourceMode? Mode { get; }

    IEnumerable<string> MissingSettings();
}

/// <summary>Typed external provider groups.</summary>
public sealed class ExternalProviderOptions
{
    public AzureProviderOptions? Azure { get; set; }

    public AwsProviderOptions? Aws { get; set; }
}

/// <summary>Azure resource coordinates. Authentication uses Azure.Identity.</summary>
public sealed class AzureProviderOptions : IExternalProviderConfiguration
{
    public ExternalResourceMode? Mode { get; set; }

    public string? SubscriptionId { get; set; }

    public string? ResourceGroup { get; set; }

    public string? Location { get; set; }

    public string? ResourceNamePrefix { get; set; }

    public string Name => "Azure";

    public IEnumerable<string> MissingSettings()
    {
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

/// <summary>AWS resource coordinates. Authentication uses the AWS SDK provider chain.</summary>
public sealed class AwsProviderOptions : IExternalProviderConfiguration
{
    public ExternalResourceMode? Mode { get; set; }

    public string? Region { get; set; }

    public string? ResourceNamePrefix { get; set; }

    public string Name => "Aws";

    public IEnumerable<string> MissingSettings()
    {
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
