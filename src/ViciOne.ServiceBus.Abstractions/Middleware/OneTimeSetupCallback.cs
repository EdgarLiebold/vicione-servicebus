using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Advanced.Middleware;

/// <summary>
/// Represents the method that handles one time setup callback.
/// </summary>
/// <returns>The result of the operation.</returns>
public delegate Task OneTimeSetupCallback();
