using System.Threading;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Initializers.Contexts;

/// <summary>Carries state for base initialize operations.</summary>
public class BaseInitializeContext :
    BasePipeContext,
    InitializeContext
{
    /// <summary>Initializes a new instance.</summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    public BaseInitializeContext(CancellationToken cancellationToken)
        : base(cancellationToken)
    {
    }

    /// <summary>Gets the depth.</summary>
    public virtual int Depth => 0;

    /// <summary>Gets the parent.</summary>
    public virtual InitializeContext? Parent => null;

    /// <summary>Attempts to get parent.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="parentContext">Receives the parent context produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public virtual bool TryGetParent<T>([NotNullWhen(true)] out InitializeContext<T>? parentContext)
        where T : class
    {
        parentContext = default;
        return false;
    }

    /// <summary>Creates message context.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="message">The message to process.</param>
    /// <returns>The created message context.</returns>
    public InitializeContext<T> CreateMessageContext<T>(T message)
        where T : class
    {
        return new DynamicInitializeContext<T>(this, message);
    }
}
