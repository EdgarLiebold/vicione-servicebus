// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.EntityFrameworkIntegration
{
    using System;
    using System.Data.Entity;


    public interface ISagaClassMap
    {
        Type SagaType { get; }
        void Configure(DbModelBuilder modelBuilder);
    }
}
