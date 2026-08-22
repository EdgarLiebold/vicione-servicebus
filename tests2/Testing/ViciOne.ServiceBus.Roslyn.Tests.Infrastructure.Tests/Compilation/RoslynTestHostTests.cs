using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.Infrastructure.Roslyn;
using Xunit;

namespace ViciOne.ServiceBus.Roslyn.Tests.Infrastructure.Tests.Compilation;

public sealed class RoslynTestHostTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-ROSLYN-TEST-HOST", "invalid-fixture-fails-closed")]
    public async Task InvalidFixture_ThrowsBeforeAnalyzerExecution()
    {
        const string source = "class Fixture { ThisTypeDoesNotExistAnywhere Value { get; set; } }";

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => RoslynTestHost.ValidateCompilationAsync(
                source,
                cancellationToken: TestContext.Current.CancellationToken));

        Assert.Contains("compilation reported", exception.Message, StringComparison.Ordinal);
        Assert.Contains("CS0246", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ROSLYN-TEST-HOST", "valid-fixture-binds")]
    public async Task ValidFixture_ProducesNoCompilationFailure()
    {
        const string source = "using System; class Fixture { Uri Value { get; } = new(\"urn:test\"); }";

        await RoslynTestHost.ValidateCompilationAsync(
            source,
            cancellationToken: TestContext.Current.CancellationToken);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ROSLYN-TEST-HOST", "trusted-platform-closure")]
    public async Task SharedFrameworkFacades_AreAvailableToFixtures()
    {
        const string source = """
            using System;
            using System.ComponentModel;
            using System.Threading.Tasks;

            class Fixture
            {
                Uri Value { get; } = new("urn:test");
                IServiceProvider? Provider { get; }
                PropertyChangedEventHandler? Handler { get; }
                Task Completed { get; } = Task.CompletedTask;
            }
            """;

        await RoslynTestHost.ValidateCompilationAsync(
            source,
            cancellationToken: TestContext.Current.CancellationToken);
    }
}
