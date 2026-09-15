namespace ViciOne.ServiceBus.Configuration;

public partial class PipeConfigurator<TContext>
    where TContext : class, PipeContext
{
    /// <summary>Forwards filters to another builder with explicit specification-application markers.</summary>
    public class ChildSpecificationPipeBuilder :
        ISpecificationPipeBuilder<TContext>
    {
        readonly ISpecificationPipeBuilder<TContext> _builder;

        /// <summary>Wraps a builder and assigns the delegated and implemented application markers.</summary>
        /// <param name="builder">The builder that receives every added filter.</param>
        /// <param name="isImplemented">Whether base message specifications are suppressed.</param>
        /// <param name="isDelegated">Whether implemented-message-type specifications are suppressed.</param>
        public ChildSpecificationPipeBuilder(ISpecificationPipeBuilder<TContext> builder, bool isImplemented, bool isDelegated)
        {
            _builder = builder;

            IsDelegated = isDelegated;
            IsImplemented = isImplemented;
        }

        /// <summary>Forwards a filter to the wrapped builder.</summary>
        /// <param name="filter">The filter appended to the wrapped builder's execution order.</param>
        public void AddFilter(IFilter<TContext> filter)
        {
            _builder.AddFilter(filter);
        }

        /// <summary>Gets whether implemented-message-type specifications are suppressed.</summary>
        public bool IsDelegated { get; }

        /// <summary>Gets whether base message specifications are suppressed.</summary>
        public bool IsImplemented { get; }

        /// <summary>Creates a delegated wrapper while preserving the implemented marker.</summary>
        /// <returns>A wrapper that forwards filters here and suppresses implemented-message-type specifications.</returns>
        public ISpecificationPipeBuilder<TContext> CreateDelegatedBuilder()
        {
            return new ChildSpecificationPipeBuilder(this, IsImplemented, true);
        }

        /// <summary>Creates an implemented wrapper while preserving the delegated marker.</summary>
        /// <returns>A wrapper that forwards filters here and suppresses base message specifications.</returns>
        public ISpecificationPipeBuilder<TContext> CreateImplementedBuilder()
        {
            return new ChildSpecificationPipeBuilder(this, true, IsDelegated);
        }
    }
}
