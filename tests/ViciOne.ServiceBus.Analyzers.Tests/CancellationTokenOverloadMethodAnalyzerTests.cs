using Microsoft.CodeAnalysis;
using ViciOne.ServiceBus.Analyzers.Tests.Fixtures;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.Infrastructure.Roslyn;
using ViciOne.ServiceBus.Tests.Infrastructure.Roslyn.Diagnostics;
using Xunit;

namespace ViciOne.ServiceBus.Analyzers.Tests;

public sealed class CancellationTokenOverloadMethodAnalyzerTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-CANCELLATION-TOKEN-ANALYZER", "task-delay-overload")]
    public async Task TaskDelayWithoutToken_ReportsTheContextTokenOverload()
    {
        var source = ServiceBusAnalyzerFixture.Usings + ServiceBusAnalyzerFixture.SimpleMessageContracts + @"
namespace ConsoleApplication1
{
    class Consumer :
        IConsumer<SubmitOrder>
    {
        public Task Consume(ConsumeContext<SubmitOrder> context)
        {
            return Task.Delay(10);
        }
    }
}
";

        await AssertDiagnostics(
            source,
            new DiagnosticObservation(
                "MCA2016",
                DiagnosticSeverity.Info,
                "Cancellation token from 'context.CancellationToken' can be used in cancellation token overload for 'Task.Delay' method",
                "Test0.cs",
                30,
                20));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CANCELLATION-TOKEN-ANALYZER", "state-machine-activity-overloads")]
    public async Task StateMachineActivity_ReportsEachAvailableContextToken()
    {
        var source = ServiceBusAnalyzerFixture.Usings + ServiceBusAnalyzerFixture.SimpleMessageContracts + @"
namespace ConsoleApplication1
{

        class TestInstance :
            SagaStateMachineInstance
        {
            public Guid CorrelationId { get; set; }
            public State CurrentState { get; set; }
            public string Value { get; set; }
        }

        class SetValueAsyncActivity :
            IStateMachineActivity<TestInstance, SubmitOrder>
        {

        Task IStateMachineActivity<TestInstance, SubmitOrder>.Execute(BehaviorContext<TestInstance, SubmitOrder> context,
            IBehavior<TestInstance, SubmitOrder> next)
        {
            return Task.Delay(10);
        }

        Task IStateMachineActivity<TestInstance, SubmitOrder>.Faulted<TException>(BehaviorExceptionContext<TestInstance, SubmitOrder, TException> ctx,
            IBehavior<TestInstance, SubmitOrder> next)
        {
            return Task.Run(() => next.Faulted(ctx));
        }

        public void Accept(StateMachineVisitor visitor)
        {
            visitor.Visit(this);
        }

        public void Probe(ProbeContext context)
        {
        }
    }
}
";

        await AssertDiagnostics(
            source,
            new DiagnosticObservation(
                "MCA2016",
                DiagnosticSeverity.Info,
                "Cancellation token from 'context.CancellationToken' can be used in cancellation token overload for 'Task.Delay' method",
                "Test0.cs",
                41,
                20),
            new DiagnosticObservation(
                "MCA2016",
                DiagnosticSeverity.Info,
                "Cancellation token from 'ctx.CancellationToken' can be used in cancellation token overload for 'Task.Run' method",
                "Test0.cs",
                47,
                20));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CANCELLATION-TOKEN-ANALYZER", "later-task-run-overload")]
    public async Task ExistingTokenUse_DoesNotSuppressASeparateMissingToken()
    {
        var source = ServiceBusAnalyzerFixture.Usings + ServiceBusAnalyzerFixture.SimpleMessageContracts + @"
namespace ConsoleApplication1
{
    class Consumer :
        IConsumer<SubmitOrder>
    {
        public async Task Consume(ConsumeContext<SubmitOrder> context)
        {
            await Task.Delay(10, context.CancellationToken);
            context.RespondAsync<OrderSubmitted>(context.Message);
            await Task.Run(() => {});

        }
    }
}
";

        await AssertDiagnostics(
            source,
            new DiagnosticObservation(
                "MCA2016",
                DiagnosticSeverity.Info,
                "Cancellation token from 'context.CancellationToken' can be used in cancellation token overload for 'Task.Run' method",
                "Test0.cs",
                32,
                19));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CANCELLATION-TOKEN-ANALYZER", "existing-token-is-silent")]
    public async Task ExistingCancellationToken_DoesNotReport()
    {
        var source = ServiceBusAnalyzerFixture.Usings + ServiceBusAnalyzerFixture.SimpleMessageContracts + @"
namespace ConsoleApplication1
{
    class Consumer :
        IConsumer<SubmitOrder>
    {
        public async Task Consume(ConsumeContext<SubmitOrder> context)
        {
            await Task.Delay(10, context.CancellationToken);
            context.RespondAsync<OrderSubmitted>(context.Message);
        }
    }
}
";

        await AssertDiagnostics(source);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CANCELLATION-TOKEN-ANALYZER", "servicebus-method-is-excluded")]
    public async Task ServiceBusPublishMethod_DoesNotSuggestItsOwnContextToken()
    {
        var source = ServiceBusAnalyzerFixture.Usings + ServiceBusAnalyzerFixture.SimpleMessageContracts + @"
namespace ConsoleApplication1
{
    class Consumer :
        IConsumer<SubmitOrder>
    {
        public Task Consume(ConsumeContext<SubmitOrder> context)
        {
            return context.Publish<OrderSubmitted>(new {});
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
            new CancellationTokenOverloadMethodAnalyzer(),
            ServiceBusAnalyzerFixture.ReferenceRoots,
            TestContext.Current.CancellationToken);

        ServiceBusAnalyzerFixture.AssertDiagnostics(actual, expected);
    }
}
