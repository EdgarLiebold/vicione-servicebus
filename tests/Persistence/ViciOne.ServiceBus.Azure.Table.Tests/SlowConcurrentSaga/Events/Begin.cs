namespace ViciOne.ServiceBus.Azure.Table.Tests.SlowConcurrentSaga.Events
{
    using System;


    public class Begin
    {
        public Guid CorrelationId { get; set; }
    }
}
