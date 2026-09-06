using System;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>Represents the method that handles rescue context factory.</summary>
/// <typeparam name="TContext">The pipeline context carried by the member.</typeparam>
/// <typeparam name="TRescueContext">The rescue context type.</typeparam>
/// <param name="context">The context associated with the operation.</param>
/// <param name="exception">The exception associated with the operation.</param>
/// <returns>The value produced by the operation.</returns>
public delegate TRescueContext RescueContextFactory<in TContext, out TRescueContext>(TContext context, Exception exception)
    where TContext : class, PipeContext
    where TRescueContext : class, PipeContext;
