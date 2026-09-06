using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.ServiceBus.Advanced.Registration;
using ViciOne.ServiceBus.DependencyInjection.Registration;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Carries state for bus registration operations.</summary>
public class BusRegistrationContext :
    RegistrationContext,
    IBusRegistrationContext,
    IBusRegistrationIdentity
{
    IConfigureReceiveEndpoint? _configureReceiveEndpoints;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="provider">The service provider used to resolve dependencies.</param>
    /// <param name="selector">The selector.</param>
    /// <param name="setScopedConsumeContext">The set scoped consume context.</param>
    /// <param name="busType">The runtime bus type used by the operation.</param>
    public BusRegistrationContext(IServiceProvider provider, IContainerSelector selector, ISetScopedConsumeContext setScopedConsumeContext, Type busType)
        : base(provider, selector, setScopedConsumeContext)
    {
        BusKey = BusRegistrationIdentity.GetKey(busType);
    }

    string IBusRegistrationIdentity.BusKey => BusKey;

    internal string BusKey { get; }

    /// <summary>Gets the endpoint name formatter.</summary>
    public IEndpointNameFormatter EndpointNameFormatter => Selector.GetEndpointNameFormatter(this);

    /// <summary>Configures endpoints.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="endpointNameFormatter">The endpoint name formatter.</param>
    public void ConfigureEndpoints<T>(IReceiveConfigurator<T> configurator, IEndpointNameFormatter? endpointNameFormatter = null)
        where T : IReceiveEndpointConfigurator
    {
        ConfigureEndpoints(configurator, endpointNameFormatter, NoFilter);
    }

    /// <summary>Configures endpoints.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="endpointNameFormatter">The endpoint name formatter.</param>
    /// <param name="configureFilter">The configure filter.</param>
    public void ConfigureEndpoints<T>(IReceiveConfigurator<T> configurator, IEndpointNameFormatter? endpointNameFormatter,
        Action<IRegistrationFilterConfigurator>? configureFilter)
        where T : IReceiveEndpointConfigurator
    {
        endpointNameFormatter ??= EndpointNameFormatter;

        var builder = new RegistrationFilterConfigurator();
        configureFilter?.Invoke(builder);

        var registrationFilter = builder.Filter;

        var consumerKinds = ((IEnumerable<IConsumerKind>?)GetService(typeof(IEnumerable<IConsumerKind>)) ?? Array.Empty<IConsumerKind>())
            .OrderBy(x => x.IsFallback)
            .ThenBy(x => x.Order)
            .ThenBy(x => x.Name, StringComparer.Ordinal)
            .ToArray();
        var consumerKindContext = new ConsumerKindContext(this, endpointNameFormatter, registrationFilter);
        var registrations = consumerKinds
            .SelectMany(kind => kind.GetRegistrations(consumerKindContext)
                .Select(registration => new PlannedRegistration(kind, registration,
                    registration.RequiresServiceInstance || consumerKinds.Any(candidate => candidate.RequiresServiceInstance(registration.RegistrationType)))))
            .ToList();
        registrations = SelectRegistrationOwners(registrations);
        var registrationsByEndpoint = registrations
            .GroupBy(x => x.Registration.EndpointName)
            .ToDictionary(x => x.Key, x => x.OrderBy(value => value.Kind.Order).ToList());

        var endpointsWithName = Selector.GetRegistrations<IEndpointRegistration>(this)
            .Where(x => x.IncludeInConfigureEndpoints && !WasConfigured(x.Type) && registrationFilter.Matches(x))
            .Select(x => x.GetDefinition(this))
            .Select(x => new
            {
                Name = x.GetEndpointName(endpointNameFormatter),
                Definition = x
            })
            .GroupBy(x => x.Name, (name, values) => new
            {
                Name = name,
                Definition = values.Select(x => x.Definition).Combine(this, name)
            })
            .ToList();

        IEndpointDefinition? GetEndpointDefinitionByName(string name)
        {
            return endpointsWithName.SingleOrDefault(x => x.Name == name)?.Definition;
        }

        var companionEndpointNames = registrations
            .SelectMany(x => x.Registration.CompanionEndpointNames)
            .ToHashSet(StringComparer.Ordinal);

        IEnumerable<string> endpointNames = registrationsByEndpoint.Keys
            .Union(endpointsWithName.Select(x => x.Name))
            .Where(name => !companionEndpointNames.Contains(name))
            .OrderBy(name => registrationsByEndpoint.TryGetValue(name, out List<PlannedRegistration>? endpointRegistrations)
                ? endpointRegistrations.Min(x => x.Kind.Order)
                : int.MaxValue)
            .ThenBy(name => name, StringComparer.Ordinal);

        var endpoints = endpointNames
            .Select(name =>
            {
                registrationsByEndpoint.TryGetValue(name, out var endpointRegistrations);
                endpointRegistrations ??= new List<PlannedRegistration>();

                var definition = GetEndpointDefinitionByName(name) ?? CreateConsumerKindEndpointDefinition(name, endpointRegistrations);
                return new Endpoint(definition, endpointRegistrations);
            })
            .ToList();

        var needsServiceInstance = !(configurator is IServiceInstanceConfigurator<T>) && endpoints.Any(endpoint => endpoint.RequiresServiceInstance);
        if (needsServiceInstance)
        {
            var hosts = Selector.GetRegistrations<IConsumerKindHost>(this).ToArray();
            if (hosts.Length != 1)
            {
                throw new ConfigurationException(
                    Providers.Configuration.ConfigurationMessages.Create("Consumer kind host", BusKey,
                        "Exactly one service-instance host is required by the registered consumer kinds",
                        "Register the capability package that owns the service-instance consumer kind"));
            }

            var host = hosts[0];

            var endpointDefinition = new RegistrationContextEndpointDefinition(host.EndpointDefinition, this);

            configurator.ReceiveEndpoint(endpointDefinition, endpointNameFormatter, endpointConfigurator =>
            {
                var options = new ServiceInstanceOptions().SetEndpointNameFormatter(endpointNameFormatter);

                var instanceConfigurator = new ServiceInstanceConfigurator<T>(configurator, options, endpointConfigurator);

                host.Configure(instanceConfigurator, this);

                ConfigureTheEndpoints(endpoints, endpointNameFormatter, GetEndpointDefinitionByName, configurator, instanceConfigurator);
            });
        }
        else
            ConfigureTheEndpoints(endpoints, endpointNameFormatter, GetEndpointDefinitionByName, configurator);
    }

    /// <summary>Gets configure receive endpoints.</summary>
    /// <returns>The configure receive endpoints.</returns>
    public IConfigureReceiveEndpoint GetConfigureReceiveEndpoints()
    {
        if (_configureReceiveEndpoints != null)
            return _configureReceiveEndpoints;

        _configureReceiveEndpoints = Selector.GetConfigureReceiveEndpoints(this);

        return _configureReceiveEndpoints;
    }

    IEndpointDefinition CreateConsumerKindEndpointDefinition(string endpointName, IReadOnlyCollection<PlannedRegistration> registrations)
    {
        if (registrations.Count == 0)
            return new NamedEndpointDefinition(endpointName);

        var firstKind = registrations
            .GroupBy(x => x.Kind.Order)
            .OrderBy(x => x.Key)
            .First();

        return firstKind
            .Select(x => (IEndpointDefinition)new DelegateEndpointDefinition(endpointName, x.Registration.Definition,
                x.Registration.EndpointDefinition))
            .Combine(this, endpointName) ?? new NamedEndpointDefinition(endpointName);
    }

    void ConfigureTheEndpoints<T>(IEnumerable<Endpoint> endpoints, IEndpointNameFormatter endpointNameFormatter,
        Func<string, IEndpointDefinition?> getEndpointDefinitionByName,
        IReceiveConfigurator<T> configurator, IReceiveConfigurator<T>? instanceConfigurator = null)
        where T : IReceiveEndpointConfigurator
    {
        var configureReceiveEndpoint = GetConfigureReceiveEndpoints();

        foreach (var endpoint in endpoints)
        {
            IReceiveConfigurator<T> useConfigurator = instanceConfigurator != null && endpoint.RequiresServiceInstance
                ? instanceConfigurator
                : configurator;

            var endpointDefinition = new RegistrationContextEndpointDefinition(endpoint.Definition, this);

            useConfigurator.ReceiveEndpoint(endpointDefinition, endpointNameFormatter, cfg =>
            {
                configureReceiveEndpoint.Configure(endpointDefinition.GetEndpointName(endpointNameFormatter), cfg);

                var context = new ConsumerKindEndpointContext<T>(this, cfg, configurator, endpointNameFormatter,
                    getEndpointDefinitionByName, configureReceiveEndpoint);
                foreach (var registration in endpoint.Registrations)
                {
                    registration.Registration.Configure(context);
                    MarkConfigured(registration.Registration.RegistrationType);
                }
            });
        }
    }

    static void NoFilter(IRegistrationFilterConfigurator configurator)
    {
    }

    List<PlannedRegistration> SelectRegistrationOwners(IEnumerable<PlannedRegistration> registrations)
    {
        return registrations
            .GroupBy(registration => registration.Registration.RegistrationType)
            .Select(group =>
            {
                PlannedRegistration[] explicitOwners = group.Where(registration => !registration.Kind.IsFallback).ToArray();
                if (explicitOwners.Length == 1)
                    return explicitOwners[0];

                if (explicitOwners.Length > 1)
                {
                    string owners = string.Join(", ", explicitOwners.Select(registration => registration.Kind.Name)
                        .OrderBy(name => name, StringComparer.Ordinal));
                    throw new ConfigurationException(
                        Providers.Configuration.ConfigurationMessages.Create("Consumer kind", BusKey,
                            $"Handler '{TypeCache.GetShortName(group.Key)}' is claimed by multiple capability categories: {owners}",
                            "Register exactly one capability package that owns the handler type"));
                }

                PlannedRegistration[] fallbackOwners = group.ToArray();
                if (fallbackOwners.Length == 1)
                    return fallbackOwners[0];

                throw new ConfigurationException(
                    Providers.Configuration.ConfigurationMessages.Create("Consumer kind", BusKey,
                        $"Handler '{TypeCache.GetShortName(group.Key)}' is claimed by multiple fallback categories",
                        "Register exactly one fallback consumer category"));
            })
            .ToList();
    }


    sealed class ConsumerKindContext :
        IConsumerKindContext
    {
        readonly BusRegistrationContext _context;
        readonly IRegistrationFilter _filter;

        public ConsumerKindContext(BusRegistrationContext context, IEndpointNameFormatter endpointNameFormatter,
            IRegistrationFilter filter)
        {
            _context = context;
            EndpointNameFormatter = endpointNameFormatter;
            _filter = filter;
        }

        public IRegistrationContext RegistrationContext => _context;
        public IEndpointNameFormatter EndpointNameFormatter { get; }

        public IEnumerable<TRegistration> GetRegistrations<TRegistration>()
            where TRegistration : class, IRegistration
        {
            return _context.Selector.GetRegistrations<TRegistration>(_context)
                .Where(x => x.IncludeInConfigureEndpoints && !_context.WasConfigured(x.Type) && _filter.Matches(x));
        }
    }


    sealed class ConsumerKindEndpointContext<T> :
        IConsumerKindEndpointContext
        where T : IReceiveEndpointConfigurator
    {
        readonly IConfigureReceiveEndpoint _configureReceiveEndpoint;
        readonly IReceiveConfigurator<T> _configurator;
        readonly IEndpointNameFormatter _endpointNameFormatter;
        readonly Func<string, IEndpointDefinition?> _getEndpointDefinitionByName;

        public ConsumerKindEndpointContext(BusRegistrationContext registrationContext,
            IReceiveEndpointConfigurator endpointConfigurator, IReceiveConfigurator<T> configurator,
            IEndpointNameFormatter endpointNameFormatter, Func<string, IEndpointDefinition?> getEndpointDefinitionByName,
            IConfigureReceiveEndpoint configureReceiveEndpoint)
        {
            RegistrationContext = registrationContext;
            EndpointConfigurator = endpointConfigurator;
            _configurator = configurator;
            _endpointNameFormatter = endpointNameFormatter;
            _getEndpointDefinitionByName = getEndpointDefinitionByName;
            _configureReceiveEndpoint = configureReceiveEndpoint;
        }

        public IRegistrationContext RegistrationContext { get; }
        public IReceiveEndpointConfigurator EndpointConfigurator { get; }

        public void ConfigureCompanionEndpoint(string endpointName, IEndpointDefinition? endpointDefinition,
            Action<IReceiveEndpointConfigurator> configure)
        {
            endpointDefinition ??= _getEndpointDefinitionByName(endpointName);
            if (endpointDefinition != null)
            {
                var registrationDefinition = new RegistrationContextEndpointDefinition(endpointDefinition,
                    (IBusRegistrationContext)RegistrationContext);
                _configurator.ReceiveEndpoint(registrationDefinition, _endpointNameFormatter, companionConfigurator =>
                {
                    _configureReceiveEndpoint.Configure(registrationDefinition.GetEndpointName(_endpointNameFormatter), companionConfigurator);
                    configure(companionConfigurator);
                });
                return;
            }

            _configurator.ReceiveEndpoint(endpointName, companionConfigurator =>
            {
                _configureReceiveEndpoint.Configure(endpointName, companionConfigurator);
                configure(companionConfigurator);
            });
        }
    }


    sealed record PlannedRegistration(IConsumerKind Kind, IConsumerKindRegistration Registration, bool RequiresServiceInstance);


    sealed class Endpoint
    {
        public Endpoint(IEndpointDefinition definition, IReadOnlyList<PlannedRegistration> registrations)
        {
            Definition = definition;
            Registrations = registrations;
        }

        public IEndpointDefinition Definition { get; }
        public IReadOnlyList<PlannedRegistration> Registrations { get; }
        public bool RequiresServiceInstance => Registrations.Any(x => x.RequiresServiceInstance);
    }
}
