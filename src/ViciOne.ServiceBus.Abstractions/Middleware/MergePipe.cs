using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>
/// Provides a merge pipe implementation.
/// </summary>
/// <typeparam name="TInput">The t input type.</typeparam>
/// <typeparam name="TSplit">The t split type.</typeparam>
public class MergePipe<TInput, TSplit> :
    IPipe<TSplit>
    where TSplit : class, PipeContext
    where TInput : class, PipeContext
{
    readonly MergeFilterContextProvider<TInput, TSplit> _contextProvider;
    readonly TInput _input;
    readonly IPipe<TInput> _next;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="next">The next value.</param>
    /// <param name="input">The input value.</param>
    /// <param name="contextProvider">The context provider value.</param>
    public MergePipe(IPipe<TInput> next, TInput input, MergeFilterContextProvider<TInput, TSplit> contextProvider)
    {
        _next = next;
        _input = input;
        _contextProvider = contextProvider;
    }

    /// <summary>
    /// Performs the probe operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    public void Probe(ProbeContext context)
    {
        var scope = context.CreateFilterScope("merge");
        scope.Set(new { InputType = TypeCache<TInput>.ShortName });

        _next.Probe(scope);
    }

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <returns>The result of the operation.</returns>
    public Task SendAsync(TSplit context)
    {
        var inputContext = _contextProvider(_input, context);

        return _next.SendAsync(inputContext);
    }
}
