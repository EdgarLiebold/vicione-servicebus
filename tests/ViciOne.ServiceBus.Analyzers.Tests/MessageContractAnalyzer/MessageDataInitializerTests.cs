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
using ViciOne.ServiceBus; using ViciOne.ServiceBus.Advanced.Initializers;
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
    public async Task EmptyAnonymousValue_ReportsEveryMissingMessageDataPropertyAsync()
    {
        var source = Usings + Contracts + @"
namespace ConsoleApplication1
{
    class Program
    {
        static async Task Main()
        {
            var bus = Bus.Factory.CreateUsingInMemory(cfg => { });

            await bus.PublishAsync<ProcessDocument>(new
            {
            });
        }
    }
}
";

        await AssertDiagnosticsAsync(
            source,
            new DiagnosticObservation(
                "VOSB1004",
                DiagnosticSeverity.Info,
                "Message values for contract 'ProcessDocument' are missing properties: Id, CustomerId, Document, Stream",
                "Test0.cs",
                28,
                53));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-CONTRACT-ANALYZER", "message-data-incompatible-values")]
    public async Task ScalarValues_RejectMessageDataStringAndStreamContractsAsync()
    {
        var source = Usings + Contracts + @"
namespace ConsoleApplication1
{
    class Program
    {
        static async Task Main()
        {
            var bus = Bus.Factory.CreateUsingInMemory(cfg => { });

            await bus.PublishAsync<ProcessDocument>(new
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

        await AssertDiagnosticsAsync(
            source,
            new DiagnosticObservation(
                "VOSB1002",
                DiagnosticSeverity.Error,
                "Message values do not map to contract 'ProcessDocument'; incompatible properties: Document, Stream",
                "Test0.cs",
                28,
                53));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-CONTRACT-ANALYZER", "message-data-stream-compatible")]
    public async Task StreamValue_SatisfiesTheMessageDataStreamContractAsync()
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

            await bus.PublishAsync<ProcessDocument>(new
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

        await AssertDiagnosticsAsync(source);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-CONTRACT-ANALYZER", "message-data-derived-stream-compatible")]
    public async Task DerivedStreamValue_SatisfiesTheMessageDataStreamContractAsync()
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

            await bus.PublishAsync<ProcessDocument>(new
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

        await AssertDiagnosticsAsync(source);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-CONTRACT-ANALYZER", "message-data-supported-conversion-matrix")]
    public async Task SupportedMessageDataInputs_SatisfyEveryCanonicalConversionAsync()
    {
        var source = Usings + @"
namespace ConsoleApplication1
{
    public interface Document
    {
        string Name { get; }
    }

    public sealed class PdfDocument : Document
    {
        public string Name { get; init; }
    }

    public interface StoreDocument
    {
        MessageData<byte[]> BinaryFromBytes { get; }
        MessageData<byte[]> BinaryFromText { get; }
        MessageData<byte[]> BinaryFromMessageData { get; }
        MessageData<string> Text { get; }
        MessageData<Stream> Stream { get; }
        MessageData<Document> Document { get; }
    }

    class Program
    {
        static async Task Main()
        {
            var bus = Bus.Factory.CreateUsingInMemory(cfg => { });
            MessageData<string> textData = default!;

            await bus.PublishAsync<StoreDocument>(new
            {
                BinaryFromBytes = new byte[] { 1, 2, 3 },
                BinaryFromText = ""content"",
                BinaryFromMessageData = textData,
                Text = ""content"",
                Stream = new MemoryStream(),
                Document = new PdfDocument { Name = ""document"" }
            });
        }
    }
}
";

        await AssertDiagnosticsAsync(source);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-CONTRACT-ANALYZER", "message-data-unsupported-conversion-matrix")]
    public async Task UnsupportedMessageDataInputs_ReportEveryIncompatiblePropertyWithoutHangingAsync()
    {
        var source = Usings + @"
namespace ConsoleApplication1
{
    public interface InvalidMessageData
    {
        MessageData<byte[]> Binary { get; }
        MessageData<string> Text { get; }
        MessageData<Stream> Stream { get; }
        MessageData<int> ValueType { get; }
    }

    class Program
    {
        static async Task Main()
        {
            var bus = Bus.Factory.CreateUsingInMemory(cfg => { });

            await bus.PublishAsync<InvalidMessageData>(new
            {
                Binary = 1,
                Text = 2,
                Stream = 3,
                ValueType = 4
            });
        }
    }
}
";

        var diagnostic = Assert.Single(await AnalyzeAsync(source));
        Assert.Equal("VOSB1002", diagnostic.Id);
        Assert.Equal(
            "Message values do not map to contract 'InvalidMessageData'; incompatible properties: Binary, Text, Stream, ValueType",
            diagnostic.Message);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-CONTRACT-ANALYZER", "nullable-source-and-target-conversions")]
    public async Task NullableAndNonNullableValues_AreCompatibleInBothDirectionsAsync()
    {
        var source = Usings + @"
namespace ConsoleApplication1
{
    public interface PriceChanged
    {
        decimal? OptionalPrice { get; }
        decimal RequiredPrice { get; }
    }

    class Program
    {
        static async Task Main()
        {
            var bus = Bus.Factory.CreateUsingInMemory(cfg => { });
            decimal? requiredPrice = 12m;

            await bus.PublishAsync<PriceChanged>(new
            {
                OptionalPrice = 10m,
                RequiredPrice = requiredPrice
            });
        }
    }
}
";

        await AssertDiagnosticsAsync(source);
    }

    private static async Task AssertDiagnosticsAsync(
        string source,
        params DiagnosticObservation[] expected)
    {
        var actual = await AnalyzeAsync(source);

        ServiceBusAnalyzerFixture.AssertDiagnostics(actual, expected);
    }

    private static Task<IReadOnlyList<DiagnosticObservation>> AnalyzeAsync(string source) =>
        RoslynTestHost.AnalyzeAsync(
            source,
            new global::ViciOne.ServiceBus.Analyzers.MessageContractAnalyzer(),
            ServiceBusAnalyzerFixture.ReferenceRoots,
            TestContext.Current.CancellationToken);
}
