using System;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>
/// Represents the method that handles rescue context factory.
/// </summary>
/// <typeparam name="TContext">The t context type.</typeparam>
/// <typeparam name="TRescueContext">The t rescue context type.</typeparam>
/// <param name="context">The operation context.</param>
/// <param name="exception">The exception associated with the operation.</param>
/// <returns>The result of the operation.</returns>
public delegate TRescueContext RescueContextFactory<in TContext, out TRescueContext>(TContext context, Exception exception)
    where TContext : class, PipeContext
    where TRescueContext : class, PipeContext;
