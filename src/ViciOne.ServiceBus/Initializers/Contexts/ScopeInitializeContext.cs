using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Initializers.Contexts;

/// <summary>
/// Provides a scope initialize context implementation.
/// </summary>
public class ScopeInitializeContext :
    ScopePipeContext,
    InitializeContext
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="context">The operation context.</param>
    public ScopeInitializeContext(PipeContext context)
        : base(context)
    {
    }

    /// <summary>
    /// Gets the depth value.
    /// </summary>
    public virtual int Depth => 0;

    /// <summary>
    /// Gets the parent value.
    /// </summary>
    public virtual InitializeContext? Parent => null;

    /// <summary>
    /// Attempts to get parent.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="parentContext">The parent context value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public virtual bool TryGetParent<T>([NotNullWhen(true)] out InitializeContext<T>? parentContext)
        where T : class
    {
        parentContext = default;
        return false;
    }

    /// <summary>
    /// Creates message context.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="message">The message value.</param>
    /// <returns>The result of the operation.</returns>
    public InitializeContext<T> CreateMessageContext<T>(T message)
        where T : class
    {
        return new DynamicInitializeContext<T>(this, message);
    }
}
