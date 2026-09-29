using System.Reflection;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Middleware;

public sealed class RethrowErrorTransportFilterTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-RECEIVE-FAULT", "rethrow-defers-fault-notification-to-dispatcher")]
    public async Task RethrowFilter_LeavesReceiveFaultReportingToDispatcherAsync(bool alreadyFaulted)
    {
        var receiveFailure = new InvalidOperationException("receive failed");
        var downstreamCalls = 0;
        ExceptionReceiveContext context = StrictProxy.Create<ExceptionReceiveContext>((method, _) => method.Name switch
        {
            "get_IsFaulted" => alreadyFaulted,
            "get_Exception" => receiveFailure,
            _ => throw new NotSupportedException(method.Name),
        });
        IPipe<ExceptionReceiveContext> next = Pipe.Execute<ExceptionReceiveContext>(_ => downstreamCalls++);
        var filter = new RethrowErrorTransportFilter();

        Exception actual = await Record.ExceptionAsync(() => filter.SendAsync(context, next))
            ?? throw new Xunit.Sdk.XunitException("The failed receive completed successfully.");

        Assert.Same(receiveFailure, actual);
        Assert.Equal(0, downstreamCalls);
    }

    public class StrictProxy : DispatchProxy
    {
        private Func<MethodInfo, object?[]?, object?> _invoke = null!;

        public static T Create<T>(Func<MethodInfo, object?[]?, object?> invoke) where T : class
        {
            T instance = Create<T, StrictProxy>();
            ((StrictProxy)(object)instance)._invoke = invoke;
            return instance;
        }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            _invoke(targetMethod ?? throw new ArgumentNullException(nameof(targetMethod)), args);
    }
}
