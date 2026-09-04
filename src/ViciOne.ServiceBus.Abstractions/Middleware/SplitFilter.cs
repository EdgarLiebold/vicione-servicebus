using System.Diagnostics;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>
/// Provides a split filter implementation.
/// </summary>
/// <typeparam name="TInput">The t input type.</typeparam>
/// <typeparam name="TSplit">The t split type.</typeparam>
public class SplitFilter<TInput, TSplit> :
    IFilter<TInput>
    where TSplit : class, PipeContext
    where TInput : class, PipeContext
{
    readonly MergeFilterContextProvider<TInput, TSplit> _contextProvider;
    readonly FilterContextProvider<TSplit, TInput> _inputContextProvider;
    readonly IFilter<TSplit> _split;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="split">The split value.</param>
    /// <param name="contextProvider">The context provider value.</param>
    /// <param name="inputContextProvider">The input context provider value.</param>
    public SplitFilter(IFilter<TSplit> split, MergeFilterContextProvider<TInput, TSplit> contextProvider,
        FilterContextProvider<TSplit, TInput> inputContextProvider)
    {
        _split = split;
        _contextProvider = contextProvider;
        _inputContextProvider = inputContextProvider;
    }

    /// <summary>
    /// Performs the probe operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    public void Probe(ProbeContext context)
    {
        var scope = context.CreateFilterScope("split");
        scope.Set(new { SplitType = TypeCache<TSplit>.ShortName });

        _split.Probe(scope);
    }

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="next">The next value.</param>
    /// <returns>The result of the operation.</returns>
    [DebuggerNonUserCode]
    public Task SendAsync(TInput context, IPipe<TInput> next)
    {
        var mergePipe = new MergePipe<TInput, TSplit>(next, context, _contextProvider);

        return _split.SendAsync(_inputContextProvider(context), mergePipe);
    }
}
