using System;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>Projects a failed pipeline context into the context consumed by a rescue pipe.</summary>
/// <typeparam name="TContext">The failed pipeline context type.</typeparam>
/// <typeparam name="TRescueContext">The projected rescue context type.</typeparam>
/// <param name="context">The failed pipeline context.</param>
/// <param name="exception">The selected failure.</param>
/// <returns>The context passed to the rescue pipe.</returns>
public delegate TRescueContext RescueContextFactory<in TContext, out TRescueContext>(TContext context, Exception exception)
    where TContext : class, PipeContext
    where TRescueContext : class, PipeContext;
