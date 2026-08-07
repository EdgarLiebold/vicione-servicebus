// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.HangfireIntegration
{
    using System;
    using System.Collections.Concurrent;
    using System.Linq.Expressions;
    using System.Reflection;
    using Hangfire;
    using Internals;


    public class ViciOneServiceBusJobActivator :
        JobActivator
    {
        readonly IBus _bus;
        readonly ConcurrentDictionary<Type, IViciOneServiceBusJobActivatorFactory> _typeFactories;

        public ViciOneServiceBusJobActivator(IBus bus)
        {
            _bus = bus;
            _typeFactories = new ConcurrentDictionary<Type, IViciOneServiceBusJobActivatorFactory>();
        }

        public override object ActivateJob(Type jobType)
        {
            return _typeFactories.GetOrAdd(jobType, CreateJobFactory)
                .ActivateJob();
        }

        IViciOneServiceBusJobActivatorFactory CreateJobFactory(Type type)
        {
            var genericType = typeof(ViciOneServiceBusJobActivatorFactory<>).MakeGenericType(type);

            return (IViciOneServiceBusJobActivatorFactory)Activator.CreateInstance(genericType, _bus)!;
        }


        interface IViciOneServiceBusJobActivatorFactory
        {
            object ActivateJob();
        }


        class ViciOneServiceBusJobActivatorFactory<T> :
            IViciOneServiceBusJobActivatorFactory
        {
            readonly IBus _bus;
            readonly Func<IBus, T> _factory;

            public ViciOneServiceBusJobActivatorFactory(IBus bus)
            {
                _bus = bus;
                _factory = CreateConstructor();
            }

            public object ActivateJob()
            {
                return NewJob()!;
            }

            T NewJob()
            {
                try
                {
                    return _factory(_bus);
                }
                catch (Exception ex)
                {
                    throw new Exception($"Problem instantiating class '{TypeCache<T>.ShortName}'", ex);
                }
            }

            static Func<IBus, T> CreateConstructor()
            {
                var ctor = typeof(T).GetConstructor(new[] { typeof(IBus) });
                if (ctor != null)
                    return CreateServiceBusConstructor(ctor);

                ctor = typeof(T).GetConstructor(Type.EmptyTypes);
                if (ctor != null)
                    return CreateDefaultConstructor(ctor);

                throw new Exception($"The job class does not have a supported constructor: {TypeCache<T>.ShortName}");
            }

            static Func<IBus, T> CreateDefaultConstructor(ConstructorInfo constructorInfo)
            {
                var bus = Expression.Parameter(typeof(IBus), "bus");
                var @new = Expression.New(constructorInfo);

                return Expression.Lambda<Func<IBus, T>>(@new, bus).CompileFast();
            }

            static Func<IBus, T> CreateServiceBusConstructor(ConstructorInfo constructorInfo)
            {
                var bus = Expression.Parameter(typeof(IBus), "bus");
                var @new = Expression.New(constructorInfo, bus);

                return Expression.Lambda<Func<IBus, T>>(@new, bus).CompileFast();
            }
        }
    }
}
