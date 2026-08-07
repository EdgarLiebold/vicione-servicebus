// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus
{
    using System;
    using System.Linq.Expressions;


    public interface Observes<TMessage, TSaga> :
        IConsumer<TMessage>
        where TMessage : class
    {
        Expression<Func<TSaga, TMessage, bool>> CorrelationExpression { get; }
    }
}
