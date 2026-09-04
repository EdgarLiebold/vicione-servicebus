using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Represents the method that handles latest filter created.
/// </summary>
/// <typeparam name="T">The t type.</typeparam>
/// <param name="filter">The filter value.</param>
/// <returns>The result of the operation.</returns>
public delegate void LatestFilterCreated<T>(ILatestFilter<T> filter)
    where T : class, PipeContext;
