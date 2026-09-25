using System.Reflection;
using RabbitMQ.Client;
using Xunit;

namespace ViciOne.ServiceBus.RabbitMq.Tests.RabbitMqTransport;

public sealed class RabbitMqCleanupTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ChannelCleanup_ClosesOnlyOpenChannelsThenDisposesAsync(bool isOpen)
    {
        IChannel channel = DispatchProxy.Create<IChannel, CleanupProxy>();
        var proxy = (CleanupProxy)channel;
        proxy.IsOpen = isOpen;
        using var cancellation = new CancellationTokenSource();

        await channel.CleanupAsync(503, "shutdown", cancellation.Token);

        Assert.Equal(isOpen ? ["close", "dispose"] : ["dispose"], proxy.Calls);
        if (isOpen)
        {
            Assert.Equal((ushort)503, proxy.ReplyCode);
            Assert.Equal("shutdown", proxy.Message);
            Assert.Equal(cancellation.Token, proxy.CloseToken);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ChannelCleanup_AttemptsDisposalAfterCloseFailureAndSuppressesBothFailuresAsync(bool disposeFails)
    {
        IChannel channel = DispatchProxy.Create<IChannel, CleanupProxy>();
        var proxy = (CleanupProxy)channel;
        proxy.CloseFailure = new InvalidOperationException("broker close failed");
        if (disposeFails)
            proxy.DisposeFailure = new InvalidOperationException("socket disposal failed");

        await channel.CleanupAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(["close", "dispose"], proxy.Calls);
        Assert.Equal((ushort)200, proxy.ReplyCode);
        Assert.Equal("Unknown", proxy.Message);
    }

    [Fact]
    public async Task ChannelCleanup_DisposesEvenWhenOpenStateCannotBeReadAsync()
    {
        IChannel channel = DispatchProxy.Create<IChannel, CleanupProxy>();
        var proxy = (CleanupProxy)channel;
        proxy.IsOpenFailure = new InvalidOperationException("channel status unavailable");

        await channel.CleanupAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(["dispose"], proxy.Calls);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ConnectionCleanup_ClosesOnlyOpenConnectionsThenDisposesAsync(bool isOpen)
    {
        IConnection connection = DispatchProxy.Create<IConnection, CleanupProxy>();
        var proxy = (CleanupProxy)connection;
        proxy.IsOpen = isOpen;
        using var cancellation = new CancellationTokenSource();

        await connection.CleanupAsync(504, "connection shutdown", cancellation.Token);

        Assert.Equal(isOpen ? ["close", "dispose"] : ["dispose"], proxy.Calls);
        if (isOpen)
        {
            Assert.Equal((ushort)504, proxy.ReplyCode);
            Assert.Equal("connection shutdown", proxy.Message);
            Assert.Equal(cancellation.Token, proxy.CloseToken);
        }
    }

    [Fact]
    public async Task ConnectionCleanup_CloseFailureStillDisposesAndPreservesDisposalFailureAsync()
    {
        IConnection connection = DispatchProxy.Create<IConnection, CleanupProxy>();
        var proxy = (CleanupProxy)connection;
        proxy.CloseFailure = new InvalidOperationException("broker close failed");
        var disposalFailure = new InvalidOperationException("socket disposal failed");
        proxy.DisposeFailure = disposalFailure;

        var actual = await Assert.ThrowsAsync<InvalidOperationException>(
            () => connection.CleanupAsync(cancellationToken: TestContext.Current.CancellationToken));

        Assert.Same(disposalFailure, actual);
        Assert.Equal(["close", "dispose"], proxy.Calls);
    }

    [Fact]
    public async Task ConnectionCleanup_SuppressesCloseFailureWhenDisposalSucceedsAsync()
    {
        IConnection connection = DispatchProxy.Create<IConnection, CleanupProxy>();
        var proxy = (CleanupProxy)connection;
        proxy.CloseFailure = new InvalidOperationException("broker close failed");

        await connection.CleanupAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(["close", "dispose"], proxy.Calls);
    }

    [Fact]
    public async Task ConnectionCleanup_DisposesEvenWhenOpenStateCannotBeReadAsync()
    {
        IConnection connection = DispatchProxy.Create<IConnection, CleanupProxy>();
        var proxy = (CleanupProxy)connection;
        proxy.IsOpenFailure = new InvalidOperationException("connection status unavailable");

        await connection.CleanupAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(["dispose"], proxy.Calls);
    }

    [Fact]
    public async Task ConnectionCleanup_NullConnectionHasNoWorkAsync()
    {
        IConnection? connection = null;

        await connection.CleanupAsync(cancellationToken: TestContext.Current.CancellationToken);
    }

    public class CleanupProxy : DispatchProxy
    {
        public bool IsOpen { get; set; } = true;
        public List<string> Calls { get; } = [];
        public Exception? CloseFailure { get; set; }
        public Exception? IsOpenFailure { get; set; }
        public Exception? DisposeFailure { get; set; }
        public ushort ReplyCode { get; private set; }
        public string? Message { get; private set; }
        public CancellationToken CloseToken { get; private set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            return targetMethod?.Name switch
            {
                "get_IsOpen" => IsOpenFailure is null ? IsOpen : throw IsOpenFailure,
                "CloseAsync" => CloseAsync(args),
                "DisposeAsync" => DisposeAsync(),
                _ => throw new InvalidOperationException($"Unexpected RabbitMQ client call: {targetMethod?.Name}"),
            };
        }

        private Task CloseAsync(object?[]? args)
        {
            Calls.Add("close");
            Assert.NotNull(args);
            Assert.True(args.Length >= 3, "CloseAsync did not receive reply code, message, and cancellation token.");
            ReplyCode = Assert.IsType<ushort>(args[0]);
            Message = Assert.IsType<string>(args[1]);
            CloseToken = Assert.IsType<CancellationToken>(args[^1]);
            return CloseFailure is null ? Task.CompletedTask : Task.FromException(CloseFailure);
        }

        private ValueTask DisposeAsync()
        {
            Calls.Add("dispose");
            return DisposeFailure is null ? ValueTask.CompletedTask : ValueTask.FromException(DisposeFailure);
        }
    }
}
