using System.Reflection;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.SqlTransport.SqlServer;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.SqlTransport.SqlServer.LocalIntegration.Tests.SqlServer;

public sealed class SqlServerSendFailureClassifierTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-SQLSERVER-SEND-FAILURE", "numeric-transient-and-permanent-taxonomy")]
    public void NumericTaxonomy_ClassifiesEveryOwnedErrorNumberWithoutUsingMessageText()
    {
        int[] transient = [-2, 20, 64, 233, 1205, 10053, 10054, 10060, 10928, 10929, 40143, 40197, 40501, 40613];

        Assert.All(transient, number =>
            Assert.Equal(TransportSendFailureKind.Transient, SqlServerSendFailureClassifier.ClassifyErrorNumber(number)));
        Assert.Equal(TransportSendFailureKind.Permanent, SqlServerSendFailureClassifier.ClassifyErrorNumber(50000));

        var classifier = new SqlServerSendFailureClassifier();
        AssertKind(classifier, CreateSqlException(1205), TransportSendFailureKind.Transient);
        AssertKind(classifier, CreateSqlException(50000), TransportSendFailureKind.Permanent);
        AssertKind(
            classifier,
            CreateSqlException(1205, CreateSqlException(50000)),
            TransportSendFailureKind.Permanent);
        Assert.False(classifier.TryClassify(new IOException("1205 words do not matter"), out TransportSendFailureKind unknown));
        Assert.Equal(TransportSendFailureKind.Unclassified, unknown);
        Assert.Throws<ArgumentNullException>(() => classifier.TryClassify(null!, out _));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SQLSERVER-SEND-FAILURE", "registration-is-singleton-across-overloads")]
    public void BusRegistration_AddsExactlyOneClassifierAcrossConfigurationOverloads()
    {
        const string connectionString =
            "Server=database.internal;Database=servicebus;Integrated Security=true;TrustServerCertificate=true";

        var defaultServices = new ServiceCollection();
        defaultServices.AddViciOneServiceBus(configuration => configuration.UsingSqlServer());
        AssertRegistration(defaultServices);

        var connectionStringServices = new ServiceCollection();
        connectionStringServices.AddViciOneServiceBus(configuration => configuration.UsingSqlServer(connectionString));
        AssertRegistration(connectionStringServices);

        var services = new ServiceCollection();
        services
            .AddViciOneServiceBus(configuration => configuration.UsingSqlServer())
            .AddViciOneServiceBus<ISecondBus>(configuration => configuration.UsingSqlServer(connectionString));

        AssertRegistration(services);
    }

    static void AssertRegistration(IServiceCollection services)
    {
        ServiceDescriptor descriptor = Assert.Single(
            services,
            candidate => candidate.ServiceType == typeof(ITransportSendFailureClassifier));
        Assert.Equal(typeof(SqlServerSendFailureClassifier), descriptor.ImplementationType);
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
    }

    public interface ISecondBus : IBus;

    private static SqlException CreateSqlException(int number, Exception? innerException = null)
    {
        var errors = (SqlErrorCollection)Activator.CreateInstance(typeof(SqlErrorCollection), nonPublic: true)!;
        ConstructorInfo constructor = Assert.Single(
            typeof(SqlError).GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic),
            candidate => candidate.GetParameters().Length == 8);
        object?[] constructorArguments = constructor.GetParameters()
            .Select(parameter => CreateArgument(parameter, number, innerException))
            .ToArray();
        var error = (SqlError)constructor.Invoke(constructorArguments);

        MethodInfo add = Assert.Single(typeof(SqlErrorCollection).GetMethods(BindingFlags.Instance | BindingFlags.NonPublic),
            method => method.Name == "Add" && method.GetParameters() is [{ ParameterType: var parameterType }]
                && parameterType == typeof(SqlError));
        add.Invoke(errors, [error]);

        MethodInfo createException = Assert.Single(
            typeof(SqlException).GetMethods(BindingFlags.Static | BindingFlags.NonPublic),
            method => method.Name == "CreateException"
                && method.GetParameters() is
                [
                { ParameterType: var errorsType },
                { ParameterType: var versionType },
                    _,
                { ParameterType: var exceptionType },
                ]
                && errorsType == typeof(SqlErrorCollection)
                && versionType == typeof(string)
                && method.GetParameters()[2].ParameterType.FullName ==
                    "Microsoft.Data.SqlClient.Connection.SqlConnectionInternal"
                && exceptionType == typeof(Exception));
        object?[] factoryArguments = createException.GetParameters()
            .Select(parameter => parameter.ParameterType == typeof(SqlErrorCollection)
                ? errors
                : CreateArgument(parameter, number, innerException))
            .ToArray();

        var exception = Assert.IsType<SqlException>(createException.Invoke(null, factoryArguments));
        Assert.Equal(number, exception.Number);
        Assert.Same(innerException, exception.InnerException);
        return exception;
    }

    private static object? CreateArgument(ParameterInfo parameter, int number, Exception? innerException)
    {
        if (parameter.ParameterType == typeof(int) && parameter.Name is "infoNumber" or "number")
            return number;
        if (parameter.ParameterType == typeof(string))
            return parameter.Name == "errorMessage" ? "message text deliberately carries no policy" : string.Empty;
        if (parameter.ParameterType == typeof(Exception))
            return innerException;
        if (parameter.ParameterType == typeof(Guid))
            return Guid.Empty;
        if (parameter.HasDefaultValue)
            return parameter.DefaultValue;
        if (parameter.ParameterType.IsValueType)
            return Activator.CreateInstance(parameter.ParameterType);

        return null;
    }

    private static void AssertKind(ITransportSendFailureClassifier classifier, Exception exception, TransportSendFailureKind expected)
    {
        Assert.True(classifier.TryClassify(exception, out TransportSendFailureKind actual));
        Assert.Equal(expected, actual);
    }
}
