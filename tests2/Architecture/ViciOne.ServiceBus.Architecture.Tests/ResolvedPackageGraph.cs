using System.Text.Json;

namespace ViciOne.ServiceBus.Architecture.Tests;

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
            .Select(package => package.Name)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    /// <summary>
    /// Canonical NuGet identities that may never appear in the native test tree.
    /// </summary>
    /// <remarks>
    /// Every entry is the exact package id as published, because a shortened or invented identity
    /// can never match anything and would make the check look effective while proving nothing.
    /// <para>
    /// The list bans the VSTest, NUnit and MSTest adapters and the VSTest bridge, and deliberately
    /// does not ban <c>Microsoft.Testing.Platform</c> or the other <c>Microsoft.Testing.*</c>
    /// packages: those are what Microsoft Testing Platform v2 is made of, and this tree requires
    /// them. What is forbidden is a second executor or a bridge back to VSTest, not the platform
    /// itself.
    /// </para>
    /// <para>
    /// That these names are the real published ids is not asserted here - a test cannot prove the
    /// contents of nuget.org offline, and a test that claimed to would be asserting its own
    /// constant list. The sabotage evidence installs the genuine TngTech.ArchUnitNET.xUnit package
    /// and shows it is rejected; the rejection is raised by the build-level check VOSBT006, which
    /// sees the declaration first. This closure check is the second, independent layer: it covers
    /// the arrival the declaration cannot see, namely a forbidden package pulled in transitively by
    /// someone else's dependency.
    /// </para>
    /// </remarks>
    internal static IReadOnlyList<string> ForbiddenIdentities =>
    [
        "GitHubActionsTestLogger",
        "Microsoft.NET.Test.Sdk",
        "Microsoft.Testing.Extensions.VSTestBridge",
        "Microsoft.TestPlatform.ObjectModel",
        "Microsoft.TestPlatform.TestHost",
        "MSTest",
        "MSTest.TestAdapter",
        "MSTest.TestFramework",
        "NUnit",
        "NUnit.Analyzers",
        "NUnit3TestAdapter",
        "NUnitLite",
        "TngTech.ArchUnitNET.xUnit",
        "xunit.runner.visualstudio",
    ];
}
