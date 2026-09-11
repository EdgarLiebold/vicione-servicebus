using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Advanced.Middleware;

/// <summary>Performs one-time middleware setup asynchronously.</summary>
/// <returns>A task that completes when the one-time setup has finished.</returns>
public delegate Task OneTimeSetupCallback();
