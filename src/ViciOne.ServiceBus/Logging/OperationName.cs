// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
#nullable enable
namespace ViciOne.ServiceBus.Logging
{
    public static class OperationName
    {
        public static class Consumer
        {
            public const string Consume = "ViciOne.ServiceBus.Consumer.Consume";
            public const string Handle = "ViciOne.ServiceBus.Consumer.Handle";
        }


        public static class Saga
        {
            public const string Send = "ViciOne.ServiceBus.Saga.Send";
            public const string SendQuery = "ViciOne.ServiceBus.Saga.SendQuery";
            public const string Initiate = "ViciOne.ServiceBus.Saga.Initiate";
            public const string Orchestrate = "ViciOne.ServiceBus.Saga.Orchestrate";
            public const string InitiateOrOrchestrate = "ViciOne.ServiceBus.Saga.InitiateOrOrchestrate";
            public const string Observe = "ViciOne.ServiceBus.Saga.Observe";
            public const string RaiseEvent = "ViciOne.ServiceBus.Saga.RaiseEvent";
        }


        public static class Courier
        {
            public const string Execute = "ViciOne.ServiceBus.Activity.Execute";
            public const string Compensate = "ViciOne.ServiceBus.Activity.Compensate";
        }
    }
}
