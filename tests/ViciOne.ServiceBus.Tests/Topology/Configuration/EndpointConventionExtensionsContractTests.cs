using System.Reflection;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Topology.Configuration;

public sealed class EndpointConventionExtensionsContractTests
{
    private const string VariantHeader = "ViciOne-Convention-Variant";

    [Fact]
    [RequirementCoverage("REQ-VSB-ENDPOINT-CONVENTION", "every-send-overload-validates-required-inputs")]
    public async Task EverySendOverload_RejectsEachMissingRequiredInputBeforeResolvingARouteAsync()
    {
        ISendEndpointProvider provider = DispatchProxy.Create<ISendEndpointProvider, UnexpectedInvocationProxy>();
        var message = new ConventionDispatchMessage(1, "message");
        IPipe<SendContext<ConventionDispatchContract>> typedPipe = Pipe.Empty<SendContext<ConventionDispatchContract>>();
        IPipe<SendContext> untypedPipe = Pipe.Empty<SendContext>();

        await AssertParameterAsync("provider", () => EndpointConventionExtensions.SendAsync<ConventionDispatchContract>(null!, message));
        await AssertParameterAsync("message", () => EndpointConventionExtensions.SendAsync<ConventionDispatchContract>(provider, null!));

        await AssertParameterAsync("provider", () => EndpointConventionExtensions.SendAsync(null!, (object)message));
        await AssertParameterAsync("message", () => EndpointConventionExtensions.SendAsync(provider, (object)null!));
        await AssertParameterAsync("messageType", () => EndpointConventionExtensions.SendAsync(provider, message, (Type)null!));

        await AssertParameterAsync("pipe", () =>
            EndpointConventionExtensions.SendAsync<ConventionDispatchContract>(provider, message, (IPipe<SendContext<ConventionDispatchContract>>)null!));
        await AssertParameterAsync("pipe", () =>
            EndpointConventionExtensions.SendAsync<ConventionDispatchContract>(provider, message, (IPipe<SendContext>)null!));
        await AssertParameterAsync("pipe", () => EndpointConventionExtensions.SendAsync(provider, message, (IPipe<SendContext>)null!));
        await AssertParameterAsync("messageType", () => EndpointConventionExtensions.SendAsync(provider, message, null!, untypedPipe));
        await AssertParameterAsync("pipe", () => EndpointConventionExtensions.SendAsync(provider, message, typeof(ConventionDispatchContract), null!));

        await AssertParameterAsync("values", () =>
            EndpointConventionExtensions.SendAsync<ConventionDispatchContract>(provider, (object)null!));
        await AssertParameterAsync("values", () =>
            EndpointConventionExtensions.SendAsync<ConventionDispatchContract>(provider, (object)null!, typedPipe));
        await AssertParameterAsync("values", () =>
            EndpointConventionExtensions.SendAsync<ConventionDispatchContract>(provider, (object)null!, untypedPipe));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ENDPOINT-CONVENTION", "every-send-overload-routes-exact-message-pipe-and-runtime-contract")]
    public async Task EverySendOverload_RoutesItsExactMessagePipeAndRuntimeContractAsync()
    {
        TimeSpan timeout = TestConfigurationProvider.ForCurrentTestRun().GetValidatedOptions().OperationTimeout!.Value;
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = new InMemoryTestHarness($"convention-overloads-{NewId.NextGuid():N}")
        {
            TestTimeout = timeout,
            TestInactivityTimeout = timeout,
        };
        TaskCompletionSource<ConsumeContext<ConventionDispatchContract>>[] received = Enumerable.Range(0, 11)
            .Select(_ => new TaskCompletionSource<ConsumeContext<ConventionDispatchContract>>(TaskCreationOptions.RunContinuationsAsynchronously))
            .ToArray();

        harness.InMemoryBusConfiguring += bus => bus.Route<ConventionDispatchContract>(harness.InputQueueAddress);
        harness.InMemoryReceiveEndpointConfiguring += endpoint => endpoint.Handler<ConventionDispatchContract>(context =>
        {
            received[context.Message.Id].TrySetResult(context);
            return Task.CompletedTask;
        });

        await harness.StartAsync(cancellationToken).WaitAsync(timeout, cancellationToken);
        try
        {
            ISendEndpointProvider provider = harness.Bus;

            await EndpointConventionExtensions.SendAsync<ConventionDispatchContract>(
                provider, new ConventionDispatchMessage(1, "typed"), cancellationToken);
            await EndpointConventionExtensions.SendAsync(
                provider, (ConventionDispatchContract)new ConventionDispatchMessage(2, "typed-pipe"),
                HeaderPipe<ConventionDispatchContract>("typed"), cancellationToken);
            await EndpointConventionExtensions.SendAsync(
                provider, (ConventionDispatchContract)new ConventionDispatchMessage(3, "untyped-pipe"),
                UntypedHeaderPipe("untyped"), cancellationToken);
            await EndpointConventionExtensions.SendAsync(
                provider, (object)new ConventionDispatchMessage(4, "runtime"), cancellationToken);
            await EndpointConventionExtensions.SendAsync(
                provider, new ConventionDispatchMessage(5, "explicit-runtime"), typeof(ConventionDispatchContract), cancellationToken);
            await EndpointConventionExtensions.SendAsync(
                provider, (object)new ConventionDispatchMessage(6, "runtime-pipe"), UntypedHeaderPipe("runtime"), cancellationToken);
            await EndpointConventionExtensions.SendAsync(
                provider, new ConventionDispatchMessage(7, "explicit-runtime-pipe"), typeof(ConventionDispatchContract),
                UntypedHeaderPipe("explicit"), cancellationToken);
            await EndpointConventionExtensions.SendAsync<ConventionDispatchContract>(
                provider, new { Id = 8, Kind = "values" }, cancellationToken);
            await EndpointConventionExtensions.SendAsync<ConventionDispatchContract>(
                provider, new { Id = 9, Kind = "values-typed-pipe" }, HeaderPipe<ConventionDispatchContract>("values-typed"), cancellationToken);
            await EndpointConventionExtensions.SendAsync<ConventionDispatchContract>(
                provider, new { Id = 10, Kind = "values-untyped-pipe" }, UntypedHeaderPipe("values-untyped"), cancellationToken);

            ConsumeContext<ConventionDispatchContract>[] contexts = await Task.WhenAll(received.Skip(1).Select(signal => signal.Task))
                .WaitAsync(timeout, cancellationToken);

            Assert.Equal(Enumerable.Range(1, 10), contexts.Select(context => context.Message.Id));
            Assert.Equal(
            [
                "typed",
                "typed-pipe",
                "untyped-pipe",
                "runtime",
                "explicit-runtime",
                "runtime-pipe",
                "explicit-runtime-pipe",
                "values",
                "values-typed-pipe",
                "values-untyped-pipe",
            ], contexts.Select(context => context.Message.Kind));
            Assert.Equal(
            [
                null,
                "typed",
                "untyped",
                null,
                null,
                "runtime",
                "explicit",
                null,
                "values-typed",
                "values-untyped",
            ], contexts.Select(context => context.Headers.Get<string>(VariantHeader)));
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken).WaitAsync(timeout, CancellationToken.None);
        }
    }

    private static IPipe<SendContext<T>> HeaderPipe<T>(string value)
        where T : class => Pipe.Execute<SendContext<T>>(context => context.Headers.Set(VariantHeader, value));

    private static IPipe<SendContext> UntypedHeaderPipe(string value) =>
        Pipe.Execute<SendContext>(context => context.Headers.Set(VariantHeader, value));

    private static async Task AssertParameterAsync(string expectedParameterName, Func<Task> operation)
    {
        ArgumentNullException exception = await Assert.ThrowsAsync<ArgumentNullException>(operation);

        Assert.Equal(expectedParameterName, exception.ParamName);
    }

    public interface ConventionDispatchContract
    {
        int Id { get; }
        string Kind { get; }
    }

    private sealed record ConventionDispatchMessage(int Id, string Kind) : ConventionDispatchContract;

    private class UnexpectedInvocationProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new InvalidOperationException($"The null boundary invoked {targetMethod?.Name}.");
    }
}
