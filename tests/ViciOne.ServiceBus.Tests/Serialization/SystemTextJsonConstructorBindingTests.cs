using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Serialization;

public sealed class SystemTextJsonConstructorBindingTests
{
    [Fact]
    [RequirementCoverage(
        "REQ-VSB-SYSTEM-TEXT-JSON-CONSTRUCTOR-BOUND-MESSAGES",
        "request-response-immutable-values")]
    public async Task ConstructorBoundRequestAndResponse_RoundTripEveryValueAsync()
    {
        TimeSpan operationTimeout = TestConfigurationProvider.ForCurrentTestRun()
            .GetValidatedOptions()
            .OperationTimeout!.Value;
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = new InMemoryTestHarness($"constructor-binding-{NewId.NextGuid():N}")
        {
            TestTimeout = operationTimeout,
        };
        harness.BeginTestScope();
        harness.InMemoryReceiveEndpointConfiguring += configurator =>
            configurator.Handler<ConstructorBoundRequest>(context =>
                context.RespondAsync(new ConstructorBoundResponse(
                    context.Message.Message,
                    5000,
                    "I am lost!")));

        try
        {
            await harness.StartAsync(cancellationToken).WaitAsync(operationTimeout, cancellationToken);
            IRequestClient<ConstructorBoundRequest> client =
                harness.CreateRequestClient<ConstructorBoundRequest>();

            Response<ConstructorBoundResponse> response = await client.GetResponseAsync<ConstructorBoundResponse>(
                    new ConstructorBoundRequest("This is the real deal."),
                    cancellationToken)
                .WaitAsync(operationTimeout, cancellationToken);

            Assert.Equal("This is the real deal.", response.Message.Message);
            Assert.Equal(5000, response.Message.Cost);
            Assert.Equal("I am lost!", response.Message.Name);
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken).WaitAsync(operationTimeout, CancellationToken.None);
        }
    }

    public sealed class ConstructorBoundRequest
    {
        public ConstructorBoundRequest(string message)
        {
            Message = message;
        }

        public string Message { get; }
    }

    public sealed class ConstructorBoundResponse
    {
        public ConstructorBoundResponse(string message, int cost, string name)
        {
            Message = message;
            Cost = cost;
            Name = name;
        }

        public string Message { get; }

        public int Cost { get; }

        public string Name { get; }
    }
}
