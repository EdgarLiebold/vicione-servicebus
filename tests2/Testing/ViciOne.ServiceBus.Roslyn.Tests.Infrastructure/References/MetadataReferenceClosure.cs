using System.Reflection;
using Microsoft.CodeAnalysis;

namespace ViciOne.ServiceBus.Tests.Infrastructure.Roslyn.References;

/// <summary>Builds the deterministic metadata-reference closure used by Roslyn fixtures.</summary>
internal static class MetadataReferenceClosure
{
    internal static IReadOnlyList<MetadataReference> Create(IReadOnlyCollection<Assembly> roots)
    {
        var locations = TrustedPlatformAssemblyLocations();
        var pending = new Queue<Assembly>(roots);
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        while (pending.TryDequeue(out var assembly))
        {
            var identity = assembly.FullName
                ?? throw new InvalidOperationException("A fixture reference assembly has no identity.");

            if (!visited.Add(identity))
            {
                continue;
            }

            if (assembly.IsDynamic || string.IsNullOrWhiteSpace(assembly.Location))
            {
                throw new InvalidOperationException(
                    $"Fixture reference '{identity}' has no stable assembly file.");
            }

            locations.Add(assembly.Location);

            foreach (var reference in assembly.GetReferencedAssemblies())
            {
                pending.Enqueue(Assembly.Load(reference));
            }
        }

        return locations
            .Order(StringComparer.Ordinal)
            .Select(path => MetadataReference.CreateFromFile(path))
            .ToArray();
    }

    private static HashSet<string> TrustedPlatformAssemblyLocations()
    {
        var value = AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string;

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(
                "TRUSTED_PLATFORM_ASSEMBLIES is unavailable; Roslyn fixtures cannot bind against the running .NET framework.");
        }

        return value
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }
}
