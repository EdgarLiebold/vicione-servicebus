using System.Reflection;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Util.Scanning;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Util;

public sealed class AssemblyScannerTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-ASSEMBLY-SCAN-CALLER", "registers-actual-user-assembly")]
    public void TheCallingAssembly_RegistersTheUserAssemblyForTypeScanning()
    {
        var scanner = new AssemblyScanner();
        scanner.TheCallingAssembly();
        scanner.Include(type => type == typeof(AssemblyScannerTests));

        Assert.Equal(1, scanner.Count);
        Assert.True(scanner.Contains(typeof(AssemblyScannerTests).Assembly.GetName().Name!));
        Assert.Equal([typeof(AssemblyScannerTests)], scanner.ScanForTypes().AllTypes());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASSEMBLY-SCAN-IDENTITY", "mixed-registration-keeps-one-type-result")]
    public void RegisteringTheSameAssemblyThroughAllEntryPoints_DoesNotDuplicateDiscoveredTypes()
    {
        var scanner = new AssemblyScanner();
        scanner.Assembly(typeof(AssemblyScannerTests).Assembly);
        scanner.Assembly(typeof(AssemblyScannerTests).Assembly.GetName().Name!);
        scanner.AssemblyContainingType<AssemblyScannerTests>();
        scanner.AssemblyContainingType(typeof(AssemblyScannerTests));
        scanner.Include(type => type == typeof(AssemblyScannerTests));

        Assert.Equal(1, scanner.Count);
        Assert.Equal([typeof(AssemblyScannerTests)], scanner.ScanForTypes().AllTypes());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [RequirementCoverage("REQ-VSB-ASSEMBLY-SCAN-IDENTITY", "each-entry-point-registers-discoverable-assembly")]
    public void EachAssemblyEntryPoint_IndependentlyRegistersDiscoverableTypes(int entryPoint)
    {
        Assembly expected = typeof(AssemblyScannerTests).Assembly;
        var scanner = new AssemblyScanner();

        switch (entryPoint)
        {
            case 0:
                scanner.Assembly(expected);
                break;
            case 1:
                scanner.Assembly(expected.GetName().Name!);
                break;
            case 2:
                scanner.AssemblyContainingType<AssemblyScannerTests>();
                break;
            case 3:
                scanner.AssemblyContainingType(typeof(AssemblyScannerTests));
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(entryPoint));
        }

        scanner.Include(type => type == typeof(AssemblyScannerTests));
        Assert.Equal(1, scanner.Count);
        Assert.True(scanner.Contains(expected.GetName().Name!));
        Assert.Equal([typeof(AssemblyScannerTests)], scanner.ScanForTypes().AllTypes());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASSEMBLY-SCAN-FILTER", "namespace-include-with-explicit-type-exclusion")]
    public void NamespaceIncludeAndTypeExclude_SelectOnlyEligibleExportedTypes()
    {
        var scanner = new AssemblyScanner();
        scanner.AssemblyContainingType<AssemblyScannerTests>();
        scanner.IncludeNamespaceContainingType<AssemblyScannerTests>();
        scanner.ExcludeType<AssemblyScannerTests>();

        Type[] types = scanner.ScanForTypes().AllTypes().ToArray();

        Assert.Contains(typeof(AssemblyFinderTests), types);
        Assert.DoesNotContain(typeof(AssemblyScannerTests), types);
        Assert.All(types, type => Assert.StartsWith(typeof(AssemblyScannerTests).Namespace!, type.Namespace!));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASSEMBLY-SCAN-FILTER", "case-insensitive-include-and-exclude-path-scan")]
    public void PathScan_AppliesCaseInsensitiveIncludeAndExcludeFilePrefixes()
    {
        string path = Path.Combine(Path.GetTempPath(), $"vsb-scanner-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        try
        {
            Assembly expected = typeof(AssemblyScannerTests).Assembly;
            File.Copy(expected.Location, Path.Combine(path, "SELECT-accepted.dll"));
            Assembly rejected = typeof(AssemblyScanner).Assembly;
            File.Copy(rejected.Location, Path.Combine(path, "select-rejected.dll"));
            Assembly ignored = typeof(FactAttribute).Assembly;
            File.Copy(ignored.Location, Path.Combine(path, "ignored-other.dll"));

            var scanner = new AssemblyScanner();
            scanner.IncludeFileNameStartsWith("select");
            scanner.ExcludeFileNameStartsWith("SELECT-rejected");
            scanner.AssembliesFromPath(path);
            scanner.Include(type => type == typeof(AssemblyScannerTests));

            Assert.Equal(1, scanner.Count);
            Assert.True(scanner.Contains(expected.GetName().Name!));
            Assert.False(scanner.Contains(rejected.GetName().Name!));
            Assert.False(scanner.Contains(ignored.GetName().Name!));
            Assert.Equal([typeof(AssemblyScannerTests)], scanner.ScanForTypes().AllTypes());
        }
        finally
        {
            Directory.Delete(path, recursive: true);
        }
    }
}
