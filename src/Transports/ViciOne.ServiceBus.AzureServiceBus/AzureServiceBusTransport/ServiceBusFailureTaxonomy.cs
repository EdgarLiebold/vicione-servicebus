using Azure.Messaging.ServiceBus;

namespace ViciOne.ServiceBus.AzureServiceBus;

internal static class ServiceBusFailureTaxonomy
{
    internal static bool CanRetryBrokerFailure(ServiceBusException exception, bool receive) => exception.Reason switch
    {
        ServiceBusFailureReason.MessagingEntityDisabled => receive,
        ServiceBusFailureReason.MessagingEntityNotFound => !receive,
        ServiceBusFailureReason.MessagingEntityAlreadyExists => !receive,
        ServiceBusFailureReason.MessageNotFound => false,
        ServiceBusFailureReason.MessageSizeExceeded => false,
        ServiceBusFailureReason.ServiceCommunicationProblem => exception.IsTransient,
        ServiceBusFailureReason.ServiceBusy => exception.IsTransient,
        ServiceBusFailureReason.ServiceTimeout => exception.IsTransient,
        ServiceBusFailureReason.GeneralError => exception.IsTransient,
        _ => false
    };
}
