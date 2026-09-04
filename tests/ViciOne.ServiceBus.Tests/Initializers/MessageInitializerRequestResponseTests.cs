using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Initializers;

public sealed class MessageInitializerRequestResponseTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-INITIALIZER-REQUEST-RESPONSE", "merged-inputs")]
    public async Task RequestAndResponseInitializers_MergeTheirSuppliedValuesAsync()
    {
        SimpleResponse response = await GetResponseAsync<SimpleRequest, SimpleResponse>(
            configurator => configurator.Handler<SimpleRequest>(context =>
                context.Advanced().RespondAsync<SimpleResponse>(new { Value = "World" })),
            new { Name = "Hello" });

        Assert.Equal("Hello", response.Name);
        Assert.Equal("World", response.Value);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INITIALIZER-REQUEST-RESPONSE", "missing-response-property")]
    public async Task MissingResponseValue_RemainsDefaultWhileTheRequestValueCarriesForwardAsync()
    {
        SimpleResponse response = await GetResponseAsync<SimpleRequest, SimpleResponse>(
            configurator => configurator.Handler<SimpleRequest>(context =>
                context.Advanced().RespondAsync<SimpleResponse>(new { })),
            new { Name = "Hello" });

        Assert.Equal("Hello", response.Name);
        Assert.Null(response.Value);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INITIALIZER-REQUEST-RESPONSE", "nullable-numeric-boundaries")]
    public async Task NullableNumericValues_RoundTripAcrossRequestAndResponseContractsAsync()
    {
        ComplexResponse response = await GetResponseAsync<ComplexRequest, ComplexResponse>(
            configurator => configurator.Handler<ComplexRequest>(context =>
                context.Advanced().RespondAsync<ComplexResponse>(new { })),
            new
            {
                Name = "Hello",
                IntValue = 27,
                NullableIntValue = (int?)42,
            });

        Assert.Equal("Hello", response.Name);
        Assert.Equal(27, response.IntValue);
        Assert.Equal(42, response.NullableIntValue);
    }

    private static async Task<TResponse> GetResponseAsync<TRequest, TResponse>(
        Action<IInMemoryReceiveEndpointConfigurator> configure,
        object values)
        where TRequest : class
        where TResponse : class
    {
        TimeSpan operationTimeout = TestConfigurationProvider.ForCurrentTestRun()
            .GetValidatedOptions()
            .OperationTimeout!.Value;
        using var harness = new InMemoryTestHarness($"initializer-request-response-{NewId.NextGuid():N}")
        {
            TestTimeout = operationTimeout,
        };
        harness.BeginTestScope();
        harness.OnConfigureInMemoryReceiveEndpoint += configure;
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        try
        {
            await harness.StartAsync(cancellationToken);
            IRequestClient<TRequest> client = harness.CreateRequestClient<TRequest>();
            Response<TResponse> response = await client.Advanced().GetResponseAsync<TResponse>(
                values: values,
                cancellationToken: cancellationToken);

            return response.Message;
        }
        finally
        {
            await harness.StopAsync();
        }
    }

    public interface SimpleRequest
    {
        string Name { get; }
    }

    public interface SimpleResponse
    {
        string Name { get; }

        string? Value { get; }
    }

    public interface ComplexRequest
    {
        string Name { get; }

        int IntValue { get; }

        int? NullableIntValue { get; }
    }

    public interface ComplexResponse
    {
        string Name { get; }

        int? IntValue { get; }

        int NullableIntValue { get; }
    }
}
