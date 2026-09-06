namespace ViciOne.ServiceBus.Logging;

/// <summary>
/// Provides an operation name implementation.
/// </summary>
public static class OperationName
{
    /// <summary>
    /// Provides a consumer implementation.
    /// </summary>
    public static class Consumer
    {
        /// <summary>
        /// Defines the consume value.
        /// </summary>
        public const string Consume = "ViciOne.ServiceBus.Consumer.Consume";
        /// <summary>
        /// Defines the handle value.
        /// </summary>
        public const string Handle = "ViciOne.ServiceBus.Consumer.Handle";
    }


    /// <summary>
    /// Provides a saga implementation.
    /// </summary>
    public static class Saga
    {
        /// <summary>
        /// Defines the send value.
        /// </summary>
        public const string Send = "ViciOne.ServiceBus.Saga.Send";
        /// <summary>
        /// Defines the send query value.
        /// </summary>
        public const string SendQuery = "ViciOne.ServiceBus.Saga.SendQuery";
        /// <summary>
        /// Defines the initiate value.
        /// </summary>
        public const string Initiate = "ViciOne.ServiceBus.Saga.Initiate";
        /// <summary>
        /// Defines the orchestrate value.
        /// </summary>
        public const string Orchestrate = "ViciOne.ServiceBus.Saga.Orchestrate";
        /// <summary>
        /// Defines the initiate or orchestrate value.
        /// </summary>
        public const string InitiateOrOrchestrate = "ViciOne.ServiceBus.Saga.InitiateOrOrchestrate";
        /// <summary>
        /// Defines the observe value.
        /// </summary>
        public const string Observe = "ViciOne.ServiceBus.Saga.Observe";
        /// <summary>
        /// Defines the raise event value.
        /// </summary>
        public const string RaiseEvent = "ViciOne.ServiceBus.Saga.RaiseEvent";
    }


    /// <summary>
    /// Provides a courier implementation.
    /// </summary>
    public static class Courier
    {
        /// <summary>
        /// Defines the execute value.
        /// </summary>
        public const string Execute = "ViciOne.ServiceBus.Activity.Execute";
        /// <summary>
        /// Defines the compensate value.
        /// </summary>
        public const string Compensate = "ViciOne.ServiceBus.Activity.Compensate";
    }
}
