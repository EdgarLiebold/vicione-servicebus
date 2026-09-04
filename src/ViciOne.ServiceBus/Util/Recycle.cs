using System;
using System.Threading;

namespace ViciOne.ServiceBus.Util;

/// <summary>
/// Recycles a supervisor once it is stopped, replacing it with a new one
/// </summary>
/// <typeparam name="T"></typeparam>
public class Recycle<T>
    where T : class, IAgent
{
    Lazy<T> _supervisor = null!;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="supervisorFactory">The supervisor factory value.</param>
    public Recycle(Func<T> supervisorFactory)
    {
        CancellationTokenRegistration registration = default;

        void RecycleSupervisor()
        {
            registration.Dispose();

            Volatile.Write(ref _supervisor, new Lazy<T>(() =>
            {
                var supervisor = supervisorFactory();

                registration = supervisor.Stopping.Register(() => RecycleSupervisor());

                return supervisor;
            }));
        }

        RecycleSupervisor();
    }

    /// <summary>
    /// Gets the supervisor value.
    /// </summary>
    public T Supervisor => Volatile.Read(ref _supervisor).Value;
}
