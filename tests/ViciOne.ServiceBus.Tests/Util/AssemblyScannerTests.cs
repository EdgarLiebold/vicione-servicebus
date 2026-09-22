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
}
