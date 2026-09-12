namespace ViciOne.ServiceBus.Initializers.Conventions;

/// <summary>Creates a convention adapter for a closed contract type.</summary>
internal interface IConventionTypeCacheFactory
{
    /// <summary>Creates the adapter that handles <typeparamref name="T" />.</summary>
    /// <typeparam name="T">The closed message or input contract type.</typeparam>
    /// <param name="convention">The owning convention used for the next dispatch level.</param>
    /// <returns>The convention adapter.</returns>
    object Create<T>(IInitializerConvention convention)
        where T : class;
}
