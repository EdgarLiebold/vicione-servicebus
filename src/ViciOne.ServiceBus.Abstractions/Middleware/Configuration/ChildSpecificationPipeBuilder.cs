namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides a pipe configurator implementation.
/// </summary>
public partial class PipeConfigurator<TContext>
    where TContext : class, PipeContext
{
    /// <summary>
    /// Provides a child specification pipe builder implementation.
    /// </summary>
    public class ChildSpecificationPipeBuilder :
        ISpecificationPipeBuilder<TContext>
    {
        readonly ISpecificationPipeBuilder<TContext> _builder;

        /// <summary>
        /// Initializes a new instance of the containing type.
        /// </summary>
        /// <param name="builder">The builder value.</param>
        /// <param name="isImplemented">The is implemented value.</param>
        /// <param name="isDelegated">The is delegated value.</param>
        public ChildSpecificationPipeBuilder(ISpecificationPipeBuilder<TContext> builder, bool isImplemented, bool isDelegated)
        {
            _builder = builder;

            IsDelegated = isDelegated;
            IsImplemented = isImplemented;
        }

        /// <summary>
        /// Adds filter to the configuration.
        /// </summary>
        /// <param name="filter">The filter value.</param>
        public void AddFilter(IFilter<TContext> filter)
        {
            _builder.AddFilter(filter);
        }

        /// <summary>
        /// Gets the is delegated value.
        /// </summary>
        public bool IsDelegated { get; }

        /// <summary>
        /// Gets the is implemented value.
        /// </summary>
        public bool IsImplemented { get; }

        /// <summary>
        /// Creates delegated builder.
        /// </summary>
        /// <returns>The result of the operation.</returns>
        public ISpecificationPipeBuilder<TContext> CreateDelegatedBuilder()
        {
            return new ChildSpecificationPipeBuilder(this, IsImplemented, true);
        }

        /// <summary>
        /// Creates implemented builder.
        /// </summary>
        /// <returns>The result of the operation.</returns>
        public ISpecificationPipeBuilder<TContext> CreateImplementedBuilder()
        {
            return new ChildSpecificationPipeBuilder(this, true, IsDelegated);
        }
    }
}
