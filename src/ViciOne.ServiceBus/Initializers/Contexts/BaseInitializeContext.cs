using System.Threading;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Initializers.Contexts;

/// <summary>Represents the root of an initialization graph created from a cancellation token.</summary>
internal sealed class BaseInitializeContext :
    BasePipeContext,
    InitializeContext
{
    /// <summary>Creates a root context with the supplied cancellation state.</summary>
    /// <param name="cancellationToken">The token exposed through the pipeline context.</param>
    public BaseInitializeContext(CancellationToken cancellationToken)
        : base(cancellationToken)
    {
    }

    /// <summary>Gets the root depth, which is always zero.</summary>
    public int Depth => 0;

    /// <summary>Gets no parent because this context is the graph root.</summary>
    public InitializeContext? Parent => null;

    /// <summary>Reports that a root context has no typed parent.</summary>
    /// <typeparam name="T">The requested parent message contract.</typeparam>
    /// <param name="parentContext">Receives <see langword="null"/>.</param>
    /// <returns>Always <see langword="false"/>.</returns>
    public bool TryGetParent<T>([NotNullWhen(true)] out InitializeContext<T>? parentContext)
        where T : class
    {
        parentContext = default;
        return false;
    }

    /// <summary>Creates the first typed message node in the initialization graph.</summary>
    /// <typeparam name="T">The message contract.</typeparam>
    /// <param name="message">The message instance owned by the new node.</param>
    /// <returns>A child context for <paramref name="message"/>.</returns>
    public InitializeContext<T> CreateMessageContext<T>(T message)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(message);
        return new DynamicInitializeContext<T>(this, message);
    }
}
