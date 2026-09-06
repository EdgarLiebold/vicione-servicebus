namespace ViciOne.ServiceBus.Logging;

/// <summary>Represents the canonical name for operation.</summary>
public static class OperationName
{
    /// <summary>Defines diagnostic operation names for message consumers.</summary>
    public static class Consumer
    {
        /// <summary>Exposes the consume used by the containing type.</summary>
        public const string Consume = "ViciOne.ServiceBus.Consumer.Consume";
        /// <summary>Exposes the handle used by the containing type.</summary>
        public const string Handle = "ViciOne.ServiceBus.Consumer.Handle";
    }


    /// <summary>Defines diagnostic operation names for saga activities.</summary>
    public static class Saga
    {
        /// <summary>Exposes the send used by the containing type.</summary>
        public const string Send = "ViciOne.ServiceBus.Saga.Send";
        /// <summary>Exposes the send query used by the containing type.</summary>
        public const string SendQuery = "ViciOne.ServiceBus.Saga.SendQuery";
        /// <summary>Exposes the initiate used by the containing type.</summary>
        public const string Initiate = "ViciOne.ServiceBus.Saga.Initiate";
        /// <summary>Exposes the orchestrate used by the containing type.</summary>
        public const string Orchestrate = "ViciOne.ServiceBus.Saga.Orchestrate";
        /// <summary>Exposes the initiate or orchestrate used by the containing type.</summary>
        public const string InitiateOrOrchestrate = "ViciOne.ServiceBus.Saga.InitiateOrOrchestrate";
        /// <summary>Exposes the observe used by the containing type.</summary>
        public const string Observe = "ViciOne.ServiceBus.Saga.Observe";
        /// <summary>Exposes the raise event used by the containing type.</summary>
        public const string RaiseEvent = "ViciOne.ServiceBus.Saga.RaiseEvent";
    }


    /// <summary>Defines diagnostic operation names for routing-slip activities.</summary>
    public static class Courier
    {
        /// <summary>Exposes the execute used by the containing type.</summary>
        public const string Execute = "ViciOne.ServiceBus.Activity.Execute";
        /// <summary>Exposes the compensate used by the containing type.</summary>
        public const string Compensate = "ViciOne.ServiceBus.Activity.Compensate";
    }
}
