using System.Reflection;
using ViciOne.ServiceBus.Tests.Infrastructure.Roslyn.Diagnostics;
using Xunit;

namespace ViciOne.ServiceBus.Analyzers.Tests.Fixtures;

internal static class ServiceBusAnalyzerFixture
{
    internal static IReadOnlyCollection<Assembly> ReferenceRoots { get; } = [typeof(Bus).Assembly];

    internal const string Usings = @"
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ViciOne.ServiceBus; using ViciOne.ServiceBus.Advanced; using ViciOne.ServiceBus.Advanced.Initializers;
";

    internal const string SimpleMessageContracts = @"
namespace ConsoleApplication1
{
    public interface OrderSubmitted
    {
        Guid Id { get; }
        string CustomerId { get; }
    }

    public interface SubmitOrder
    {
        Guid Id { get; }
        string CustomerId { get; }
    }
}
";

    internal static void AssertDiagnostics(
        IReadOnlyList<DiagnosticObservation> actual,
        params DiagnosticObservation[] expected) =>
        Assert.Equal(expected, actual);
}
