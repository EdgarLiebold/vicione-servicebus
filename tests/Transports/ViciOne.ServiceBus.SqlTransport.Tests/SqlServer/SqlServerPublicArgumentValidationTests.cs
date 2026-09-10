using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.SqlTransport.SqlServer;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.SqlTransport.Tests.SqlServer;

public sealed class SqlServerPublicArgumentValidationTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-SQLSERVER-PUBLIC-ARGUMENTS", "required-extension-arguments-fail-at-call-site")]
    public void PublicExtensions_RejectEveryNullOrEmptyRequiredArgumentAtTheCallSite()
    {
        IBusRegistrationConfigurator registration = DispatchProxy.Create<IBusRegistrationConfigurator, UnexpectedInvocationProxy>();
        ISqlBusFactoryConfigurator bus = DispatchProxy.Create<ISqlBusFactoryConfigurator, UnexpectedInvocationProxy>();

        Assert.Throws<ArgumentNullException>(() =>
            SqlServerBusFactoryConfiguratorExtensions.UsingSqlServer(null!));
        Assert.Throws<ArgumentException>(() => registration.UsingSqlServer(" \t"));

        Assert.Throws<ArgumentNullException>(() => bus.UseSqlServer((Uri)null!));
        Assert.Throws<ArgumentException>(() => bus.UseSqlServer(" \t"));
        Assert.Throws<ArgumentNullException>(() => bus.UseSqlServer((IBusRegistrationContext)null!));

        Assert.Throws<ArgumentNullException>(() =>
            SqlServerTransportConfigurationExtensions.AddSqlServerMigrationHostedService(null!, true, false));
        Assert.Throws<ArgumentNullException>(() =>
            SqlServerTransportConfigurationExtensions.AddSqlServerMigrationHostedService(
                null!,
                (Action<SqlTransportMigrationOptions>?)null));
    }

    private class UnexpectedInvocationProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            throw new InvalidOperationException($"The argument guard invoked {targetMethod?.Name ?? "an unknown member"}.");
        }
    }
}
