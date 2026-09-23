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
    public async Task ConcurrentSynchronousReads_ShareOneSnapshotUntilTheCacheIsClearedAsync()
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

    [Fact]
    [RequirementCoverage("REQ-VSB-ASSEMBLY-SCAN-FAILURE", "multiple-exported-type-failures-cache-and-retry")]
    public void FailedAssemblyScan_IsCachedAndReportedWithoutHidingHealthyTypes()
    {
        var firstFailure = new TypeLoadException("The first referenced contract could not be loaded.");
        var secondFailure = new TypeLoadException("The second referenced contract could not be loaded.");
        var firstAssembly = new RecoverableAssembly("FirstFaultingAssembly", firstFailure);
        var secondAssembly = new RecoverableAssembly("SecondFaultingAssembly", secondFailure);
        AssemblyTypeCache.Clear();

        try
        {
            AssemblyScanTypeInfo first = AssemblyTypeCache.ForAssembly(firstAssembly);
            AssemblyScanTypeInfo second = AssemblyTypeCache.ForAssembly(secondAssembly);
            AssemblyScanTypeInfo healthy = AssemblyTypeCache.ForAssembly(typeof(AssemblyTypeCacheTests).Assembly);

            Assert.Same(firstFailure, first.Record.LoadException);
            Assert.Same(secondFailure, second.Record.LoadException);
            Assert.Empty(first.FindTypes(TypeClassification.All));
            Assert.Empty(second.FindTypes(TypeClassification.All));
            Assert.Contains(typeof(AssemblyTypeCacheTests), healthy.FindTypes(TypeClassification.All));
            AssemblyScanTypeInfo[] failed = AssemblyTypeCache.FailedAssemblies().ToArray();
            Assert.Equal(2, failed.Length);
            Assert.Contains(first, failed);
            Assert.Contains(second, failed);

            AggregateException aggregate = Assert.Throws<AggregateException>(AssemblyTypeCache.ThrowIfAnyTypeScanFailures);
            Assert.Equal(2, aggregate.InnerExceptions.Count);
            Assert.Contains(firstFailure, aggregate.InnerExceptions);
            Assert.Contains(secondFailure, aggregate.InnerExceptions);

            firstAssembly.Recover();
            secondAssembly.Recover();
            Assert.Same(first, AssemblyTypeCache.ForAssembly(firstAssembly));
            Assert.Same(second, AssemblyTypeCache.ForAssembly(secondAssembly));
            Assert.Equal(1, firstAssembly.ScanCount);
            Assert.Equal(1, secondAssembly.ScanCount);

            AssemblyTypeCache.Clear();
            AssemblyScanTypeInfo recoveredFirst = AssemblyTypeCache.ForAssembly(firstAssembly);
            AssemblyScanTypeInfo recoveredSecond = AssemblyTypeCache.ForAssembly(secondAssembly);

            Assert.NotSame(first, recoveredFirst);
            Assert.NotSame(second, recoveredSecond);
            Assert.Null(recoveredFirst.Record.LoadException);
            Assert.Null(recoveredSecond.Record.LoadException);
            Assert.Contains(typeof(AssemblyTypeCacheTests), recoveredFirst.FindTypes(TypeClassification.All));
            Assert.Contains(typeof(AssemblyTypeCacheTests), recoveredSecond.FindTypes(TypeClassification.All));
            Assert.Empty(AssemblyTypeCache.FailedAssemblies());
            AssemblyTypeCache.ThrowIfAnyTypeScanFailures();
            Assert.Equal(2, firstAssembly.ScanCount);
            Assert.Equal(2, secondAssembly.ScanCount);
        }
        finally
        {
            AssemblyTypeCache.Clear();
        }
    }

    private sealed class RecoverableAssembly(string name, Exception failure) : Assembly
    {
        private bool _recovered;

        public int ScanCount { get; private set; }

        public override string FullName => name;

        public override Type[] GetExportedTypes()
        {
            ScanCount++;
            if (!_recovered)
                throw failure;

            return [typeof(AssemblyTypeCacheTests)];
        }

        public void Recover() => _recovered = true;
    }
}
