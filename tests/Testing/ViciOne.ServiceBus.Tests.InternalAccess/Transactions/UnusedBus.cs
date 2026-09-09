using System.Reflection;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Tests.InternalAccess.Transactions;

internal static class UnusedBus
{
    public static IBus Create() => DispatchProxy.Create<IBus, UnexpectedBusProxy>();

    public static ISendEndpoint CreateSendEndpoint() => DispatchProxy.Create<ISendEndpoint, UnexpectedBusProxy>();

    public static ITransportSendEndpoint CreateTransportSendEndpoint() =>
        DispatchProxy.Create<ITransportSendEndpoint, UnexpectedBusProxy>();

    private class UnexpectedBusProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new InvalidOperationException($"The test-only bus member '{targetMethod?.Name}' was not expected to be called.");
    }
}
