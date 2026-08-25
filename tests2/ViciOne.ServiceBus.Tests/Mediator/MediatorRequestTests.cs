using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Mediator;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.Testing;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Mediator;

public sealed class MediatorRequestTests
{
    private static readonly TimeSpan VirtualTimeSafetyTimeout = TimeSpan.FromSeconds(2);
    private static readonly DateTimeOffset StartTime =
        new(2033, 5, 6, 7, 8, 9, TimeSpan.Zero);

    [Fact]
    [RequirementCoverage("REQ-VSB-MEDIATOR-REQUEST", "two-request-contracts-one-consumer")]
    public async Task SendRequest_ServesTwoTypedRequestContractsWithExactResponses()
    {
        await using ServiceProvider provider = new ServiceCollection()
            .AddMediator(configurator => configurator.AddConsumer<UserRequestConsumer>())
            .BuildServiceProvider(validateScopes: true);
        IMediator mediator = provider.GetRequiredService<IMediator>();

        User byUsername = await mediator.SendRequest(
            new UserFromUsername("phatboyg"),
            TestContext.Current.CancellationToken);
        User byEmail = await mediator.SendRequest(
            new UserFromEmail("phatboyg@gmail.com"),
            TestContext.Current.CancellationToken);

        Assert.Equal(new User(123, "phatboyg", "phatboyg@compuserve.net"), byUsername);
        Assert.Equal(new User(123, "phatboyg", "phatboyg@gmail.com"), byEmail);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MEDIATOR-REQUEST", "consumer-exception-unwrapped")]
    public async Task SendRequest_UnwrapsTheOriginalConsumerExceptionExactly()
    {
        await using ServiceProvider provider = new ServiceCollection()
            .AddMediator(configurator => configurator.AddConsumer<UserRequestConsumer>())
            .BuildServiceProvider(validateScopes: true);
        IMediator mediator = provider.GetRequiredService<IMediator>();

        UserNotFoundException exception = await Assert.ThrowsAsync<UserNotFoundException>(() =>
            mediator.SendRequest(
                new UserFromUsername("missing"),
                TestContext.Current.CancellationToken));

        Assert.Equal("User not found: missing", exception.Message);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MEDIATOR-REQUEST", "virtual-deadline")]
    public async Task MissingMediatorResponse_ExpiresOnlyWhenTheInjectedTimeProviderAdvances()
    {
        var timeProvider = new ObservableTimeProvider(StartTime);
        IMediator mediator = Bus.Factory.CreateMediator(
            null,
            configurator => configurator.Handler<PendingRequest>(_ => Task.CompletedTask),
            timeProvider);
        await using IAsyncDisposable lifetime = Assert.IsAssignableFrom<IAsyncDisposable>(mediator);
        IRequestClient<PendingRequest> client = mediator.CreateRequestClient<PendingRequest>(RequestTimeout.After(m: 1));
        var message = new PendingRequest("no-response");
        using RequestHandle<PendingRequest> request = client.Create(
            message,
            TestContext.Current.CancellationToken);
        var concreteRequest = Assert.IsType<ViciOne.ServiceBus.Clients.ClientRequestHandle<PendingRequest>>(request);
        Task<Response<PendingResponse>> response = request.GetResponse<PendingResponse>();

        await timeProvider.WaitForTimerCount(1).WaitAsync(
            VirtualTimeSafetyTimeout,
            TestContext.Current.CancellationToken);
        Assert.Same(message, await request.Message.WaitAsync(
            VirtualTimeSafetyTimeout,
            TestContext.Current.CancellationToken));
        Assert.False(response.IsCompleted);

        timeProvider.Advance(TimeSpan.FromMinutes(1));

        RequestTimeoutException exception =
            await Assert.ThrowsAsync<RequestTimeoutException>(() => response.WaitAsync(
                VirtualTimeSafetyTimeout,
                TestContext.Current.CancellationToken));
        Assert.Equal(
            $"Timeout waiting for response, RequestId: {concreteRequest.RequestId}",
            exception.Message);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MEDIATOR-REQUEST", "dependency-injection-time-provider")]
    public async Task DependencyInjectionMediator_UsesTheRegisteredTimeProviderForRequestDeadlines()
    {
        var timeProvider = new ObservableTimeProvider(StartTime);
        await using ServiceProvider provider = new ServiceCollection()
            .AddSingleton<TimeProvider>(timeProvider)
            .AddMediator(configurator => configurator.AddConsumer<PendingRequestConsumer>())
            .BuildServiceProvider(new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true,
            });
        IMediator mediator = provider.GetRequiredService<IMediator>();
        IRequestClient<PendingRequest> client = mediator.CreateRequestClient<PendingRequest>(RequestTimeout.After(m: 1));
        var message = new PendingRequest("di-time-provider");
        using RequestHandle<PendingRequest> request = client.Create(
            message,
            TestContext.Current.CancellationToken);
        Task<Response<PendingResponse>> response = request.GetResponse<PendingResponse>();

        await timeProvider.WaitForTimerCount(1).WaitAsync(
            VirtualTimeSafetyTimeout,
            TestContext.Current.CancellationToken);
        Assert.Same(message, await request.Message.WaitAsync(
            VirtualTimeSafetyTimeout,
            TestContext.Current.CancellationToken));
        Assert.False(response.IsCompleted);

        timeProvider.Advance(TimeSpan.FromMinutes(1));

        var concreteRequest = Assert.IsType<ViciOne.ServiceBus.Clients.ClientRequestHandle<PendingRequest>>(request);
        RequestTimeoutException exception = await Assert.ThrowsAsync<RequestTimeoutException>(() => response.WaitAsync(
            VirtualTimeSafetyTimeout,
            TestContext.Current.CancellationToken));
        Assert.Equal(
            $"Timeout waiting for response, RequestId: {concreteRequest.RequestId}",
            exception.Message);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MEDIATOR-REQUEST", "missing-handler-owned-task-failures")]
    public async Task MissingMediatorHandler_FaultsEveryPublicRequestTaskWithItsOwnedFailure()
    {
        IMediator mediator = Bus.Factory.CreateMediator(_ => { });
        await using IAsyncDisposable lifetime = Assert.IsAssignableFrom<IAsyncDisposable>(mediator);
        IRequestClient<PendingRequest> client = mediator.CreateRequestClient<PendingRequest>(OperationTimeout());
        using RequestHandle<PendingRequest> request = client.Create(
            new PendingRequest("missing-handler"),
            TestContext.Current.CancellationToken);
        Task<Response<PendingResponse>> response = request.GetResponse<PendingResponse>();

        RequestException responseException =
            await Assert.ThrowsAsync<RequestException>(() => response);
        MessageNotConsumedException sendException =
            await Assert.ThrowsAsync<MessageNotConsumedException>(() => request.Message);

        Assert.Same(sendException, responseException.InnerException);
        Assert.Contains("not consumed", sendException.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static TimeSpan OperationTimeout() =>
        Infrastructure.Configuration.TestConfigurationProvider.ForCurrentTestRun()
            .GetValidatedOptions()
            .OperationTimeout!.Value;

    private sealed record UserFromEmail(string Email) : Request<User>;

    private sealed record UserFromUsername(string Username) : Request<User>;

    private sealed record User(int Id, string Username, string Email);

    private sealed record PendingRequest(string Value);

    private sealed record PendingResponse(string Value);

    private sealed class UserRequestConsumer :
        IConsumer<UserFromEmail>,
        IConsumer<UserFromUsername>
    {
        public Task Consume(ConsumeContext<UserFromEmail> context) =>
            context.RespondAsync(new User(
                123,
                context.Message.Email.Split('@')[0],
                context.Message.Email));

        public Task Consume(ConsumeContext<UserFromUsername> context)
        {
            if (context.Message.Username == "missing")
                throw new UserNotFoundException("User not found: missing");

            return context.RespondAsync(new User(
                123,
                context.Message.Username,
                $"{context.Message.Username}@compuserve.net"));
        }
    }

    private sealed class PendingRequestConsumer : IConsumer<PendingRequest>
    {
        public Task Consume(ConsumeContext<PendingRequest> context) => Task.CompletedTask;
    }

    private sealed class UserNotFoundException(string message) : Exception(message);
}
