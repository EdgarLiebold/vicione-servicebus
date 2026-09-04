namespace ViciOne.ServiceBus.Advanced;

/// <summary>
/// Represents the method that handles message scheduler factory.
/// </summary>
/// <param name="context">The operation context.</param>
/// <returns>The result of the operation.</returns>
public delegate IMessageScheduler MessageSchedulerFactory(ConsumeContext context);
