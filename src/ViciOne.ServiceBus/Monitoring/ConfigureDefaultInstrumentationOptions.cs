namespace ViciOne.ServiceBus.Monitoring
{
    using Metadata;
    using Microsoft.Extensions.Options;


    public class ConfigureDefaultInstrumentationOptions :
        IConfigureOptions<InstrumentationOptions>
    {
        public void Configure(InstrumentationOptions options)
        {
            options.ServiceName = HostMetadataCache.Host.ProcessName;
            options.EndpointLabel = "messaging.vicione-servicebus.destination";
            options.ConsumerTypeLabel = "messaging.vicione-servicebus.consumer_type";
            options.ExceptionTypeLabel = "messaging.vicione-servicebus.exception_type";
            options.MessageTypeLabel = "messaging.vicione-servicebus.message_type";
            options.ActivityNameLabel = "messaging.vicione-servicebus.activity_type";
            options.ArgumentTypeLabel = "messaging.vicione-servicebus.argument_type";
            options.LogTypeLabel = "messaging.vicione-servicebus.log_type";
            options.ServiceNameLabel = "messaging.vicione-servicebus.service";
            options.ReceiveTotal = "messaging.vicione-servicebus.receive";
            options.ReceiveFaultTotal = "messaging.vicione-servicebus.receive.errors";
            options.ReceiveDuration = "messaging.vicione-servicebus.receive.duration";
            options.ReceiveInProgress = "messaging.vicione-servicebus.receive.active";
            options.ConsumeTotal = "messaging.vicione-servicebus.consume";
            options.ConsumeFaultTotal = "messaging.vicione-servicebus.consume.errors";
            options.ConsumeRetryTotal = "messaging.vicione-servicebus.consume.retries";
            options.ConsumeDuration = "messaging.vicione-servicebus.consume.duration";
            options.ConsumerInProgress = "messaging.vicione-servicebus.consume.active";
            options.SagaTotal = "messaging.vicione-servicebus.saga";
            options.SagaFaultTotal = "messaging.vicione-servicebus.saga.errors";
            options.SagaDuration = "messaging.vicione-servicebus.saga.duration";
            options.HandlerTotal = "messaging.vicione-servicebus.handler";
            options.HandlerFaultTotal = "messaging.vicione-servicebus.handler.errors";
            options.HandlerDuration = "messaging.vicione-servicebus.handler.duration";
            options.OutboxDeliveryTotal = "messaging.vicione-servicebus.outbox.delivery";
            options.OutboxDeliveryFaultTotal = "messaging.vicione-servicebus.outbox.delivery.errors";
            options.DeliveryDuration = "messaging.vicione-servicebus.delivery.duration";
            options.SendTotal = "messaging.vicione-servicebus.send";
            options.SendFaultTotal = "messaging.vicione-servicebus.send.errors";
            options.OutboxSendTotal = "messaging.vicione-servicebus.outbox.send";
            options.OutboxSendFaultTotal = "messaging.vicione-servicebus.outbox.send.errors";
            options.ActivityExecuteTotal = "messaging.vicione-servicebus.execute";
            options.ActivityExecuteFaultTotal = "messaging.vicione-servicebus.execute.errors";
            options.ActivityExecuteDuration = "messaging.vicione-servicebus.execute.duration";
            options.ExecuteInProgress = "messaging.vicione-servicebus.execute.active";
            options.ActivityCompensateTotal = "messaging.vicione-servicebus.compensate";
            options.ActivityCompensateFailureTotal = "messaging.vicione-servicebus.compensate.errors";
            options.ActivityCompensateDuration = "messaging.vicione-servicebus.compensate.duration";
            options.CompensateInProgress = "messaging.vicione-servicebus.compensate.active";
            options.BusInstances = "messaging.vicione-servicebus.bus";
            options.EndpointInstances = "messaging.vicione-servicebus.endpoint";
            options.HandlerInProgress = "messaging.vicione-servicebus.handler.active";
            options.SagaInProgress = "messaging.vicione-servicebus.saga.active";
        }
    }
}
