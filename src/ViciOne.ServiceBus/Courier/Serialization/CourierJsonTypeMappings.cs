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
        JsonMessageTypeMappingRegistry.Register<RoutingSlip, RoutingSlipRoutingSlip>();
        JsonMessageTypeMappingRegistry.Register<Activity, RoutingSlipActivity>();
        JsonMessageTypeMappingRegistry.Register<ActivityLog, RoutingSlipActivityLog>();
        JsonMessageTypeMappingRegistry.Register<CompensateLog, RoutingSlipCompensateLog>();
        JsonMessageTypeMappingRegistry.Register<ActivityException, RoutingSlipActivityException>();
        JsonMessageTypeMappingRegistry.Register<Subscription, RoutingSlipSubscription>();
        JsonMessageTypeMappingRegistry.Register<RoutingSlipCompleted, RoutingSlipCompletedMessage>();
        JsonMessageTypeMappingRegistry.Register<RoutingSlipFaulted, RoutingSlipFaultedMessage>();
        JsonMessageTypeMappingRegistry.Register<RoutingSlipActivityCompleted, RoutingSlipActivityCompletedMessage>();
        JsonMessageTypeMappingRegistry.Register<RoutingSlipActivityFaulted, RoutingSlipActivityFaultedMessage>();
        JsonMessageTypeMappingRegistry.Register<RoutingSlipActivityCompensated, RoutingSlipActivityCompensatedMessage>();
        JsonMessageTypeMappingRegistry.Register<RoutingSlipActivityCompensationFailed, RoutingSlipActivityCompensationFailedMessage>();
        JsonMessageTypeMappingRegistry.Register<RoutingSlipCompensationFailed, RoutingSlipCompensationFailedMessage>();
        JsonMessageTypeMappingRegistry.Register<RoutingSlipTerminated, RoutingSlipTerminatedMessage>();
        JsonMessageTypeMappingRegistry.Register<RoutingSlipRevised, RoutingSlipRevisedMessage>();
    }
}
