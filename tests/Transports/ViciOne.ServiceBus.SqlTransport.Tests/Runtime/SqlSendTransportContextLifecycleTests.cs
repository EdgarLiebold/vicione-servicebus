using System.Reflection;
using ViciOne.ServiceBus.SqlTransport;
using ViciOne.ServiceBus.SqlTransport.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports;
using Xunit;

namespace ViciOne.ServiceBus.SqlTransport.Tests.Runtime;

public sealed class SqlSendTransportContextLifecycleTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-SQL-SEND-LIFECYCLE", "queue-and-topic-own-their-client-supervisor")]
    public void SendTransport_OwnsAndProbesItsClientSupervisor(bool topic)
    {
        ISerialization serialization = InterfaceProxy<ISerialization>.Create(static (_, _) => null);
        ReceiveEndpointContext receiveEndpointContext = InterfaceProxy<ReceiveEndpointContext>.Create((method, _) => method.Name switch
        {
            "get_Serialization" => serialization,
            _ => Default(method.ReturnType),
        });
        ISqlHostConfiguration hostConfiguration = InterfaceProxy<ISqlHostConfiguration>.Create((method, _) => Default(method.ReturnType));
        ProbeContext? observedProbe = null;
        IClientContextSupervisor supervisor = InterfaceProxy<IClientContextSupervisor>.Create((method, args) => method.Name switch
        {
            "Probe" => RecordProbe(args, probe => observedProbe = probe),
            _ => Default(method.ReturnType),
        });
        IPipe<ClientContext> topologyPipe = InterfaceProxy<IPipe<ClientContext>>.Create((method, _) => Default(method.ReturnType));

        SendTransportContext<ClientContext> transport = topic
            ? new TopicSendTransportContext(hostConfiguration, receiveEndpointContext, supervisor, topologyPipe, "orders")
            : new QueueSendTransportContext(hostConfiguration, receiveEndpointContext, supervisor, topologyPipe, "orders");
        ProbeContext probeContext = InterfaceProxy<ProbeContext>.Create((method, _) => Default(method.ReturnType));

        Assert.Same(supervisor, Assert.Single(transport.GetAgentHandles()));
        ((IProbeSite)transport).Probe(probeContext);
        Assert.Same(probeContext, observedProbe);
    }

    private static object? Default(Type returnType) => returnType.IsValueType ? Activator.CreateInstance(returnType) : null;

    private static object? RecordProbe(object?[]? arguments, Action<ProbeContext> record)
    {
        record(Assert.IsAssignableFrom<ProbeContext>(arguments![0]));
        return null;
    }

    private class InterfaceProxy<T> : DispatchProxy
        where T : class
    {
        Func<MethodInfo, object?[]?, object?> _handler = null!;

        public static T Create(Func<MethodInfo, object?[]?, object?> handler)
        {
            T proxy = Create<T, InterfaceProxy<T>>();
            ((InterfaceProxy<T>)(object)proxy)._handler = handler;
            return proxy;
        }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);
            return _handler(targetMethod, args);
        }
    }
}
