using ViciOne.ServiceBus.Analyzers.CodeFixes.Tests.Fixtures;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.Infrastructure.Roslyn;
using Xunit;

namespace ViciOne.ServiceBus.Analyzers.CodeFixes.Tests;

public sealed class CancellationTokenOverloadMethodFixerTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-CANCELLATION-TOKEN-CODEFIX", "task-delay-overload")]
    public async Task TaskDelayFix_AddsTheContextCancellationToken()
    {
        var source = Prefix + @"
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
        var expected = Prefix + @"
namespace ConsoleApplication1
{
    class Consumer :
        IConsumer<SubmitOrder>
    {
        public Task Consume(ConsumeContext<SubmitOrder> context)
        {
            return Task.Delay(10, context.CancellationToken);
        }
    }
}
";

        await AssertFixedSource(source, expected);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CANCELLATION-TOKEN-CODEFIX", "state-machine-activity-overloads")]
    public async Task ActivityFix_AddsEachMatchingBehaviorContextToken()
    {
        var source = Prefix + @"
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
        var expected = Prefix + @"
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
            return Task.Delay(10, context.CancellationToken);
        }

        Task IStateMachineActivity<TestInstance, SubmitOrder>.Faulted<TException>(BehaviorExceptionContext<TestInstance, SubmitOrder, TException> ctx,
            IBehavior<TestInstance, SubmitOrder> next)
        {
            return Task.Run(() => next.Faulted(ctx), ctx.CancellationToken);
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

        await AssertFixedSource(source, expected);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CANCELLATION-TOKEN-CODEFIX", "later-task-run-overload")]
    public async Task TaskRunFix_PreservesExistingTokenUseAndAddsTheMissingOne()
    {
        var source = Prefix + @"
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
        var expected = Prefix + @"
namespace ConsoleApplication1
{
    class Consumer :
        IConsumer<SubmitOrder>
    {
        public async Task Consume(ConsumeContext<SubmitOrder> context)
        {
            await Task.Delay(10, context.CancellationToken);
            context.RespondAsync<OrderSubmitted>(context.Message);
            await Task.Run(() => {}, context.CancellationToken);

        }
    }
}
";

        await AssertFixedSource(source, expected);
    }

    private static string Prefix =>
        ServiceBusCodeFixFixture.Usings + ServiceBusCodeFixFixture.SimpleMessageContracts;

    private static async Task AssertFixedSource(string source, string expected)
    {
        var actual = await RoslynTestHost.ApplyAllFixesAsync(
            source,
            new CancellationTokenOverloadMethodAnalyzer(),
            new CancellationTokenOverloadMethodFixer(),
            ServiceBusCodeFixFixture.ReferenceRoots,
            TestContext.Current.CancellationToken);

        Assert.Equal(RoslynTestHost.NormalizeLineEndings(expected), actual);
    }
}
