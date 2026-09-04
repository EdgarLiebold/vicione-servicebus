using System.Security.Cryptography;
using System.Text;

namespace ViciOne.ServiceBus.Tests.Infrastructure.Databases;
/// <summary>Creates provider-safe database names from an explicit, run-scoped identity.</summary>
public static class TestDatabaseName
{
    private const int HashLength = 32;
    private const string RunRootEnvironmentVariable = "VICIONE_SERVICEBUS_RUN_ROOT";
    private static readonly string LocalRunIdentity = $"local-{Guid.CreateVersion7():N}";

    public static string Create(string providerPrefix, string runIdentity, int maximumLength = 63)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerPrefix);
        ArgumentException.ThrowIfNullOrWhiteSpace(runIdentity);

        if (maximumLength <= HashLength + 1)
            throw new ArgumentOutOfRangeException(nameof(maximumLength), maximumLength,
                $"The maximum length must leave room for a prefix, a separator, and {HashLength} hash characters.");

        string normalizedPrefix = NormalizePrefix(providerPrefix);
        int prefixLength = Math.Min(normalizedPrefix.Length, maximumLength - HashLength - 1);
        string identityHash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(runIdentity)));

        return $"{normalizedPrefix[..prefixLength]}_{identityHash[..HashLength]}";
    }

    /// <summary>Creates a name isolated by the current fixture run and the current test identity.</summary>
    public static string CreateForCurrentRun(
        string providerPrefix,
        string testIdentity,
        string purpose,
        int maximumLength = 63)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(testIdentity);
        ArgumentException.ThrowIfNullOrWhiteSpace(purpose);

        string? runRoot = Environment.GetEnvironmentVariable(RunRootEnvironmentVariable);
        string runIdentity = string.IsNullOrWhiteSpace(runRoot)
            ? LocalRunIdentity
            : Path.GetFileName(Path.TrimEndingDirectorySeparator(runRoot));

        if (string.IsNullOrWhiteSpace(runIdentity))
            runIdentity = LocalRunIdentity;

        return Create(providerPrefix, $"{runIdentity}\u001f{testIdentity}\u001f{purpose}", maximumLength);
    }

    private static string NormalizePrefix(string prefix)
    {
        string normalized = new(prefix
            .Where(character => char.IsAsciiLetterOrDigit(character))
            .Select(char.ToLowerInvariant)
            .ToArray());

        return normalized.Length > 0
            ? normalized
            : throw new ArgumentException("The provider prefix must contain at least one ASCII letter or digit.", nameof(prefix));
    }
}
