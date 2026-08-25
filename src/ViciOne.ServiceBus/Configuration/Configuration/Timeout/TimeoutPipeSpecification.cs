#nullable enable
namespace ViciOne.ServiceBus.Configuration
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using Middleware;


    internal abstract class TimeoutPipeSpecification<TContext, TResult> :
        IPipeSpecification<TContext>,
        ITimeoutConfigurator
        where TContext : class, ConsumeContext
        where TResult : TContext
    {
        TimeProvider? _timeProvider;

        public TimeSpan Timeout { get; set; }

        public TimeProvider TimeProvider
        {
            set => _timeProvider = value ?? throw new ArgumentNullException(nameof(value));
        }

        public void Apply(IPipeBuilder<TContext> builder)
        {
            ArgumentNullException.ThrowIfNull(builder);

            TimeSpan timeout = Timeout;
            TimeProvider? timeProvider = _timeProvider;
            TResult CreateTimeoutContext(TContext context, CancellationToken cancellationToken) =>
                CreateContext(context, cancellationToken, timeout);

            builder.AddFilter(timeProvider is null
                ? new TimeoutFilter<TContext, TResult>(CreateTimeoutContext, timeout)
                : new TimeoutFilter<TContext, TResult>(CreateTimeoutContext, timeout, timeProvider));
        }

        public IEnumerable<ValidationResult> Validate()
        {
            if (Timeout <= TimeSpan.Zero)
                yield return this.Failure(nameof(Timeout), "must be greater than zero");
        }

        protected abstract TResult CreateContext(TContext context, CancellationToken cancellationToken, TimeSpan timeout);
    }
}
