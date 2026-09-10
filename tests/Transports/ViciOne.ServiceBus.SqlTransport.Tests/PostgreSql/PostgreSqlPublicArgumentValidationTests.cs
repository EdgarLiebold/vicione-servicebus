using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using ViciOne.ServiceBus.SqlTransport.Configuration;
using ViciOne.ServiceBus.SqlTransport.PostgreSql;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.SqlTransport.Tests.PostgreSql;

public sealed class PostgreSqlPublicArgumentValidationTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-POSTGRES-PUBLIC-ARGUMENTS", "required-extension-arguments-fail-at-call-site")]
    public void PublicExtensions_RejectEveryNullOrEmptyRequiredArgumentAtTheCallSite()
    {
        IBusRegistrationConfigurator registration = DispatchProxy.Create<IBusRegistrationConfigurator, UnexpectedInvocationProxy>();
        ISqlBusFactoryConfigurator bus = DispatchProxy.Create<ISqlBusFactoryConfigurator, UnexpectedInvocationProxy>();

        Assert.Throws<ArgumentNullException>(() =>
            PostgreSqlBusFactoryConfiguratorExtensions.UsingPostgreSql(null!));
        Assert.Throws<ArgumentException>(() => registration.UsingPostgreSql(" \t"));
        Assert.Throws<ArgumentNullException>(() => registration.UsingPostgreSql((NpgsqlDataSource)null!));
        Assert.Throws<ArgumentNullException>(() => registration.UsingPostgreSql(
            (Func<IBusRegistrationContext, NpgsqlDataSource>)null!));

        Assert.Throws<ArgumentNullException>(() => bus.UsePostgreSql((Uri)null!));
        Assert.Throws<ArgumentException>(() => bus.UsePostgreSql(" \t"));
        Assert.Throws<ArgumentNullException>(() => bus.UsePostgreSql((NpgsqlDataSource)null!));
        Assert.Throws<ArgumentNullException>(() => bus.UsePostgreSql((IBusRegistrationContext)null!));

        Assert.Throws<ArgumentNullException>(() =>
            PostgreSqlTransportConfigurationExtensions.AddPostgreSqlMigrationHostedService(null!, true, false));
        Assert.Throws<ArgumentNullException>(() =>
            PostgreSqlTransportConfigurationExtensions.AddPostgreSqlMigrationHostedService(
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
