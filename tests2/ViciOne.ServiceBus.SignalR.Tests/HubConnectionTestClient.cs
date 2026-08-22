using System.Buffers;
using System.IO.Pipelines;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace ViciOne.ServiceBus.SignalR.Tests;

internal sealed class HubConnectionTestClient : IAsyncDisposable, ITransferFormatFeature
{
    private readonly IInvocationBinder _invocationBinder = new TestInvocationBinder();
    private readonly IDuplexPipe _application;
    private readonly IHubProtocol _protocol;

    public HubConnectionTestClient(
        string? userIdentifier = null,
        IHubProtocol? protocol = null,
        bool failWrites = false)
    {
        _protocol = protocol ?? new JsonHubProtocol();

        var options = new PipeOptions(
            readerScheduler: PipeScheduler.Inline,
            writerScheduler: PipeScheduler.Inline,
            useSynchronizationContext: false);
        var pipes = CreateConnectionPair(options, options);
        _application = pipes.Application;

        Connection = new DefaultConnectionContext(
            Guid.NewGuid().ToString("N"),
            pipes.Transport,
            pipes.Application);
        Connection.Features.Set<ITransferFormatFeature>(this);

        HubConnection = failWrites
            ? new FailingHubConnectionContext(Connection)
            : new HubConnectionContext(
                Connection,
                new HubConnectionContextOptions { KeepAliveInterval = TimeSpan.FromSeconds(15) },
                NullLoggerFactory.Instance);
        HubConnection.Protocol = _protocol;
        HubConnection.UserIdentifier = userIdentifier;
    }

    public DefaultConnectionContext Connection { get; }

    public HubConnectionContext HubConnection { get; }

    public TransferFormat SupportedFormats { get; set; } = TransferFormat.Text | TransferFormat.Binary;

    public TransferFormat ActiveFormat { get; set; }

    public async Task<InvocationMessage> ReadInvocationAsync(
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(timeout);

        while (true)
        {
            var result = await _application.Input
                .ReadAsync(timeoutSource.Token)
                .ConfigureAwait(false);
            var buffer = result.Buffer;

            try
            {
                if (_protocol.TryParseMessage(ref buffer, _invocationBinder, out var message))
                {
                    return message as InvocationMessage
                        ?? throw new InvalidDataException(
                            $"Expected an invocation, but received {message.GetType().Name}.");
                }

                if (result.IsCompleted && buffer.IsEmpty)
                {
                    throw new EndOfStreamException("The SignalR test connection completed without an invocation.");
                }
            }
            finally
            {
                _application.Input.AdvanceTo(buffer.Start, buffer.End);
            }
        }
    }

    public InvocationMessage? TryReadInvocation()
    {
        var message = TryReadMessage();

        return message switch
        {
            null => null,
            InvocationMessage invocation => invocation,
            _ => throw new InvalidDataException(
                $"Expected an invocation, but received {message.GetType().Name}."),
        };
    }

    public async ValueTask DisposeAsync()
    {
        await _application.Output.CompleteAsync().ConfigureAwait(false);
        await _application.Input.CompleteAsync().ConfigureAwait(false);
        await Connection.DisposeAsync().ConfigureAwait(false);
    }

    private HubMessage? TryReadMessage()
    {
        if (!_application.Input.TryRead(out var result))
        {
            return null;
        }

        var buffer = result.Buffer;

        try
        {
            return _protocol.TryParseMessage(ref buffer, _invocationBinder, out var message)
                ? message
                : null;
        }
        finally
        {
            _application.Input.AdvanceTo(buffer.Start, buffer.End);
        }
    }

    private static DuplexPipePair CreateConnectionPair(PipeOptions inputOptions, PipeOptions outputOptions)
    {
        var input = new global::System.IO.Pipelines.Pipe(inputOptions);
        var output = new global::System.IO.Pipelines.Pipe(outputOptions);

        return new DuplexPipePair(
            new DuplexPipe(output.Reader, input.Writer),
            new DuplexPipe(input.Reader, output.Writer));
    }

    private sealed class FailingHubConnectionContext(ConnectionContext connectionContext)
        : HubConnectionContext(
            connectionContext,
            new HubConnectionContextOptions
            {
                KeepAliveInterval = TimeSpan.FromSeconds(15),
                ClientTimeoutInterval = TimeSpan.FromSeconds(15),
            },
            NullLoggerFactory.Instance)
    {
        public override ValueTask WriteAsync(
            HubMessage message,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromException(new InvalidOperationException("Intentional SignalR write failure."));

        public override ValueTask WriteAsync(
            SerializedHubMessage message,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromException(new InvalidOperationException("Intentional SignalR write failure."));
    }

    private sealed class TestInvocationBinder : IInvocationBinder
    {
        public IReadOnlyList<Type> GetParameterTypes(string methodName) => [typeof(object)];

        public Type GetStreamItemType(string streamId) => typeof(object);

        public Type GetReturnType(string invocationId) => typeof(object);
    }

    private sealed class DuplexPipe(PipeReader input, PipeWriter output) : IDuplexPipe
    {
        public PipeReader Input { get; } = input;

        public PipeWriter Output { get; } = output;
    }

    private readonly record struct DuplexPipePair(IDuplexPipe Transport, IDuplexPipe Application);
}
