namespace ViciOne.ServiceBus.Configuration;

public partial class PipeConfigurator<TContext>
    where TContext : class, PipeContext
{
    /// <summary>Builds child specification pipe components.</summary>
    public class ChildSpecificationPipeBuilder :
        ISpecificationPipeBuilder<TContext>
    {
        readonly ISpecificationPipeBuilder<TContext> _builder;

        /// <summary>Initializes a new instance.</summary>
        /// <param name="builder">The builder that receives the configuration.</param>
        /// <param name="isImplemented">The is implemented.</param>
        /// <param name="isDelegated">The is delegated.</param>
        public ChildSpecificationPipeBuilder(ISpecificationPipeBuilder<TContext> builder, bool isImplemented, bool isDelegated)
        {
            _builder = builder;

            IsDelegated = isDelegated;
            IsImplemented = isImplemented;
        }

        /// <summary>Adds filter to the configuration.</summary>
        /// <param name="filter">The filter to add to the pipeline.</param>
        public void AddFilter(IFilter<TContext> filter)
        {
            _builder.AddFilter(filter);
        }

        /// <summary>Gets a value indicating whether delegated.</summary>
        public bool IsDelegated { get; }

        /// <summary>Gets a value indicating whether implemented.</summary>
        public bool IsImplemented { get; }

        /// <summary>Creates delegated builder.</summary>
        /// <returns>The created delegated builder.</returns>
        public ISpecificationPipeBuilder<TContext> CreateDelegatedBuilder()
        {
            return new ChildSpecificationPipeBuilder(this, IsImplemented, true);
        }

        /// <summary>Creates implemented builder.</summary>
        /// <returns>The created implemented builder.</returns>
        public ISpecificationPipeBuilder<TContext> CreateImplementedBuilder()
        {
            return new ChildSpecificationPipeBuilder(this, true, IsDelegated);
        }
    }
}
