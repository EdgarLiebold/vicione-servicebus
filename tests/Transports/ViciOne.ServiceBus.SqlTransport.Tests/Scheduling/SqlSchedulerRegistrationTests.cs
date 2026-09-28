using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using ViciOne.ServiceBus.Advanced.Registration;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Providers.Transports;
using ViciOne.ServiceBus.Scheduling;
using ViciOne.ServiceBus.SqlTransport.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports;
using Xunit;

namespace ViciOne.ServiceBus.SqlTransport.Tests.Scheduling;

public sealed class SqlSchedulerRegistrationTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-SQL-SCHEDULER-REGISTRATION", "null-owner-rejected")]
    public void Registration_RejectsMissingOwnerBeforeAccessingServices()
    {
        Assert.Equal("configurator", Assert.Throws<ArgumentNullException>(() =>
            SqlScheduleMessageExtensions.AddSqlMessageScheduler(null!)).ParamName);
        Assert.Equal("configurator", Assert.Throws<ArgumentNullException>(() =>
            SqlScheduleMessageExtensions.AddSqlMessageScheduler<IProbeBus>(null!)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SQL-SCHEDULER-REGISTRATION", "default-preserves-existing-and-rejects-wrong-transport")]
    public void DefaultRegistration_PreservesExistingSchedulerAndRejectsWrongTransport()
    {
        var services = new ServiceCollection();
        IBusRegistrationConfigurator registration = CreateRegistration<IBusRegistrationConfigurator>(services);
        IMessageScheduler existing = DispatchProxy.Create<IMessageScheduler, UnsupportedProxy>();
        services.AddScoped(_ => existing);
        registration.AddSqlMessageScheduler();
        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();
        Assert.Same(existing, scope.ServiceProvider.GetRequiredService<IMessageScheduler>());

        var wrongTransportServices = new ServiceCollection();
        CreateRegistration<IBusRegistrationConfigurator>(wrongTransportServices).AddSqlMessageScheduler();
        wrongTransportServices.AddSingleton(Bind<IBus>.Create<IBusInstance>(CreateInstance<IBusInstance>(null)));
        wrongTransportServices.AddSingleton(DispatchProxy.Create<ISendEndpointProvider, UnsupportedProxy>());
        using ServiceProvider wrongTransportProvider = wrongTransportServices.BuildServiceProvider();
        using IServiceScope wrongTransportScope = wrongTransportProvider.CreateScope();
        ArgumentException error = Assert.Throws<ArgumentException>(() =>
            wrongTransportScope.ServiceProvider.GetRequiredService<IMessageScheduler>());
        Assert.Contains("SQL transport configuration", error.Message);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SQL-SCHEDULER-REGISTRATION", "default-scoped-clock")]
    public void DefaultRegistration_UsesScopedInstancesAndConfiguredClock()
    {
        var services = new ServiceCollection();
        var clock = new FakeTimeProvider(new DateTimeOffset(2039, 1, 2, 3, 4, 5, TimeSpan.Zero));
        services.AddSingleton<TimeProvider>(clock);
        services.AddSingleton(Bind<IBus>.Create<IBusInstance>(CreateInstance<IBusInstance>(
            DispatchProxy.Create<ISqlHostConfiguration, UnsupportedProxy>())));
        services.AddSingleton(DispatchProxy.Create<ISendEndpointProvider, UnsupportedProxy>());
        CreateRegistration<IBusRegistrationConfigurator>(services).AddSqlMessageScheduler();
        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope first = provider.CreateScope();
        using IServiceScope second = provider.CreateScope();

        IMessageScheduler firstScheduler = first.ServiceProvider.GetRequiredService<IMessageScheduler>();
        Assert.Same(firstScheduler, first.ServiceProvider.GetRequiredService<IMessageScheduler>());
        Assert.NotSame(firstScheduler, second.ServiceProvider.GetRequiredService<IMessageScheduler>());
        Assert.Same(clock, firstScheduler.TimeProvider);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SQL-SCHEDULER-REGISTRATION", "typed-owner-scope-and-transport")]
    public void TypedRegistration_PreservesOwnerAndScopeAndRejectsWrongTransport()
    {
        var services = new ServiceCollection();
        var clock = new FakeTimeProvider();
        services.AddSingleton<TimeProvider>(clock);
        services.AddSingleton<IBusInstance<IProbeBus>>(CreateInstance<IBusInstance<IProbeBus>>(
            DispatchProxy.Create<ISqlHostConfiguration, UnsupportedProxy>()));
        services.AddSingleton(Bind<IProbeBus>.Create<ISendEndpointProvider>(
            DispatchProxy.Create<ISendEndpointProvider, UnsupportedProxy>()));
        CreateRegistration<IBusRegistrationConfigurator<IProbeBus>>(services).AddSqlMessageScheduler<IProbeBus>();
        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();
        using IServiceScope secondScope = provider.CreateScope();

        IMessageScheduler owned = scope.ServiceProvider.GetRequiredService<Bind<IProbeBus, IMessageScheduler>>().Value;
        Assert.Same(owned, scope.ServiceProvider.GetRequiredService<Bind<IProbeBus, IMessageScheduler>>().Value);
        Assert.NotSame(owned, secondScope.ServiceProvider.GetRequiredService<Bind<IProbeBus, IMessageScheduler>>().Value);
        Assert.Same(clock, owned.TimeProvider);
        Assert.Null(scope.ServiceProvider.GetService<IMessageScheduler>());

        var existingServices = new ServiceCollection();
        IMessageScheduler existing = DispatchProxy.Create<IMessageScheduler, UnsupportedProxy>();
        existingServices.AddScoped(_ => Bind<IProbeBus>.Create(existing));
        CreateRegistration<IBusRegistrationConfigurator<IProbeBus>>(existingServices).AddSqlMessageScheduler<IProbeBus>();
        using ServiceProvider existingProvider = existingServices.BuildServiceProvider();
        using IServiceScope existingScope = existingProvider.CreateScope();
        Assert.Same(existing, existingScope.ServiceProvider.GetRequiredService<Bind<IProbeBus, IMessageScheduler>>().Value);

        var wrongTransportServices = new ServiceCollection();
        wrongTransportServices.AddSingleton<IBusInstance<IProbeBus>>(CreateInstance<IBusInstance<IProbeBus>>(null));
        wrongTransportServices.AddSingleton(Bind<IProbeBus>.Create<ISendEndpointProvider>(
            DispatchProxy.Create<ISendEndpointProvider, UnsupportedProxy>()));
        CreateRegistration<IBusRegistrationConfigurator<IProbeBus>>(wrongTransportServices).AddSqlMessageScheduler<IProbeBus>();
        using ServiceProvider wrongTransportProvider = wrongTransportServices.BuildServiceProvider();
        using IServiceScope wrongTransportScope = wrongTransportProvider.CreateScope();
        ArgumentException error = Assert.Throws<ArgumentException>(() =>
            wrongTransportScope.ServiceProvider.GetRequiredService<Bind<IProbeBus, IMessageScheduler>>());
        Assert.Contains("SQL transport configuration", error.Message);
    }

    private static TRegistration CreateRegistration<TRegistration>(IServiceCollection services)
        where TRegistration : class
    {
        TRegistration registration = DispatchProxy.Create<TRegistration, RegistrationProxy>();
        ((RegistrationProxy)(object)registration).Services = services;
        return registration;
    }

    private static TInstance CreateInstance<TInstance>(IHostConfiguration? hostConfiguration)
        where TInstance : class
    {
        TInstance instance = DispatchProxy.Create<TInstance, InstanceProxy>();
        ((InstanceProxy)(object)instance).HostConfiguration = hostConfiguration;
        return instance;
    }

    public interface IProbeBus : IBus;

    private class RegistrationProxy : DispatchProxy
    {
        public IServiceCollection Services { get; set; } = null!;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            targetMethod?.Name == "get_Services" ? Services : throw new NotSupportedException(targetMethod?.Name);
    }

    private class InstanceProxy : DispatchProxy
    {
        public IHostConfiguration? HostConfiguration { get; set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod?.Name == "get_HostConfiguration")
                return HostConfiguration;
            if (targetMethod?.Name == "get_Bus")
                return DispatchProxy.Create(targetMethod.ReturnType, typeof(BusProxy));
            throw new NotSupportedException(targetMethod?.Name);
        }
    }

    private class BusProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            targetMethod?.Name == "get_Topology"
                ? DispatchProxy.Create<IBusTopology, UnsupportedProxy>()
                : throw new NotSupportedException(targetMethod?.Name);
    }

    private class UnsupportedProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new NotSupportedException(targetMethod?.Name);
    }
}
