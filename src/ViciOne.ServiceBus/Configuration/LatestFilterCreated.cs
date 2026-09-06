using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Represents the method that handles latest filter created.</summary>
/// <typeparam name="T">The value type.</typeparam>
/// <param name="filter">The filter to add to the pipeline.</param>
public delegate void LatestFilterCreated<T>(ILatestFilter<T> filter)
    where T : class, PipeContext;
