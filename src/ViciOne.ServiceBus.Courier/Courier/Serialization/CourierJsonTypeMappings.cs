using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using ViciOne.ServiceBus.Advanced.Serialization;
using ViciOne.ServiceBus.Courier.Contracts;
using ViciOne.ServiceBus.Courier.Messages;

namespace ViciOne.ServiceBus.Courier.Serialization;

static class CourierJsonTypeMappings
{
    [SuppressMessage("Usage", "CA2255:The 'ModuleInitializer' attribute should not be used in libraries",
        Justification = "The optional capability must register its contract mappings before serializers inspect them.")]
    [ModuleInitializer]
    internal static void Register()
    {
        JsonMessageTypeMappingRegistry.Register<IRoutingSlip, RoutingSlipRoutingSlip>();
        JsonMessageTypeMappingRegistry.Register<IActivity, RoutingSlipActivity>();
        JsonMessageTypeMappingRegistry.Register<IActivityLog, RoutingSlipActivityLog>();
        JsonMessageTypeMappingRegistry.Register<ICompensateLog, RoutingSlipCompensateLog>();
        JsonMessageTypeMappingRegistry.Register<IActivityException, RoutingSlipActivityException>();
        JsonMessageTypeMappingRegistry.Register<ISubscription, RoutingSlipSubscription>();
        JsonMessageTypeMappingRegistry.Register<IRoutingSlipCompleted, RoutingSlipCompletedMessage>();
        JsonMessageTypeMappingRegistry.Register<IRoutingSlipFaulted, RoutingSlipFaultedMessage>();
        JsonMessageTypeMappingRegistry.Register<IRoutingSlipActivityCompleted, RoutingSlipActivityCompletedMessage>();
        JsonMessageTypeMappingRegistry.Register<IRoutingSlipActivityFaulted, RoutingSlipActivityFaultedMessage>();
        JsonMessageTypeMappingRegistry.Register<IRoutingSlipActivityCompensated, RoutingSlipActivityCompensatedMessage>();
        JsonMessageTypeMappingRegistry.Register<IRoutingSlipActivityCompensationFailed, RoutingSlipActivityCompensationFailedMessage>();
        JsonMessageTypeMappingRegistry.Register<IRoutingSlipCompensationFailed, RoutingSlipCompensationFailedMessage>();
        JsonMessageTypeMappingRegistry.Register<IRoutingSlipTerminated, RoutingSlipTerminatedMessage>();
        JsonMessageTypeMappingRegistry.Register<IRoutingSlipRevised, RoutingSlipRevisedMessage>();
    }
}
