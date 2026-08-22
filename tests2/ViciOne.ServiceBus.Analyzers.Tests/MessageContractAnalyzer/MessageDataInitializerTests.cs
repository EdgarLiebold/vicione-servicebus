using Microsoft.CodeAnalysis;
using ViciOne.ServiceBus.Analyzers.Tests.Fixtures;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.Infrastructure.Roslyn;
using ViciOne.ServiceBus.Tests.Infrastructure.Roslyn.Diagnostics;
using Xunit;

namespace ViciOne.ServiceBus.Analyzers.Tests.MessageContractAnalyzer;

public sealed class MessageDataInitializerTests
{
    private const string Usings = @"
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using ViciOne.ServiceBus;
";

    private const string Contracts = @"
namespace ConsoleApplication1
{
    public interface ProcessDocument
    {
        Guid Id { get; }
        string CustomerId { get; }
        MessageData<string> Document { get; }
        MessageData<Stream> Stream { get; }
    }
}
";

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-CONTRACT-ANALYZER", "message-data-missing-properties")]
    public async Task EmptyAnonymousValue_ReportsEveryMissingMessageDataProperty()
    {
        var source = Usings + Contracts + @"
namespace ConsoleApplication1
{
    class Program
    {
        static async Task Main()
        {
            var bus = Bus.Factory.CreateUsingInMemory(cfg => { });

            await bus.Publish<ProcessDocument>(new
            {
            });
        }
    }
}
";

        await AssertDiagnostics(
            source,
            new DiagnosticObservation(
                "MCA0003",
                DiagnosticSeverity.Info,
                "Anonymous type is missing properties that are in the message contract 'ProcessDocument'. The following properties are missing: Id, CustomerId, Document, Stream.",
                "Test0.cs",
                28,
                48));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-CONTRACT-ANALYZER", "message-data-incompatible-values")]
    public async Task ScalarValues_RejectMessageDataStringAndStreamContracts()
    {
        var source = Usings + Contracts + @"
namespace ConsoleApplication1
{
    class Program
    {
        static async Task Main()
        {
            var bus = Bus.Factory.CreateUsingInMemory(cfg => { });

            await bus.Publish<ProcessDocument>(new
            {
                InVar.Id,
                CustomerId = ""53051996-AEEC-4EF1-BCFD-7835F17BA8E7"",
                Document = 72,
                Stream = 42
            });
        }
    }
}
";

        await AssertDiagnostics(
            source,
            new DiagnosticObservation(
                "MCA0001",
                DiagnosticSeverity.Error,
                "Anonymous type does not map to message contract 'ProcessDocument'. The following properties of the anonymous type are incompatible: Document, Stream.",
                "Test0.cs",
                28,
                48));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-CONTRACT-ANALYZER", "message-data-stream-compatible")]
    public async Task StreamValue_SatisfiesTheMessageDataStreamContract()
    {
        var source = Usings + Contracts + @"
namespace ConsoleApplication1
{
    class Program
    {
        static async Task Main()
        {
            var bus = Bus.Factory.CreateUsingInMemory(cfg => { });
            Stream testStream = new MemoryStream();

            await bus.Publish<ProcessDocument>(new
            {
                InVar.Id,
                CustomerId = ""53051996-AEEC-4EF1-BCFD-7835F17BA8E7"",
                Document = ""This would be a really big document typically"",
                Stream = testStream
            });
        }
    }
}
";

        await AssertDiagnostics(source);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-CONTRACT-ANALYZER", "message-data-derived-stream-compatible")]
    public async Task DerivedStreamValue_SatisfiesTheMessageDataStreamContract()
    {
        var source = Usings + Contracts + @"
namespace ConsoleApplication1
{
    class Program
    {
        static async Task Main()
        {
            var bus = Bus.Factory.CreateUsingInMemory(cfg => { });
            MemoryStream testStream = new MemoryStream();

            await bus.Publish<ProcessDocument>(new
            {
                InVar.Id,
                CustomerId = ""53051996-AEEC-4EF1-BCFD-7835F17BA8E7"",
                Document = ""This would be a really big document typically"",
                Stream = testStream
            });
        }
    }
}
";

        await AssertDiagnostics(source);
    }

    private static async Task AssertDiagnostics(
        string source,
        params DiagnosticObservation[] expected)
    {
        var actual = await RoslynTestHost.AnalyzeAsync(
            source,
            new global::ViciOne.ServiceBus.Analyzers.MessageContractAnalyzer(),
            ServiceBusAnalyzerFixture.ReferenceRoots,
            TestContext.Current.CancellationToken);

        ServiceBusAnalyzerFixture.AssertDiagnostics(actual, expected);
    }
}
