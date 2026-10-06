using System.Diagnostics;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>Processes split pipeline stages.</summary>
/// <typeparam name="TInput">The input type.</typeparam>
/// <typeparam name="TSplit">The split type.</typeparam>
public class SplitFilter<TInput, TSplit> :
    IFilter<TInput>
    where TSplit : class, PipeContext
    where TInput : class, PipeContext
{
    readonly MergeFilterContextProvider<TInput, TSplit> _contextProvider;
    readonly FilterContextProvider<TSplit, TInput> _inputContextProvider;
    readonly IFilter<TSplit> _split;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="split">The split.</param>
    /// <param name="contextProvider">The context provider.</param>
    /// <param name="inputContextProvider">The input context provider.</param>
    public SplitFilter(IFilter<TSplit> split, MergeFilterContextProvider<TInput, TSplit> contextProvider,
        FilterContextProvider<TSplit, TInput> inputContextProvider)
    {
        _split = split;
        _contextProvider = contextProvider;
        _inputContextProvider = inputContextProvider;
    }

    /// <summary>Writes diagnostic information to the probe context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    public void Probe(ProbeContext context)
    {
        var scope = context.CreateFilterScope("split");
        scope.Set(new { SplitType = TypeCache<TSplit>.ShortName });

        _split.Probe(scope);
    }

    /// <summary>Projects the input context for the retained filter and supplies a merge continuation.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [DebuggerNonUserCode]
    public Task SendAsync(TInput context, IPipe<TInput> next)
    {
        var mergePipe = new MergePipe<TInput, TSplit>(next, context, _contextProvider);

        return _split.SendAsync(_inputContextProvider(context), mergePipe);
    }
}
