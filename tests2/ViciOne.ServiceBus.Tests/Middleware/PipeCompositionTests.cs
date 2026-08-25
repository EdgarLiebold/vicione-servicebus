using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Middleware;

public sealed class PipeCompositionTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-PIPE-COMPOSITION", "nested-pipe-completes-before-parent")]
    public async Task NestedPipe_CompletesBeforeTheParentContinues()
    {
        var trace = new List<string>();
        IPipe<ParentContext> parent = Pipe.New<ParentContext>(configuration =>
        {
            configuration.UseExecuteAsync(async _ =>
            {
                IPipe<ChildContext> child = Pipe.Execute<ChildContext>(_ => trace.Add("child"));
                await child.Send(new ChildContext());
                trace.Add("parent");
            });
        });

        await parent.Send(new ParentContext());

        Assert.Equal(["child", "parent"], trace);
    }

    private sealed class ParentContext : BasePipeContext;

    private sealed class ChildContext : BasePipeContext;
}
