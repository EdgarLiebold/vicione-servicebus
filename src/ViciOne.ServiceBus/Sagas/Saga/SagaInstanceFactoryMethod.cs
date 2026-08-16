namespace ViciOne.ServiceBus.Saga
{
    using System;


    public delegate TSaga SagaInstanceFactoryMethod<out TSaga>(Guid correlationId)
        where TSaga : class, ISaga;
}
