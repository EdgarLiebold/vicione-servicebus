namespace ViciOne.ServiceBus.Advanced;

/// <summary>Creates a message scheduler for a consume context.</summary>
/// <param name="context">The consume context that owns the scheduling operation.</param>
/// <returns>The scheduler associated with <paramref name="context" />.</returns>
public delegate IMessageScheduler MessageSchedulerFactory(ConsumeContext context);
