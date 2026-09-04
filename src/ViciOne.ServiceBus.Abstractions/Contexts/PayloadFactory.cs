namespace ViciOne.ServiceBus;

public delegate TPayload PayloadFactory<out TPayload>()
    where TPayload : class;
