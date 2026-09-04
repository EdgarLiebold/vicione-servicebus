using System.Reflection;

namespace ViciOne.ServiceBus.ActiveMq.Tests.TestDoubles;

internal class InterfaceProxy<T> : DispatchProxy
    where T : class
{
    private Func<MethodInfo, object?[]?, object?>? _handler;

    internal static T Create(Func<MethodInfo, object?[]?, object?> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        T value = Create<T, InterfaceProxy<T>>();
        ((InterfaceProxy<T>)(object)value)._handler = handler;
        return value;
    }

    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
        (_handler ?? throw new InvalidOperationException("The proxy handler has not been configured."))(
            targetMethod ?? throw new InvalidOperationException("The invoked interface method is unavailable."),
            args);
}
