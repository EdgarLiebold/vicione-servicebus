using ViciOne.ServiceBus.Analyzers.CodeFixes.Tests.Fixtures;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.Infrastructure.Roslyn;
using Xunit;

namespace ViciOne.ServiceBus.Analyzers.CodeFixes.Tests;

public sealed class CancellationTokenOverloadMethodFixerTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-CANCELLATION-TOKEN-CODEFIX", "task-delay-overload")]
    public async Task TaskDelayFix_AddsTheContextCancellationTokenAsync()
    {
        var source = Prefix + @"
namespace ConsoleApplication1
{
    class Consumer :
        IConsumer<SubmitOrder>
    {
        public Task ConsumeAsync(ConsumeContext<SubmitOrder> context)
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
        public Task ConsumeAsync(ConsumeContext<SubmitOrder> context)
        {
            return Task.Delay(10, context.CancellationToken);
        }
    }
}
";

        await AssertFixedSourceAsync(source, expected);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CANCELLATION-TOKEN-CODEFIX", "state-machine-activity-overloads")]
    public async Task ActivityFix_AddsEachMatchingBehaviorContextTokenAsync()
    {
        var source = Prefix + @"
namespace ConsoleApplication1
{

        class TestInstance :
            ISagaStateMachineInstance
        {
            public Guid CorrelationId { get; set; }
            public IState CurrentState { get; set; }
            public string Value { get; set; }
        }

        class SetValueAsyncActivity :
            IStateMachineActivity<TestInstance, SubmitOrder>
        {

        Task IStateMachineActivity<TestInstance, SubmitOrder>.ExecuteAsync(IBehaviorContext<TestInstance, SubmitOrder> context,
            IBehavior<TestInstance, SubmitOrder> next)
        {
            return Task.Delay(10);
        }

        Task IStateMachineActivity<TestInstance, SubmitOrder>.FaultedAsync<TException>(IBehaviorExceptionContext<TestInstance, SubmitOrder, TException> ctx,
            IBehavior<TestInstance, SubmitOrder> next)
        {
            return Task.Run(() => next.FaultedAsync(ctx));
        }

        public void Accept(IStateMachineVisitor visitor)
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
            ISagaStateMachineInstance
        {
            public Guid CorrelationId { get; set; }
            public IState CurrentState { get; set; }
            public string Value { get; set; }
        }

        class SetValueAsyncActivity :
            IStateMachineActivity<TestInstance, SubmitOrder>
        {

        Task IStateMachineActivity<TestInstance, SubmitOrder>.ExecuteAsync(IBehaviorContext<TestInstance, SubmitOrder> context,
            IBehavior<TestInstance, SubmitOrder> next)
        {
            return Task.Delay(10, context.CancellationToken);
        }

        Task IStateMachineActivity<TestInstance, SubmitOrder>.FaultedAsync<TException>(IBehaviorExceptionContext<TestInstance, SubmitOrder, TException> ctx,
            IBehavior<TestInstance, SubmitOrder> next)
        {
            return Task.Run(() => next.FaultedAsync(ctx), ctx.CancellationToken);
        }

        public void Accept(IStateMachineVisitor visitor)
        {
            visitor.Visit(this);
        }

        public void Probe(ProbeContext context)
        {
        }
    }
}
";

        await AssertFixedSourceAsync(source, expected);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CANCELLATION-TOKEN-CODEFIX", "later-task-run-overload")]
    public async Task TaskRunFix_PreservesExistingTokenUseAndAddsTheMissingOneAsync()
    {
        var source = Prefix + @"
namespace ConsoleApplication1
{
    class Consumer :
        IConsumer<SubmitOrder>
    {
        public async Task ConsumeAsync(ConsumeContext<SubmitOrder> context)
        {
            await Task.Delay(10, context.CancellationToken);
            context.RespondAsync<SubmitOrder>(context.Message);
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
        public async Task ConsumeAsync(ConsumeContext<SubmitOrder> context)
        {
            await Task.Delay(10, context.CancellationToken);
            context.RespondAsync<SubmitOrder>(context.Message);
            await Task.Run(() => {}, context.CancellationToken);

        }
    }
}
";

        await AssertFixedSourceAsync(source, expected);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CANCELLATION-TOKEN-CODEFIX", "named-arguments-remain-valid")]
    public async Task NamedArgumentFix_AddsANamedCancellationTokenAsync()
    {
        var source = Prefix + @"
namespace ConsoleApplication1
{
    class Consumer : IConsumer<SubmitOrder>
    {
        public Task ConsumeAsync(ConsumeContext<SubmitOrder> context)
        {
            return Task.Delay(millisecondsDelay: 10);
        }
    }
}
";
        var expected = Prefix + @"
namespace ConsoleApplication1
{
    class Consumer : IConsumer<SubmitOrder>
    {
        public Task ConsumeAsync(ConsumeContext<SubmitOrder> context)
        {
            return Task.Delay(millisecondsDelay: 10, cancellationToken: context.CancellationToken);
        }
    }
}
";

        await AssertFixedSourceAsync(source, expected);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CANCELLATION-TOKEN-CODEFIX", "extension-method-overload")]
    public async Task ExtensionMethodFix_AddsTheTokenAfterTheReducedArgumentsAsync()
    {
        var source = Prefix + @"
namespace ConsoleApplication1
{
    static class WorkExtensions
    {
        public static Task WorkAsync(this string value) => Task.CompletedTask;
        public static Task WorkAsync(this string value, System.Threading.CancellationToken cancellationToken) => Task.CompletedTask;
    }

    class Consumer : IConsumer<SubmitOrder>
    {
        public Task ConsumeAsync(ConsumeContext<SubmitOrder> context)
        {
            return ""value"".WorkAsync();
        }
    }
}
";
        var expected = Prefix + @"
namespace ConsoleApplication1
{
    static class WorkExtensions
    {
        public static Task WorkAsync(this string value) => Task.CompletedTask;
        public static Task WorkAsync(this string value, System.Threading.CancellationToken cancellationToken) => Task.CompletedTask;
    }

    class Consumer : IConsumer<SubmitOrder>
    {
        public Task ConsumeAsync(ConsumeContext<SubmitOrder> context)
        {
            return ""value"".WorkAsync(cancellationToken: context.CancellationToken);
        }
    }
}
";

        await AssertFixedSourceAsync(source, expected);
    }

    private static string Prefix =>
        ServiceBusCodeFixFixture.Usings + ServiceBusCodeFixFixture.SimpleMessageContracts;

    private static async Task AssertFixedSourceAsync(string source, string expected)
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
