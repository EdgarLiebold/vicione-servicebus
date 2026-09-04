using System;

namespace ViciOne.ServiceBus.Middleware;

public delegate TRescueContext RescueContextFactory<in TContext, out TRescueContext>(TContext context, Exception exception)
    where TContext : class, PipeContext
    where TRescueContext : class, PipeContext;
