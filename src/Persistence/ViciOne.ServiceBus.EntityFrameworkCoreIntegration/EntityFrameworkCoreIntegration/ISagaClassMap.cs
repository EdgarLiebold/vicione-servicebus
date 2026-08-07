// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.EntityFrameworkCoreIntegration
{
    using System;
    using Microsoft.EntityFrameworkCore;


    public interface ISagaClassMap
    {
        Type SagaType { get; }
        void Configure(ModelBuilder model);
    }


    public interface ISagaClassMap<TSaga> :
        ISagaClassMap
        where TSaga : class, ISaga
    {
    }
}
