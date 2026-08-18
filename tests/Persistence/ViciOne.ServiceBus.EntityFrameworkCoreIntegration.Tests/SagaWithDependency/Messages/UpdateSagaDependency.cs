namespace ViciOne.ServiceBus.EntityFrameworkCoreIntegration.Tests.SagaWithDependency.Messages
{
    using System;
    using ViciOne.ServiceBus.Tests.Saga.Messages;


    [Serializable]
    public class UpdateSagaDependency :
        SimpleSagaMessageBase
    {
        public UpdateSagaDependency()
        {
        }

        public UpdateSagaDependency(Guid correlationId, string propertyValue)
            : base(correlationId)
        {
            Name = propertyValue;
        }

        public string Name { get; set; }
    }
}
