using System.Reflection;
using System.Runtime.Versioning;

namespace ViciOne.ServiceBus.Architecture.Tests;

/// <summary>
/// Resolves the real compiled assemblies this test project inspects.
/// </summary>
/// <remarks>
/// Every assembly is reached through a direct type anchor, never through a hand-maintained name
/// list. A hand-written catalogue is a second inventory truth: it keeps asserting after the thing it
/// names has moved, been renamed or been dropped, and the tests stay green because they are only
/// comparing a list against itself. A type anchor cannot do that - if
/// <see cref="ViciOne.ServiceBus.IBus"/> or <see cref="ViciOne.ServiceBus.Advanced.Bus"/> stops existing,
/// this file stops compiling.
/// </remarks>
internal static class ProductAssemblyFacts
{
    /// <summary>The abstractions assembly, anchored on its central bus contract.</summary>
    internal static Assembly Abstractions => typeof(IBus).Assembly;

    /// <summary>The core assembly, anchored on its bus entry point.</summary>
    internal static Assembly Core => typeof(Bus).Assembly;

    /// <summary>The framework-neutral test infrastructure assembly.</summary>
    internal static Assembly TestingInfrastructure =>
        typeof(ServiceBus.Tests.Infrastructure.Configuration.TestConfigurationProvider).Assembly;

    /// <summary>The framework-neutral Roslyn test infrastructure assembly.</summary>
    internal static Assembly RoslynTestingInfrastructure =>
        typeof(ServiceBus.Tests.Infrastructure.Roslyn.RoslynTestHost).Assembly;

    /// <summary>The analyzer-specific, assertion-framework-neutral fixture assembly.</summary>
    internal static Assembly AnalyzerTestingInfrastructure =>
        typeof(ServiceBus.Tests.Infrastructure.Analyzers.MessageContracts.MessageContractScenarioCatalog).Assembly;

    /// <summary>Every compiled support assembly anchored by architecture rules.</summary>
    internal static IReadOnlyList<Assembly> TestingInfrastructureAssemblies =>
        [TestingInfrastructure, RoslynTestingInfrastructure, AnalyzerTestingInfrastructure];

    /// <summary>This test assembly.</summary>
    internal static Assembly ArchitectureTests => typeof(ProductAssemblyFacts).Assembly;

    /// <summary>
    /// The two product assemblies currently anchored by compiled structural rules.
    /// </summary>
    /// <remarks>
    /// This is a deliberately small anchor set, not the complete product. It anchors the two
    /// assemblies every other one depends on. Calling it "all product assemblies" would be a completeness claim this set does
    /// not carry: the repository has twenty-two product projects. A rule that needs the full set
    /// must derive it from the evaluated product graph rather than from this list.
    /// </remarks>
    internal static IReadOnlyList<Assembly> ArchitectureAnchors => [Abstractions, Core];

    /// <summary>Reads the framework an assembly was actually compiled for.</summary>
    internal static string? TargetFrameworkOf(Assembly assembly) =>
        assembly.GetCustomAttribute<TargetFrameworkAttribute>()?.FrameworkName;

    /// <summary>Reads the build configuration an assembly was actually compiled in.</summary>
    internal static string? ConfigurationOf(Assembly assembly) =>
        assembly.GetCustomAttribute<AssemblyConfigurationAttribute>()?.Configuration;

    /// <summary>Simple names of everything an assembly references.</summary>
    internal static IReadOnlyList<string> ReferencedAssemblyNames(Assembly assembly) =>
        assembly.GetReferencedAssemblies()
            .Select(reference => reference.Name ?? string.Empty)
            .Where(name => name.Length > 0)
            .ToArray();
}
