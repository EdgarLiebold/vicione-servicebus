namespace ViciOne.ServiceBus.Logging
{
    using System.Diagnostics.Metrics;
    using Monitoring;


    internal sealed class LogContextInstrumentationState
    {
        public LogContextInstrumentationState(Meter meter, InstrumentationOptions options, ILogContext rootLogContext)
        {
            Meter = meter;
            Options = options;
            RootLogContext = rootLogContext;

            ReceiveTotal = meter.CreateCounter<long>(options.ReceiveTotal, "ea", "Number of messages received");
            ReceiveFaultTotal = meter.CreateCounter<long>(options.ReceiveFaultTotal, "ea", "Number of messages receive faults");
            ConsumeTotal = meter.CreateCounter<long>(options.ConsumeTotal, "ea", "Number of messages consumed");
            ConsumeFaultTotal = meter.CreateCounter<long>(options.ConsumeFaultTotal, "ea", "Number of message consume faults");
            ConsumeRetryTotal = meter.CreateCounter<long>(options.ConsumeRetryTotal, "ea", "Number of message consume retries");
            SagaTotal = meter.CreateCounter<long>(options.SagaTotal, "ea", "Number of sagas executed");
            SagaFaultTotal = meter.CreateCounter<long>(options.SagaFaultTotal, "ea", "Number of sagas faults");
            HandlerTotal = meter.CreateCounter<long>(options.HandlerTotal, "ea", "Number of messages handled");
            HandlerFaultTotal = meter.CreateCounter<long>(options.HandlerFaultTotal, "ea", "Number of message handler faults");
            SendTotal = meter.CreateCounter<long>(options.SendTotal, "ea", "Number of messages sent");
            SendFaultTotal = meter.CreateCounter<long>(options.SendFaultTotal, "ea", "Number of message send faults");
            OutboxSendTotal = meter.CreateCounter<long>(options.OutboxSendTotal, "ea", "Number of messages sent to outbox");
            OutboxSendFaultTotal = meter.CreateCounter<long>(options.OutboxSendFaultTotal, "ea", "Number of message send to outbox faults");
            ExecuteTotal = meter.CreateCounter<long>(options.ActivityExecuteTotal, "ea", "Number of activities executed");
            ExecuteFaultTotal = meter.CreateCounter<long>(options.ActivityExecuteFaultTotal, "ea", "Number of activity execution faults");
            CompensateTotal = meter.CreateCounter<long>(options.ActivityCompensateTotal, "ea", "Number of activities compensated");
            CompensateFaultTotal = meter.CreateCounter<long>(options.ActivityCompensateFailureTotal, "ea", "Number of activity compensation failures");
            OutboxDeliveryTotal = meter.CreateCounter<long>(options.OutboxDeliveryTotal, "ea", "Number of outbox delivery messages executed");
            OutboxDeliveryFaultTotal = meter.CreateCounter<long>(options.OutboxDeliveryFaultTotal, "ea", "Number of outbox delivery message failures");

            ReceiveInProgress = meter.CreateCounter<long>(options.ReceiveInProgress, "ea", "Number of messages being received");
            HandlerInProgress = meter.CreateCounter<long>(options.HandlerInProgress, "ea", "Number of handlers in progress");
            ConsumerInProgress = meter.CreateCounter<long>(options.ConsumerInProgress, "ea", "Number of consumers in progress");
            SagaInProgress = meter.CreateCounter<long>(options.SagaInProgress, "ea", "Number of sagas in progress");
            ExecuteInProgress = meter.CreateCounter<long>(options.ExecuteInProgress, "ea", "Number of activity executions in progress");
            CompensateInProgress = meter.CreateCounter<long>(options.CompensateInProgress, "ea", "Number of activity compensations in progress");

            ReceiveDuration = meter.CreateHistogram<double>(options.ReceiveDuration, "ms", "Elapsed time spent receiving a message, in millis");
            ConsumeDuration = meter.CreateHistogram<double>(options.ConsumeDuration, "ms", "Elapsed time spent consuming a message, in millis");
            SagaDuration = meter.CreateHistogram<double>(options.SagaDuration, "ms", "Elapsed time spent saga processing a message, in millis");
            HandlerDuration = meter.CreateHistogram<double>(options.HandlerDuration, "ms", "Elapsed time spent handler processing a message, in millis");
            DeliveryDuration = meter.CreateHistogram<double>(options.DeliveryDuration, "ms",
                "Elapsed time between when the message was sent and when it was consumed, in millis.");
            ExecuteDuration = meter.CreateHistogram<double>(options.ActivityExecuteDuration, "ms", "Elapsed time spent executing an activity, in millis");
            CompensateDuration = meter.CreateHistogram<double>(options.ActivityCompensateDuration, "ms",
                "Elapsed time spent compensating an activity, in millis");
        }

        public Meter Meter { get; }
        public InstrumentationOptions Options { get; }

        /// <summary>
        /// The log context this scope owns. Every provider has its own instance, so activating a provider never
        /// rebinds a root instance another provider already uses.
        /// </summary>
        public ILogContext RootLogContext { get; }
        public Counter<long> ReceiveTotal { get; }
        public Counter<long> ReceiveFaultTotal { get; }
        public Counter<long> ReceiveInProgress { get; }
        public Counter<long> ConsumeTotal { get; }
        public Counter<long> ConsumeFaultTotal { get; }
        public Counter<long> ConsumeRetryTotal { get; }
        public Counter<long> SagaTotal { get; }
        public Counter<long> SagaFaultTotal { get; }
        public Counter<long> SendTotal { get; }
        public Counter<long> SendFaultTotal { get; }
        public Counter<long> ExecuteTotal { get; }
        public Counter<long> ExecuteFaultTotal { get; }
        public Counter<long> CompensateTotal { get; }
        public Counter<long> CompensateFaultTotal { get; }
        public Counter<long> ConsumerInProgress { get; }
        public Counter<long> HandlerTotal { get; }
        public Counter<long> HandlerFaultTotal { get; }
        public Counter<long> HandlerInProgress { get; }
        public Counter<long> SagaInProgress { get; }
        public Counter<long> ExecuteInProgress { get; }
        public Counter<long> CompensateInProgress { get; }
        public Counter<long> OutboxSendTotal { get; }
        public Counter<long> OutboxSendFaultTotal { get; }
        public Counter<long> OutboxDeliveryTotal { get; }
        public Counter<long> OutboxDeliveryFaultTotal { get; }
        public Histogram<double> ReceiveDuration { get; }
        public Histogram<double> ConsumeDuration { get; }
        public Histogram<double> HandlerDuration { get; }
        public Histogram<double> SagaDuration { get; }
        public Histogram<double> DeliveryDuration { get; }
        public Histogram<double> ExecuteDuration { get; }
        public Histogram<double> CompensateDuration { get; }
    }
}
