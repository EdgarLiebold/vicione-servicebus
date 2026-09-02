using Microsoft.CodeAnalysis;
using ViciOne.ServiceBus.Analyzers.Tests.Fixtures;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.Infrastructure.Roslyn;
using ViciOne.ServiceBus.Tests.Infrastructure.Roslyn.Diagnostics;
using Xunit;

namespace ViciOne.ServiceBus.Analyzers.Tests;

public sealed class AsyncMethodAnalyzerTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-ASYNC-METHOD-ANALYZER", "publish-must-be-observed")]
    public async Task PublishWithoutAwaitOrCapture_ReportsMessageLossRisk()
    {
        var source = ServiceBusAnalyzerFixture.Usings + ServiceBusAnalyzerFixture.SimpleMessageContracts + @"
namespace ConsoleApplication1
{
    class Program
    {
        static async Task Main()
        {
            var bus = Bus.Factory.CreateUsingInMemory(cfg => { });

            bus.Publish<OrderSubmitted>(new
            {
                Id = NewId.NextGuid(),
                CustomerId = ""427"",
            });

            await bus.Publish<OrderSubmitted>(new
            {
                Id = NewId.NextGuid(),
                CustomerId = ""427"",
            });
        }
    }
}
";

        await AssertSingleDiagnostic(
            source,
            "Method IPublishEndpoint.Publish<OrderSubmitted>() is not awaited or captured and may result in message loss",
            line: 31,
            column: 13);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASYNC-METHOD-ANALYZER", "send-must-be-observed")]
    public async Task SendWithoutAwaitOrCapture_ReportsMessageLossRisk()
    {
        var source = ServiceBusAnalyzerFixture.Usings + ServiceBusAnalyzerFixture.SimpleMessageContracts + @"
namespace ConsoleApplication1
{
    class Program
    {
        static async Task Main()
        {
            var bus = Bus.Factory.CreateUsingInMemory(cfg => { });

            var endpoint = await bus.GetSendEndpoint(new Uri(""loopback://localhost/input_queue""));
            endpoint.Send<OrderSubmitted>(new
            {
                Id = NewId.NextGuid(),
                CustomerId = ""427"",
            });

            await endpoint.Send<OrderSubmitted>(new
            {
                Id = NewId.NextGuid(),
                CustomerId = ""427"",
            });
        }
    }
}
";

        await AssertSingleDiagnostic(
            source,
            "Method ISendEndpoint.Send<OrderSubmitted>() is not awaited or captured and may result in message loss",
            line: 32,
            column: 13);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASYNC-METHOD-ANALYZER", "request-must-be-observed")]
    public async Task GetResponseWithoutAwaitOrCapture_ReportsMessageLossRisk()
    {
        var source = ServiceBusAnalyzerFixture.Usings + ServiceBusAnalyzerFixture.SimpleMessageContracts + @"
namespace ConsoleApplication1
{
    class Program
    {
        static async Task Main()
        {
            var bus = Bus.Factory.CreateUsingInMemory(cfg => { });

            var client = bus.CreateRequestClient<SubmitOrder>();
            client.GetResponse<OrderSubmitted>(new
            {
                Id = NewId.NextGuid(),
                CustomerId = ""427"",
            });

            var response = await client.GetResponse<OrderSubmitted>(new
            {
                Id = NewId.NextGuid(),
                CustomerId = ""427"",
            });
        }
    }
}
";

        await AssertSingleDiagnostic(
            source,
            "Method IRequestClient<SubmitOrder>.GetResponse<OrderSubmitted>() is not awaited or captured and may result in message loss",
            line: 32,
            column: 13);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASYNC-METHOD-ANALYZER", "response-must-be-observed")]
    public async Task RespondWithoutAwaitOrCapture_ReportsMessageLossRisk()
    {
        var source = ServiceBusAnalyzerFixture.Usings + ServiceBusAnalyzerFixture.SimpleMessageContracts + @"
namespace ConsoleApplication1
{
    class Consumer :
        IConsumer<SubmitOrder>
    {
        public async Task Consume(ConsumeContext<SubmitOrder> context)
        {
            context.RespondAsync<OrderSubmitted>(context.Message);
        }
    }

    class Program
    {
        static async Task Main()
        {
            var bus = Bus.Factory.CreateUsingInMemory(cfg => { });

            var client = bus.CreateRequestClient<SubmitOrder>();
            var response = await client.GetResponse<OrderSubmitted>(new
            {
                Id = NewId.NextGuid(),
                CustomerId = ""427"",
            });
        }
    }
}
";

        await AssertSingleDiagnostic(
            source,
            "Method ConsumeContext.RespondAsync<OrderSubmitted>() is not awaited or captured and may result in message loss",
            line: 30,
            column: 13);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASYNC-METHOD-ANALYZER", "non-task-create-is-excluded")]
    public async Task RequestHandleCreation_DoesNotReportAsyncMessageLossRisk()
    {
        var source = ServiceBusAnalyzerFixture.Usings + ServiceBusAnalyzerFixture.SimpleMessageContracts + @"
namespace ConsoleApplication1
{
    class Program
    {
        static async Task Main()
        {
            var bus = Bus.Factory.CreateUsingInMemory(cfg => { });

            var client = bus.CreateRequestClient<SubmitOrder>();
            client.Create(new
            {
                Id = NewId.NextGuid(),
                CustomerId = ""427"",
            });
        }
    }
}
";

        var actual = await RoslynTestHost.AnalyzeAsync(
            source,
            new AsyncMethodAnalyzer(),
            ServiceBusAnalyzerFixture.ReferenceRoots,
            TestContext.Current.CancellationToken);

        Assert.Empty(actual);
    }

    private static async Task AssertSingleDiagnostic(
        string source,
        string message,
        int line,
        int column)
    {
        var actual = await RoslynTestHost.AnalyzeAsync(
            source,
            new AsyncMethodAnalyzer(),
            ServiceBusAnalyzerFixture.ReferenceRoots,
            TestContext.Current.CancellationToken);

        ServiceBusAnalyzerFixture.AssertDiagnostics(
            actual,
            new DiagnosticObservation(
                "ViciOneServiceBus0001",
                DiagnosticSeverity.Warning,
                message,
                "Test0.cs",
                line,
                column));
    }
}
