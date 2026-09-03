using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using ViciOne.ServiceBus.SqlTransport.PostgreSql;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.SqlTransport.PostgreSql.LocalIntegration.Tests.PostgreSql;

public sealed class PostgresSendFailureClassifierTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-POSTGRES-SEND-FAILURE", "typed-chain-and-permanent-precedence")]
    public void TypedFailures_UseNpgsqlStateAndTheCompleteChainWithPermanentPrecedence()
    {
        var classifier = new PostgresSendFailureClassifier();

        AssertKind(classifier, new NpgsqlException("outer", new IOException()), TransportSendFailureKind.Transient);
        AssertKind(classifier, new NpgsqlException("temporary text"), TransportSendFailureKind.Permanent);
        AssertKind(classifier, new NpgsqlException("outer", new UnauthorizedAccessException("inner")), TransportSendFailureKind.Permanent);
        AssertKind(
            classifier,
            new NpgsqlException("outer", new IOException("middle", new UnauthorizedAccessException("inner"))),
            TransportSendFailureKind.Permanent);
        Assert.False(classifier.TryClassify(new IOException("connection words do not matter"), out TransportSendFailureKind unknown));
        Assert.Equal(TransportSendFailureKind.Unclassified, unknown);
        Assert.Throws<ArgumentNullException>(() => classifier.TryClassify(null!, out _));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-POSTGRES-SEND-FAILURE", "registration-is-singleton-across-overloads")]
    public void BusRegistration_AddsExactlyOneClassifierAcrossConfigurationOverloads()
    {
        const string connectionString = "Host=database.internal;Database=servicebus;Username=test;Password=test";
        using NpgsqlDataSource dataSource = new NpgsqlDataSourceBuilder(connectionString).Build();

        var defaultServices = new ServiceCollection();
        defaultServices.AddViciOneServiceBus(configuration => configuration.UsingPostgres());
        AssertRegistration(defaultServices);

        var connectionStringServices = new ServiceCollection();
        connectionStringServices.AddViciOneServiceBus(configuration => configuration.UsingPostgres(connectionString));
        AssertRegistration(connectionStringServices);

        var dataSourceServices = new ServiceCollection();
        dataSourceServices.AddViciOneServiceBus(configuration => configuration.UsingPostgres(dataSource));
        AssertRegistration(dataSourceServices);

        var dataSourceFactoryServices = new ServiceCollection();
        dataSourceFactoryServices.AddViciOneServiceBus(configuration => configuration.UsingPostgres(_ => dataSource));
        AssertRegistration(dataSourceFactoryServices);

        var services = new ServiceCollection();
        services
            .AddViciOneServiceBus(configuration => configuration.UsingPostgres())
            .AddViciOneServiceBus<ISecondBus>(configuration => configuration.UsingPostgres(connectionString));

        AssertRegistration(services);
    }

    static void AssertRegistration(IServiceCollection services)
    {
        ServiceDescriptor descriptor = Assert.Single(
            services,
            candidate => candidate.ServiceType == typeof(ITransportSendFailureClassifier));
        Assert.Equal(typeof(PostgresSendFailureClassifier), descriptor.ImplementationType);
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
    }

    static void AssertKind(ITransportSendFailureClassifier classifier, Exception exception, TransportSendFailureKind expected)
    {
        Assert.True(classifier.TryClassify(exception, out TransportSendFailureKind actual));
        Assert.Equal(expected, actual);
    }

    public interface ISecondBus : IBus;
}
