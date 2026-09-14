using System;
using System.Reflection;
using System.Runtime.ExceptionServices;
using ViciOne.ServiceBus.Internals.Reflection;

namespace ViciOne.ServiceBus.DependencyInjection;

internal sealed class BusInstanceBuilder :
    IBusInstanceBuilder
{
    public static readonly IBusInstanceBuilder Instance = new BusInstanceBuilder();

    private readonly DynamicImplementationBuilder _implementationBuilder;

    private BusInstanceBuilder()
    {
        _implementationBuilder = DynamicImplementationBuilder.Instance;
    }

    public TResult GetBusInstanceType<TBus, TResult>(IBusInstanceBuilderCallback<TBus, TResult> callback)
        where TBus : class, IBus
    {
        ArgumentNullException.ThrowIfNull(callback);

        Type busInstanceType = _implementationBuilder.GetBusInstanceType(typeof(TBus));

        try
        {
            MethodInfo getResult = typeof(IBusInstanceBuilderCallback<TBus, TResult>).GetMethod("GetResult")
                ?? throw new InvalidOperationException("The bus instance builder callback does not expose GetResult.");

            return (TResult)(getResult
                .MakeGenericMethod(busInstanceType)
                .Invoke(callback, [])
                ?? throw new InvalidOperationException("The bus instance builder callback returned null."));
        }
        catch (TargetInvocationException exception) when (exception.InnerException != null)
        {
            ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
            throw;
        }
    }
}
