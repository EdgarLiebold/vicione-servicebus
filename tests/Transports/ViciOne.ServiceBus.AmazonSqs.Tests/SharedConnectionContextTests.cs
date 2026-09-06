using ViciOne.ServiceBus.AmazonSqs.Tests.TestDoubles;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.AmazonSqs.Tests;

public sealed class SharedConnectionContextTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-AWS-SQS-CANCELLATION", "cache-removal-links-shared-lifetime")]
    public async Task CacheRemoval_LinksTheSharedLifetimeToTheUnderlyingOperationAsync(bool topic)
    {
        CancellationToken observedToken = default;
        ConnectionContext inner = InterfaceProxy<ConnectionContext>.Create((method, args) => method.Name switch
        {
            nameof(ConnectionContext.RemoveQueueByNameAsync) => RecordTokenAsync(args, token => observedToken = token),
            nameof(ConnectionContext.RemoveTopicByNameAsync) => RecordTokenAsync(args, token => observedToken = token),
            _ => Default(method.ReturnType),
        });
        using var sharedLifetime = new CancellationTokenSource();
        var context = new SharedConnectionContext(inner, sharedLifetime.Token);

        Task<bool> removal = topic
            ? context.RemoveTopicByNameAsync("orders", CancellationToken.None)
            : context.RemoveQueueByNameAsync("orders", CancellationToken.None);
        Assert.False(removal.IsCompleted);

        sharedLifetime.Cancel();

        OperationCanceledException exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => removal.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
        Assert.True(observedToken.CanBeCanceled);
        Assert.True(observedToken.IsCancellationRequested);
        Assert.Equal(observedToken, exception.CancellationToken);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-AWS-SQS-CANCELLATION", "client-context-links-both-lifetimes")]
    public void CreatedClientContext_LinksSharedAndCallerLifetimes(bool cancelSharedLifetime)
    {
        ClientContext? createdInner = null;
        ConnectionContext inner = InterfaceProxy<ConnectionContext>.Create((method, args) => method.Name switch
        {
            nameof(ConnectionContext.CreateClientContext) => createdInner = CreateClientContext(Assert.IsType<CancellationToken>(args![0])),
            _ => Default(method.ReturnType),
        });
        using var sharedLifetime = new CancellationTokenSource();
        using var callerLifetime = new CancellationTokenSource();
        var context = new SharedConnectionContext(inner, sharedLifetime.Token);
        ClientContext created = context.CreateClientContext(callerLifetime.Token);

        if (cancelSharedLifetime)
            sharedLifetime.Cancel();
        else
            callerLifetime.Cancel();

        Assert.NotNull(createdInner);
        Assert.True(created.CancellationToken.IsCancellationRequested);
        (created as IDisposable)?.Dispose();
    }

    private static ClientContext CreateClientContext(CancellationToken cancellationToken)
    {
        return InterfaceProxy<ClientContext>.Create((method, _) => method.Name switch
        {
            "get_CancellationToken" => cancellationToken,
            _ => Default(method.ReturnType),
        });
    }

    private static Task<bool> RecordTokenAsync(object?[]? arguments, Action<CancellationToken> record)
    {
        CancellationToken cancellationToken = Assert.IsType<CancellationToken>(arguments![1]);
        record(cancellationToken);
        return WaitForCancellationAsync(cancellationToken);
    }

    private static async Task<bool> WaitForCancellationAsync(CancellationToken cancellationToken)
    {
        await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
        return false;
    }

    private static object? Default(Type returnType)
    {
        return returnType.IsValueType ? Activator.CreateInstance(returnType) : null;
    }
}
