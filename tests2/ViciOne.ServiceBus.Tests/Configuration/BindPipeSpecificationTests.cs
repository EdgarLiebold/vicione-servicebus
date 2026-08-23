using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Configuration;

public sealed class BindPipeSpecificationTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-PIPE-CONTEXT-BINDING", "source-context-and-order")]
    public async Task SourceContext_IsBoundBeforeTheFollowingPipeSegment()
    {
        var trace = new List<string>();
        var input = new InputContext("Input");
        var source = new ThingSource("Rock!");
        InputContext? contextPipeInput = null;
        InputContext? followingInput = null;
        BindContext<InputContext, Thing>? boundContext = null;

        IPipe<InputContext> pipe = Pipe.New<InputContext>(configurator =>
        {
            configurator.UseBind(bind => bind.Source(source, target =>
            {
                target.ContextPipe.UseExecute(context =>
                {
                    contextPipeInput = context;
                    trace.Add("context");
                });
                target.UseExecute(context =>
                {
                    boundContext = context;
                    trace.Add("bound");
                });
            }));
            configurator.UseExecute(context =>
            {
                followingInput = context;
                trace.Add("next");
            });
        });

        await pipe.Send(input);

        Assert.Same(input, contextPipeInput);
        Assert.Same(input, followingInput);
        Assert.Same(input, source.ReceivedInput);
        Assert.NotNull(boundContext);
        Assert.Same(input, boundContext.Left);
        Assert.Equal("Input", boundContext.Left.Value);
        Assert.Same(source.CreatedContext, boundContext.Right);
        Assert.Equal("Rock!", boundContext.Right.Value);
        Assert.Equal(["context", "bound", "next"], trace);
    }

    private sealed class InputContext(string value) : BasePipeContext
    {
        public string Value { get; } = value;
    }

    private sealed class Thing : BasePipeContext
    {
        public required string Value { get; init; }
    }

    private sealed class ThingSource(string value) : IPipeContextSource<Thing, InputContext>
    {
        public InputContext? ReceivedInput { get; private set; }

        public Thing? CreatedContext { get; private set; }

        public Task Send(InputContext context, IPipe<Thing> pipe)
        {
            ReceivedInput = context;
            CreatedContext = new Thing { Value = value };

            return pipe.Send(CreatedContext);
        }

        public void Probe(ProbeContext context)
        {
        }
    }
}
