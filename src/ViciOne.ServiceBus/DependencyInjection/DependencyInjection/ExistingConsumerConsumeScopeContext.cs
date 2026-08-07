// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.DependencyInjection
{
    using System;
    using System.Threading.Tasks;


    public class ExistingConsumerConsumeScopeContext<TConsumer, T> :
        IConsumerConsumeScopeContext<TConsumer, T>
        where TConsumer : class
        where T : class
    {
        readonly IDisposable _disposable;

        public ExistingConsumerConsumeScopeContext(ConsumerConsumeContext<TConsumer, T> context, IDisposable disposable)
        {
            _disposable = disposable;
            Context = context;
        }

        public ConsumerConsumeContext<TConsumer, T> Context { get; }

        public ValueTask DisposeAsync()
        {
            _disposable?.Dispose();
            return default;
        }
    }
}
