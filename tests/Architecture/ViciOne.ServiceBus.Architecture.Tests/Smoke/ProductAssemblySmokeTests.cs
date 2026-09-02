using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Xml.Linq;
using ViciOne.ServiceBus.Architecture.Tests.Build;
using ViciOne.ServiceBus.Architecture.Tests.Repository;
using Xunit;

namespace ViciOne.ServiceBus.Architecture.Tests.Smoke;

/// <summary>
/// Structural smoke checks over the real compiled product assemblies.
/// </summary>
/// <remarks>
/// Contract and dependency checks use the two compiled foundation assemblies reached through
/// compile-verified type anchors. The configuration check deliberately expands to every product
/// project in the UnitArchitecture solution and reads the configuration from each produced PE file.
/// Repository-wide membership and reference-closure completeness are independently owned by
/// <see cref="Repository.RepositoryGraphTests"/>.
/// </remarks>
public sealed class ProductAssemblySmokeTests
{
    [Fact]
    public void ProductAssemblies_TargetTheProductFramework()
    {
        // The framework is read off the compiled assembly, not off the project file, so a project
        // that builds for the wrong framework is caught by what it produced rather than by what it
        // declared.
        foreach (var assembly in ProductAssemblyFacts.ArchitectureAnchors)
        {
            Assert.Equal(
                ".NETCoreApp,Version=v10.0",
                ProductAssemblyFacts.TargetFrameworkOf(assembly));
        }
    }

    [Fact]
    public void ProductAssemblies_AreStrongNamed()
    {
        // The product signs its assemblies. An unsigned one would silently break every
        // InternalsVisibleTo grant that the native test assemblies depend on.
        foreach (var assembly in ProductAssemblyFacts.ArchitectureAnchors)
        {
            var publicKey = assembly.GetName().GetPublicKeyToken();

            Assert.NotNull(publicKey);
            Assert.NotEmpty(publicKey);
        }
    }

    [Fact]
    public void AbstractionsAssembly_PublishesTheBusContractPublicly()
    {
        // IBus is the contract every consumer binds to. Narrowing it to internal would compile
        // inside the product and break every consumer outside it.
        var contract = ProductAssemblyFacts.Abstractions.GetType("ViciOne.ServiceBus.IBus", throwOnError: true)!;

        Assert.True(contract.IsInterface);
        Assert.True(contract.IsPublic);
    }

    [Fact]
    public void CoreAssembly_DependsOnAbstractions_NotTheOtherWayAround()
    {
        // The dependency direction is the whole point of splitting the two assemblies. Asserting it
        // on the compiled references catches an accidental reference the project file would hide
        // behind a transitive path.
        var coreReferences = ProductAssemblyFacts.ReferencedAssemblyNames(ProductAssemblyFacts.Core);
        var abstractionsReferences =
            ProductAssemblyFacts.ReferencedAssemblyNames(ProductAssemblyFacts.Abstractions);

        Assert.Contains("ViciOne.ServiceBus.Abstractions", coreReferences);
        Assert.DoesNotContain("ViciOne.ServiceBus", abstractionsReferences);
    }

    [Fact]
    public void ProductAssemblies_ExposePublicApi()
    {
        // An assembly that produced no exported type would still load and still pass a mere
        // "does it exist" check. Counting exported types is what makes an empty build fail.
        foreach (var assembly in ProductAssemblyFacts.ArchitectureAnchors)
        {
            Assert.NotEmpty(assembly.GetExportedTypes());
        }
    }

    [Fact]
    public void ProductAssemblies_AreBuiltInTheSameConfigurationAsTheTests()
    {
        // A profile solution that only reaches a product project through a ProjectReference does not
        // include it in the solution configuration mapping. Building the profile in Release then
        // produced a Release test artifact carrying Debug product assemblies - the run asserted one
        // configuration while executing another. The configuration is read off the compiled
        // attribute, so the artifact itself answers rather than the command line that produced it.
        var testConfiguration = ProductAssemblyFacts.ConfigurationOf(ProductAssemblyFacts.ArchitectureTests);

        Assert.False(string.IsNullOrEmpty(testConfiguration));

        var unitSolution = Path.Combine(RepositoryLayout.Root, "ViciOne.ServiceBus.Tests.Unit.slnx");
        var productProjects = XDocument.Load(unitSolution)
            .Descendants("Project")
            .Select(project => project.Attribute("Path")?.Value)
            .Where(path => path?.StartsWith("src/", StringComparison.Ordinal) == true)
            .Select(path => Path.GetFullPath(path!, RepositoryLayout.Root))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        Assert.NotEmpty(productProjects);

        foreach (var project in productProjects)
        {
            var evaluatedConfiguration = MsBuildEvaluation.PropertyOf(project, "Configuration", testConfiguration);
            var targetPath = MsBuildEvaluation.PropertyOf(project, "TargetPath", testConfiguration);

            Assert.Equal(testConfiguration, evaluatedConfiguration);
            Assert.True(
                File.Exists(targetPath),
                $"{RepositoryLayout.RelativeToRoot(project)} did not produce its expected {testConfiguration} artifact: {targetPath}");

            var actualConfiguration = ReadAssemblyConfiguration(targetPath);

            Assert.True(
                string.Equals(testConfiguration, actualConfiguration, StringComparison.Ordinal),
                $"{RepositoryLayout.RelativeToRoot(project)} produced configuration '{actualConfiguration}' instead of '{testConfiguration}'.");
        }
    }

    private static string? ReadAssemblyConfiguration(string assemblyPath)
    {
        using var stream = File.OpenRead(assemblyPath);
        using var peReader = new PEReader(stream);
        var metadata = peReader.GetMetadataReader();

        foreach (var handle in metadata.GetAssemblyDefinition().GetCustomAttributes())
        {
            var attribute = metadata.GetCustomAttribute(handle);
            var attributeType = AttributeTypeName(metadata, attribute.Constructor);

            if (attributeType != "System.Reflection.AssemblyConfigurationAttribute")
            {
                continue;
            }

            var value = metadata.GetBlobReader(attribute.Value);
            Assert.Equal((ushort)1, value.ReadUInt16());
            return value.ReadSerializedString();
        }

        return null;
    }

    private static string? AttributeTypeName(MetadataReader metadata, EntityHandle constructor)
    {
        EntityHandle declaringType = constructor.Kind switch
        {
            HandleKind.MemberReference => metadata.GetMemberReference((MemberReferenceHandle)constructor).Parent,
            HandleKind.MethodDefinition => metadata.GetMethodDefinition((MethodDefinitionHandle)constructor).GetDeclaringType(),
            _ => default
        };

        return declaringType.Kind switch
        {
            HandleKind.TypeReference => TypeName(metadata, metadata.GetTypeReference((TypeReferenceHandle)declaringType)),
            HandleKind.TypeDefinition => TypeName(metadata, metadata.GetTypeDefinition((TypeDefinitionHandle)declaringType)),
            _ => null
        };
    }

    private static string TypeName(MetadataReader metadata, TypeReference type) =>
        $"{metadata.GetString(type.Namespace)}.{metadata.GetString(type.Name)}";

    private static string TypeName(MetadataReader metadata, TypeDefinition type) =>
        $"{metadata.GetString(type.Namespace)}.{metadata.GetString(type.Name)}";

    [Fact]
    public void TestAssembly_RunsOnTheProductFramework()
    {
        Assert.Equal(
            ".NETCoreApp,Version=v10.0",
            ProductAssemblyFacts.TargetFrameworkOf(ProductAssemblyFacts.ArchitectureTests));
    }
}
