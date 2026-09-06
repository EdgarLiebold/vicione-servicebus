using System.Text.Json;

using ViciOne.ServiceBus.Architecture.Tests.Build;

namespace ViciOne.ServiceBus.Architecture.Tests.Dependencies;

/// <summary>
/// Reads the full resolved package closure of a project from its lock file.
/// </summary>
/// <remarks>
/// <c>@(PackageReference)</c> only shows what a project declares plus what a GlobalPackageReference
/// injected; it says nothing about what restore ultimately pulled in. A forbidden runner can arrive
/// transitively through a package nobody named. The lock file is the resolved answer - it lists
/// direct and transitive entries alike - which is why absence has to be proven here and not against
/// the declaration.
/// </remarks>
internal static class ResolvedPackageGraph
{
    /// <summary>Every package in the resolved closure, direct and transitive, across all frameworks.</summary>
    internal static IReadOnlyList<string> PackagesOf(string projectPath)
    {
        var lockFile = Path.Combine(
            Path.GetDirectoryName(projectPath) ?? throw new InvalidOperationException($"No directory for {projectPath}."),
            "packages.lock.json");

        if (!File.Exists(lockFile))
        {
            throw new InvalidOperationException(
                $"No packages.lock.json beside {projectPath}. This repository restores in locked mode, so every project tracks one.");
        }

        using var document = JsonDocument.Parse(File.ReadAllBytes(lockFile));

        if (!document.RootElement.TryGetProperty("dependencies", out var dependencies))
        {
            return [];
        }

        return dependencies.EnumerateObject()
            .SelectMany(framework => framework.Value.EnumerateObject())
            .Where(dependency =>
                !dependency.Value.TryGetProperty("type", out var type) ||
                !string.Equals(type.GetString(), "Project", StringComparison.OrdinalIgnoreCase))
            .Select(package => package.Name)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    /// <summary>Central transitive entries whose resolved version differs from the declared minimum.</summary>
    internal static IReadOnlyList<string> CentralTransitiveVersionMismatchesOf(string projectPath)
    {
        string lockFile = LockFileOf(projectPath);
        using var document = JsonDocument.Parse(File.ReadAllBytes(lockFile));

        if (!document.RootElement.TryGetProperty("dependencies", out JsonElement dependencies))
            return [];

        return dependencies.EnumerateObject()
            .SelectMany(framework => framework.Value.EnumerateObject()
                .Select(dependency => (Framework: framework.Name, Package: dependency)))
            .Where(entry => entry.Package.Value.TryGetProperty("type", out JsonElement type)
                && string.Equals(type.GetString(), "CentralTransitive", StringComparison.OrdinalIgnoreCase))
            .Select(entry =>
            {
                string requested = entry.Package.Value.GetProperty("requested").GetString() ?? string.Empty;
                string resolved = entry.Package.Value.GetProperty("resolved").GetString() ?? string.Empty;
                int separator = requested.IndexOf(',', StringComparison.Ordinal);
                string minimum = requested.StartsWith("[", StringComparison.Ordinal) && separator > 1
                    ? requested[1..separator].Trim()
                    : requested;

                return (entry.Framework, entry.Package.Name, Minimum: minimum, Resolved: resolved);
            })
            .Where(entry => !string.Equals(entry.Minimum, entry.Resolved, StringComparison.OrdinalIgnoreCase))
            .Select(entry => $"{entry.Framework}:{entry.Name} requested {entry.Minimum}, resolved {entry.Resolved}")
            .OrderBy(entry => entry, StringComparer.Ordinal)
            .ToArray();
    }

    /// <summary>Reads the one effective MSBuild package-policy source for the repository test tree.</summary>
    internal static IReadOnlyList<string> ForbiddenIdentitiesOf(string projectPath) =>
        MsBuildEvaluation.ItemIdentities(projectPath, "ViciOneForbiddenNativeTestPackage")
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(identity => identity, StringComparer.OrdinalIgnoreCase)
            .ToArray();

    /// <summary>Returns whether a resolved identity introduces a forbidden test verdict path.</summary>
    internal static bool IsForbidden(string packageIdentity, IReadOnlyCollection<string> forbiddenIdentities) =>
        forbiddenIdentities.Contains(packageIdentity, StringComparer.OrdinalIgnoreCase);

    /// <summary>Returns whether a package belongs only to testing and may never enter product.</summary>
    internal static bool IsTestDependency(string packageIdentity) =>
        packageIdentity.StartsWith("xunit", StringComparison.OrdinalIgnoreCase) ||
        packageIdentity.StartsWith("nunit", StringComparison.OrdinalIgnoreCase) ||
        packageIdentity.StartsWith("MSTest", StringComparison.OrdinalIgnoreCase) ||
        packageIdentity.StartsWith("Microsoft.Testing", StringComparison.OrdinalIgnoreCase) ||
        packageIdentity.StartsWith("Microsoft.TestPlatform", StringComparison.OrdinalIgnoreCase) ||
        packageIdentity.StartsWith("TngTech.ArchUnitNET", StringComparison.OrdinalIgnoreCase) ||
        packageIdentity.Equals("GitHubActionsTestLogger", StringComparison.OrdinalIgnoreCase);

    private static string LockFileOf(string projectPath)
    {
        string lockFile = Path.Combine(
            Path.GetDirectoryName(projectPath) ?? throw new InvalidOperationException($"No directory for {projectPath}."),
            "packages.lock.json");

        if (!File.Exists(lockFile))
            throw new InvalidOperationException($"No packages.lock.json beside {projectPath}.");

        return lockFile;
    }
}
