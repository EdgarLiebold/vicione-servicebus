// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Saga
{
    using System;


    public delegate TSaga SagaInstanceFactoryMethod<out TSaga>(Guid correlationId)
        where TSaga : class, ISaga;
}
