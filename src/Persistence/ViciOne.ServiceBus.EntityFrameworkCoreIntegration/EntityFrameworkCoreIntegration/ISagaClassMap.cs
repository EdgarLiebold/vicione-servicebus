using System;
using Microsoft.EntityFrameworkCore;

namespace ViciOne.ServiceBus.EntityFrameworkCoreIntegration;

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
