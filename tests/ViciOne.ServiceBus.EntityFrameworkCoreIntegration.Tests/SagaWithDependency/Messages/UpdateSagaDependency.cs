// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
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
