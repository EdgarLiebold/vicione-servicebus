using System;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.DependencyInjection.Registration;

/// <summary>Caches request client registration data.</summary>
public static class RequestClientRegistrationCache
{
    /// <summary>Registers the supplied component.</summary>
    /// <param name="requestType">The runtime request type used by the operation.</param>
    /// <param name="timeout">The maximum duration allowed for the operation.</param>
    /// <param name="registrar">The registrar.</param>
    public static void Register(Type requestType, RequestTimeout timeout, IContainerRegistrar registrar)
    {
        Cached.Instance.GetOrAdd(requestType).Register(timeout, registrar);
    }

    /// <summary>Registers the supplied component.</summary>
    /// <param name="requestType">The runtime request type used by the operation.</param>
    /// <param name="destinationAddress">The destination address.</param>
    /// <param name="timeout">The maximum duration allowed for the operation.</param>
    /// <param name="registrar">The registrar.</param>
    public static void Register(Type requestType, Uri destinationAddress, RequestTimeout timeout, IContainerRegistrar registrar)
    {
        Cached.Instance.GetOrAdd(requestType).Register(destinationAddress, timeout, registrar);
    }

    static CachedRegistration Factory(Type type)
    {
        return (CachedRegistration)(Activator.CreateInstance(typeof(CachedRegistration<>).MakeGenericType(type)) ?? throw new System.InvalidOperationException("The requested runtime type could not be activated."));
    }


    static class Cached
    {
        internal static readonly RegistrationCache<CachedRegistration> Instance = new RegistrationCache<CachedRegistration>(Factory);
    }


    interface CachedRegistration
    {
        void Register(RequestTimeout timeout, IContainerRegistrar registrar);
        void Register(Uri destinationAddress, RequestTimeout timeout, IContainerRegistrar registrar);
    }


    class CachedRegistration<T> :
        CachedRegistration
        where T : class
    {
        public void Register(RequestTimeout timeout, IContainerRegistrar registrar)
        {
            registrar.RegisterRequestClient<T>(timeout);
        }

        public void Register(Uri destinationAddress, RequestTimeout timeout, IContainerRegistrar registrar)
        {
            registrar.RegisterRequestClient<T>(destinationAddress, timeout);
        }
    }
}
