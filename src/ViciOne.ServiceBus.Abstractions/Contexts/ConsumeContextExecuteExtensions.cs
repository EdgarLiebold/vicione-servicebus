using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Provides extension methods for consume context execute.</summary>
public static class ConsumeContextExecuteExtensions
{
    /// <summary>Converts this value to pipe.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The converted pipe.</returns>
    public static IPipe<ConsumeContext<T>> ToPipe<T>(this Action<ConsumeContext<T>> callback)
        where T : class
    {
        return new ConsumeContextPipe<T>(callback);
    }

    /// <summary>Converts this value to pipe.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The converted pipe.</returns>
    public static IPipe<ConsumeContext<T>> ToPipe<T>(this Func<ConsumeContext<T>, Task> callback)
        where T : class
    {
        return new ConsumeContextAsyncPipe<T>(callback);
    }

    /// <summary>Converts this value to pipe.</summary>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The converted pipe.</returns>
    public static IPipe<ConsumeContext> ToPipe(this Action<ConsumeContext> callback)
    {
        return new ConsumeContextPipe(callback);
    }

    /// <summary>Converts this value to pipe.</summary>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The converted pipe.</returns>
    public static IPipe<ConsumeContext> ToPipe(this Func<ConsumeContext, Task> callback)
    {
        return new ConsumeContextAsyncPipe(callback);
    }


    class ConsumeContextPipe<T> :
        IPipe<ConsumeContext<T>>
        where T : class
    {
        readonly Action<ConsumeContext<T>> _callback;

        public ConsumeContextPipe(Action<ConsumeContext<T>> callback)
        {
            _callback = callback;
        }

        public Task SendAsync(ConsumeContext<T> context)
        {
            _callback(context);

            return Task.CompletedTask;
        }

        public void Probe(ProbeContext context)
        {
            context.CreateFilterScope("sendContextCallback");
        }
    }


    class ConsumeContextAsyncPipe<T> :
        IPipe<ConsumeContext<T>>
        where T : class
    {
        readonly Func<ConsumeContext<T>, Task> _callback;

        public ConsumeContextAsyncPipe(Func<ConsumeContext<T>, Task> callback)
        {
            _callback = callback;
        }

        public Task SendAsync(ConsumeContext<T> context)
        {
            return _callback(context);
        }

        public void Probe(ProbeContext context)
        {
            context.CreateFilterScope("sendContextCallback");
        }
    }


    class ConsumeContextPipe :
        IPipe<ConsumeContext>
    {
        readonly Action<ConsumeContext> _callback;

        public ConsumeContextPipe(Action<ConsumeContext> callback)
        {
            _callback = callback;
        }

        public Task SendAsync(ConsumeContext context)
        {
            _callback(context);

            return Task.CompletedTask;
        }

        public void Probe(ProbeContext context)
        {
            context.CreateFilterScope("sendContextCallback");
        }
    }


    class ConsumeContextAsyncPipe :
        IPipe<ConsumeContext>
    {
        readonly Func<ConsumeContext, Task> _callback;

        public ConsumeContextAsyncPipe(Func<ConsumeContext, Task> callback)
        {
            _callback = callback;
        }

        public Task SendAsync(ConsumeContext context)
        {
            return _callback(context);
        }

        public void Probe(ProbeContext context)
        {
            context.CreateFilterScope("sendContextCallback");
        }
    }
}
