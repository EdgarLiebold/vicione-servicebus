namespace ViciOne.ServiceBus.Advanced;

/// <summary>Represents the method that handles message scheduler factory.</summary>
/// <param name="context">The context associated with the operation.</param>
/// <returns>The value produced by the operation.</returns>
public delegate IMessageScheduler MessageSchedulerFactory(ConsumeContext context);
