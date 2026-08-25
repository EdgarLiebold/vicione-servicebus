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

        if (LocalInfrastructure is null)
            errors.Add(nameof(LocalInfrastructure));

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

    /// <summary>Validates only the local resources selected by one integration run.</summary>
    public IReadOnlyList<string> ValidateForLocal(params LocalTestResource[] requiredResources)
    {
        ArgumentNullException.ThrowIfNull(requiredResources);

        var errors = new List<string>();

        if (Profile != TestProfile.LocalIntegration)
        {
            errors.Add(nameof(Profile));
            return errors;
        }

        if (OperationTimeout is null || OperationTimeout <= TimeSpan.Zero)
            errors.Add(nameof(OperationTimeout));

        if (requiredResources.Length == 0)
        {
            errors.Add($"{nameof(LocalInfrastructure)}:Selection");
            return errors;
        }

        if (LocalInfrastructure is null)
        {
            errors.Add(nameof(LocalInfrastructure));
            return errors;
        }

        var selectedResources = new HashSet<LocalTestResource>();
        foreach (var resource in requiredResources)
        {
            if (!Enum.IsDefined(resource))
            {
                errors.Add($"{nameof(LocalInfrastructure)}:Selection");
                continue;
            }

            if (!selectedResources.Add(resource))
            {
                errors.Add($"{nameof(LocalInfrastructure)}:{resource}:DuplicateSelection");
                continue;
            }

            ILocalTestResourceConfiguration? configuration = resource switch
            {
                LocalTestResource.RabbitMq => LocalInfrastructure.RabbitMq,
                LocalTestResource.PostgreSql => LocalInfrastructure.PostgreSql,
                LocalTestResource.AzureTable => LocalInfrastructure.AzureTable,
                _ => throw new UnreachableException(),
            };

            if (configuration is null)
            {
                errors.Add($"{nameof(LocalInfrastructure)}:{resource}");
                continue;
            }

            errors.AddRange(configuration.MissingSettings()
                .Select(setting => $"{nameof(LocalInfrastructure)}:{resource}:{setting}"));
        }

        return errors;
    }
}

/// <summary>Run-scoped resources available to the local integration profile.</summary>
public enum LocalTestResource
{
    RabbitMq = 0,
    PostgreSql = 1,
    AzureTable = 2,
}

/// <summary>External providers currently supported by the test configuration contract.</summary>
public enum ExternalProvider
{
    Azure = 0,
    Aws = 1,
}

/// <summary>Typed local endpoints. Credentials are supplied only by User Secrets or the fixture environment.</summary>
public sealed class LocalInfrastructureOptions
{
    public RabbitMqLocalOptions? RabbitMq { get; set; }

    public PostgreSqlLocalOptions? PostgreSql { get; set; }

    public AzureTableLocalOptions? AzureTable { get; set; }
}

public interface ILocalTestResourceConfiguration
{
    IEnumerable<string> MissingSettings();
}

public sealed class RabbitMqLocalOptions : ILocalTestResourceConfiguration
{
    public string? Host { get; set; }

    public int? Port { get; set; }

    public string? UserName { get; set; }

    public string? Password { get; set; }

    public IEnumerable<string> MissingSettings()
    {
        if (string.IsNullOrWhiteSpace(Host)) yield return nameof(Host);
        if (Port is null or < 1 or > 65535) yield return nameof(Port);
        if (string.IsNullOrWhiteSpace(UserName)) yield return nameof(UserName);
        if (string.IsNullOrWhiteSpace(Password)) yield return nameof(Password);
    }
}

public sealed class PostgreSqlLocalOptions : ILocalTestResourceConfiguration
{
    public string? Host { get; set; }

    public int? Port { get; set; }

    public string? Database { get; set; }

    public string? UserName { get; set; }

    public string? Password { get; set; }

    public IEnumerable<string> MissingSettings()
    {
        if (string.IsNullOrWhiteSpace(Host)) yield return nameof(Host);
        if (Port is null or < 1 or > 65535) yield return nameof(Port);
        if (string.IsNullOrWhiteSpace(Database)) yield return nameof(Database);
        if (string.IsNullOrWhiteSpace(UserName)) yield return nameof(UserName);
        if (string.IsNullOrWhiteSpace(Password)) yield return nameof(Password);
    }
}

public sealed class AzureTableLocalOptions : ILocalTestResourceConfiguration
{
    public string? Host { get; set; }

    public int? Port { get; set; }

    public string? AccountName { get; set; }

    public string? AccountKey { get; set; }

    public IEnumerable<string> MissingSettings()
    {
        if (string.IsNullOrWhiteSpace(Host)) yield return nameof(Host);
        if (Port is null or < 1 or > 65535) yield return nameof(Port);
        if (string.IsNullOrWhiteSpace(AccountName)) yield return nameof(AccountName);
        if (string.IsNullOrWhiteSpace(AccountKey)) yield return nameof(AccountKey);
    }
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
