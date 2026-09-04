using System.Threading;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Initializers.Contexts;

public class BaseInitializeContext :
    BasePipeContext,
    InitializeContext
{
    public BaseInitializeContext(CancellationToken cancellationToken)
        : base(cancellationToken)
    {
    }

    public virtual int Depth => 0;

    public virtual InitializeContext? Parent => null;

    public virtual bool TryGetParent<T>([NotNullWhen(true)] out InitializeContext<T>? parentContext)
        where T : class
    {
        parentContext = default;
        return false;
    }

    public InitializeContext<T> CreateMessageContext<T>(T message)
        where T : class
    {
        return new DynamicInitializeContext<T>(this, message);
    }
}
