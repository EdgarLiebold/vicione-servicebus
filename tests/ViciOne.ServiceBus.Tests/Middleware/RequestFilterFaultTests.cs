using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Middleware;

public sealed class RequestFilterFaultTests
{
    private const string ValidationFailure = "The request was rejected by validation.";

    [Fact]
    [RequirementCoverage("REQ-VSB-REQUEST-FILTER", "scoped-filter-faults-request-before-consumer")]
    public async Task ScopedConsumeFilter_ProducesAnExactRequestFaultWithoutInvokingTheConsumer()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var recorder = new FilterRecorder();
        var consumer = new ConsumerInvocationCounter();
        await using ServiceProvider provider = new ServiceCollection()
            .AddSingleton(recorder)
            .AddSingleton(consumer)
            .AddViciOneServiceBusTestHarness(configuration =>
            {
                configuration.SetTestTimeouts(timeout, timeout);
                configuration.AddConsumer<ValidatedRequestConsumer>();
                configuration.UsingInMemory((context, configurator) =>
                {
                    configurator.UseConsumeFilter(typeof(RequestValidationScopedFilter<>), context);
                    configurator.ConfigureEndpoints(context);
                });
            })
            .BuildServiceProvider(new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true,
            });
        ITestHarness harness = await provider.StartTestHarness().WaitAsync(timeout, cancellationToken);

        try
        {
            var request = new ValidatedRequest(Guid.Parse("889c7912-2ac2-4d5f-b00f-d4f67e675dbd"));
            IRequestClient<ValidatedRequest> client = harness.GetRequestClient<ValidatedRequest>();

            RequestFaultException exception = await Assert.ThrowsAsync<RequestFaultException>(() =>
                client.GetResponse<ValidatedResponse>(request, cancellationToken));
            FilterObservation observation = await recorder.Observed.Task.WaitAsync(timeout, cancellationToken);
            ExceptionInfo fault = Assert.Single(exception.Fault!.Exceptions);

            Assert.Equal(typeof(ValidatedRequest), observation.MessageType);
            Assert.Equal(request.CorrelationId, observation.CorrelationId);
            Assert.Equal(0, consumer.Count);
            Assert.Equal(TypeCache<RequestValidationException>.ShortName, fault.ExceptionType);
            Assert.Equal(ValidationFailure, fault.Message);
            Assert.Equal(request, Assert.IsAssignableFrom<Fault<ValidatedRequest>>(exception.Fault).Message);
        }
        finally
        {
            await harness.Stop(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    private static TimeSpan OperationTimeout() => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions()
        .OperationTimeout!.Value;

    public sealed record ValidatedRequest(Guid CorrelationId) : CorrelatedBy<Guid>;

    public sealed record ValidatedResponse(Guid CorrelationId) : CorrelatedBy<Guid>;

    public sealed record FilterObservation(Type MessageType, Guid CorrelationId);

    public sealed class RequestValidationScopedFilter<T>(FilterRecorder recorder) : IFilter<ConsumeContext<T>>
        where T : class
    {
        public void Probe(ProbeContext context) =>
            context.CreateFilterScope("requestValidation");

        public async Task Send(ConsumeContext<T> context, IPipe<ConsumeContext<T>> next)
        {
            Guid correlationId = context.CorrelationId
                ?? throw new InvalidOperationException("The validation request must have a correlation identifier.");
            recorder.Observed.TrySetResult(new FilterObservation(typeof(T), correlationId));
            var exception = new RequestValidationException(ValidationFailure);
            await context.NotifyFaulted(
                context.ReceiveContext.ElapsedTime,
                TypeCache<RequestValidationScopedFilter<T>>.ShortName,
                exception);
            throw exception;
        }
    }

    public sealed class ValidatedRequestConsumer(ConsumerInvocationCounter counter) : IConsumer<ValidatedRequest>
    {
        public Task Consume(ConsumeContext<ValidatedRequest> context)
        {
            counter.Increment();
            return context.RespondAsync(new ValidatedResponse(context.Message.CorrelationId));
        }
    }

    public sealed class FilterRecorder
    {
        public TaskCompletionSource<FilterObservation> Observed { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    public sealed class ConsumerInvocationCounter
    {
        private int _count;

        public int Count => Volatile.Read(ref _count);

        public void Increment() => Interlocked.Increment(ref _count);
    }

    public sealed class RequestValidationException(string message) : Exception(message);
}
