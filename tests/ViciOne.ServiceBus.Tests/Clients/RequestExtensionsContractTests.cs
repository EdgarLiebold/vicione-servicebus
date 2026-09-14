using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Clients;

public sealed class RequestExtensionsContractTests
{
    private const string VariantHeader = "ViciOne-Request-Variant";

    [Fact]
    [RequirementCoverage("REQ-VSB-REQUEST-CONVENIENCE", "every-bus-and-consume-scope-overload-preserves-message-callback-and-response")]
    public async Task EveryBusAndConsumeScopeOverload_PreservesItsExactMessageCallbackAndResponseAsync()
    {
        TimeSpan timeout = TestConfigurationProvider.ForCurrentTestRun().GetValidatedOptions().OperationTimeout!.Value;
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var requestTimeout = new RequestTimeout(timeout);
        using var harness = new InMemoryTestHarness($"request-overloads-{NewId.NextGuid():N}")
        {
            TestTimeout = timeout,
            TestInactivityTimeout = timeout,
        };
        var scopedResponses = new TaskCompletionSource<Response<ContractResponse>[]>(TaskCreationOptions.RunContinuationsAsynchronously);

        harness.InMemoryBusConfiguring += bus => bus.Route<ContractRequest>(harness.InputQueueAddress);
        harness.InMemoryReceiveEndpointConfiguring += endpoint =>
        {
            endpoint.Handler<ContractRequest>(context => context.RespondAsync(new ContractResponse(
                context.Message.Id,
                context.Message.Kind,
                context.Headers.Get<string>(VariantHeader))));
            endpoint.Handler<ScopedRequestTrigger>(async context =>
            {
                try
                {
                    ConsumeContext consumeContext = context.Advanced();
                    Response<ContractResponse>[] responses =
                    [
                        await RequestExtensions.RequestAsync<ContractRequest, ContractResponse>(
                            consumeContext, harness.Bus, harness.InputQueueAddress, new ContractRequestMessage(5, "scoped-explicit-typed"),
                            requestTimeout, SetVariant("callback-5"), context.CancellationToken),
                        await RequestExtensions.RequestAsync<ContractRequest, ContractResponse>(
                            consumeContext, harness.Bus, harness.InputQueueAddress, (object)new { Id = 6, Kind = "scoped-explicit-values" },
                            requestTimeout, SetVariant("callback-6"), context.CancellationToken),
                        await RequestExtensions.RequestAsync<ContractRequest, ContractResponse>(
                            consumeContext, harness.Bus, new ContractRequestMessage(7, "scoped-convention-typed"),
                            requestTimeout, SetVariant("callback-7"), context.CancellationToken),
                        await RequestExtensions.RequestAsync<ContractRequest, ContractResponse>(
                            consumeContext, harness.Bus, (object)new { Id = 8, Kind = "scoped-convention-values" },
                            requestTimeout, SetVariant("callback-8"), context.CancellationToken),
                    ];

                    scopedResponses.TrySetResult(responses);
                }
                catch (Exception exception)
                {
                    scopedResponses.TrySetException(exception);
                    throw;
                }
            });
        };

        await harness.StartAsync(cancellationToken).WaitAsync(timeout, cancellationToken);
        try
        {
            Response<ContractResponse>[] busResponses =
            [
                await RequestExtensions.RequestAsync<ContractRequest, ContractResponse>(
                    harness.Bus, harness.InputQueueAddress, new ContractRequestMessage(1, "bus-explicit-typed"),
                    requestTimeout, SetVariant("callback-1"), cancellationToken),
                await RequestExtensions.RequestAsync<ContractRequest, ContractResponse>(
                    harness.Bus, harness.InputQueueAddress, (object)new { Id = 2, Kind = "bus-explicit-values" },
                    requestTimeout, SetVariant("callback-2"), cancellationToken),
                await RequestExtensions.RequestAsync<ContractRequest, ContractResponse>(
                    harness.Bus, new ContractRequestMessage(3, "bus-convention-typed"),
                    requestTimeout, SetVariant("callback-3"), cancellationToken),
                await RequestExtensions.RequestAsync<ContractRequest, ContractResponse>(
                    harness.Bus, (object)new { Id = 4, Kind = "bus-convention-values" },
                    requestTimeout, SetVariant("callback-4"), cancellationToken),
            ];

            await harness.InputQueueSendEndpoint.SendAsync(new ScopedRequestTrigger(), cancellationToken);
            Response<ContractResponse>[] consumeScopeResponses = await scopedResponses.Task.WaitAsync(timeout, cancellationToken);
            ContractResponse[] responses = busResponses.Concat(consumeScopeResponses).Select(response => response.Message).ToArray();

            Assert.Equal(Enumerable.Range(1, 8), responses.Select(response => response.Id));
            Assert.Equal(
            [
                "bus-explicit-typed",
                "bus-explicit-values",
                "bus-convention-typed",
                "bus-convention-values",
                "scoped-explicit-typed",
                "scoped-explicit-values",
                "scoped-convention-typed",
                "scoped-convention-values",
            ], responses.Select(response => response.Kind));
            Assert.Equal(Enumerable.Range(1, 8).Select(id => $"callback-{id}"), responses.Select(response => response.Callback));

            IRequestClient<ContractRequest> client = harness.Bus.CreateRequestClient<ContractRequest>(
                harness.InputQueueAddress,
                requestTimeout);
            Response<UnusedResponse, ContractResponse> initializedPair =
                await client.Advanced().GetResponseAsync<UnusedResponse, ContractResponse>(
                    new { Id = 9, Kind = "initialized-pair" },
                    cancellationToken: cancellationToken);
            Response<UnusedResponse, ContractResponse, OtherUnusedResponse> initializedTriple =
                await client.Advanced().GetResponseAsync<UnusedResponse, ContractResponse, OtherUnusedResponse>(
                    new { Id = 10, Kind = "initialized-triple" },
                    cancellationToken: cancellationToken);

            Assert.True(initializedPair.Is(out Response<ContractResponse>? pairResponse));
            Assert.Equal(new ContractResponse(9, "initialized-pair", null), pairResponse.Message);
            Assert.True(initializedTriple.Is(out Response<ContractResponse>? tripleResponse));
            Assert.Equal(new ContractResponse(10, "initialized-triple", null), tripleResponse.Message);
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken).WaitAsync(timeout, CancellationToken.None);
        }
    }

    private static Action<SendContext<ContractRequest>> SetVariant(string value) =>
        context => context.Headers.Set(VariantHeader, value);

    public interface ContractRequest
    {
        int Id { get; }
        string Kind { get; }
    }

    private sealed record ContractRequestMessage(int Id, string Kind) : ContractRequest;

    private sealed record ContractResponse(int Id, string Kind, string? Callback);

    private sealed record UnusedResponse;

    private sealed record OtherUnusedResponse;

    private sealed record ScopedRequestTrigger;
}
