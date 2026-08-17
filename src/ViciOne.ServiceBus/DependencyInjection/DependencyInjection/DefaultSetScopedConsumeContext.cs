namespace ViciOne.ServiceBus.DependencyInjection
{
    using System;
    using Microsoft.Extensions.DependencyInjection;


    /// <summary>
    /// Pushes the consume context into whichever provider the scope resolves.
    /// <para>
    /// This is the behaviour a bus configured without a container has to use: there is no registration
    /// context, so there is no bus-specific setter to ask. It used to be public and called Legacy, which
    /// suggested it was compatibility scaffolding; it is not, and the one retained caller is the
    /// context-less job service configuration. The public overloads that reached it are gone, so it is
    /// internal now.
    /// </para>
    /// </summary>
    class DefaultSetScopedConsumeContext :
        ISetScopedConsumeContext
    {
        public static readonly ISetScopedConsumeContext Instance = new DefaultSetScopedConsumeContext();

        DefaultSetScopedConsumeContext()
        {
        }

        public IDisposable PushContext(IServiceScope scope, ConsumeContext context)
        {
            return scope.ServiceProvider.GetRequiredService<IScopedConsumeContextProvider>().PushContext(context);
        }
    }
}
