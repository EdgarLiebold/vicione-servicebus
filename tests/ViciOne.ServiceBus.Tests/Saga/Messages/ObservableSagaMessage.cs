namespace ViciOne.ServiceBus.Tests.Saga.Messages
{
    using System;


    [Serializable]
    public class ObservableSagaMessage
    {
        public string Name { get; set; }
    }
}
