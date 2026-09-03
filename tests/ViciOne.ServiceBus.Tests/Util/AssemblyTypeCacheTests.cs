using System.Reflection;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Util;
using ViciOne.ServiceBus.Util.Scanning;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Util;

public sealed class AssemblyTypeCacheTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-ASSEMBLY-SCAN-LIFETIME", "synchronous-single-flight-and-clear")]
    public async Task ConcurrentSynchronousReads_ShareOneSnapshotUntilTheCacheIsCleared()
    {
        AssemblyTypeCache.Clear();
        Assembly assembly = typeof(AssemblyTypeCacheTests).Assembly;

        AssemblyScanTypeInfo[] concurrent = await Task.WhenAll(
            Enumerable.Range(0, 32)
                .Select(_ => Task.Run(() => AssemblyTypeCache.ForAssembly(assembly), TestContext.Current.CancellationToken)));

        AssemblyScanTypeInfo first = concurrent[0];
        Assert.All(concurrent, value => Assert.Same(first, value));

        AssemblyTypeCache.Clear();
        AssemblyScanTypeInfo replacement = AssemblyTypeCache.ForAssembly(assembly);

        Assert.NotSame(first, replacement);
        Assert.Equal(first.Record.Name, replacement.Record.Name);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASSEMBLY-SCAN-API", "scanner-returns-filtered-types-synchronously")]
    public void ScanForTypes_ReturnsTheFilteredTypeSetSynchronously()
    {
        var scanner = new AssemblyScanner();
        scanner.Assembly(typeof(AssemblyTypeCacheTests).Assembly);
        scanner.Include(type => type == typeof(AssemblyTypeCacheTests));

        TypeSet result = scanner.ScanForTypes();

        Assert.Equal([typeof(AssemblyTypeCacheTests)], result.AllTypes());
        Assert.Equal(typeof(TypeSet), typeof(IAssemblyScanner).GetMethod(nameof(IAssemblyScanner.ScanForTypes))!.ReturnType);
    }
}
