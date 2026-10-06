using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SimpleNotificationService;
using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Logging;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Advanced.Middleware;
using ViciOne.ServiceBus.AmazonSqs.Middleware;
using ViciOne.ServiceBus.AmazonSqs.Tests.TestDoubles;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.AmazonSqs.Tests;

public class AmazonSqsStartupPurgeDiagnosticsTests
{
    const string QueueName = "startup-purge-diagnostics";
    const string QueueUrl = "https://sqs.us-east-1.amazonaws.com/123456789012/startup-purge-diagnostics";
    static readonly TimeSpan CleanupWait = TimeSpan.FromSeconds(10);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-AWS-SQS-LIFECYCLE", "successful-startup-purge-survives-own-debug-and-keeps-once-state")]
    public async Task StartupPurge_SuccessPreservesContinuationAndOnceStateDespiteDebugAsync(bool hostileLogger)
    {
        using var lifetime = new CancellationTokenSource();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var provider = new TaskCompletionSource<PurgeQueueResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
        var response = new PurgeQueueResponse { HttpStatusCode = HttpStatusCode.OK };
        var lookups = new ConcurrentQueue<(string Name, CancellationToken Token)>();
        var requests = new ConcurrentQueue<(string Url, CancellationToken Token)>();
        var forwarded = new ConcurrentQueue<ClientContext>();
        var loggerFailure = new ApplicationException("unique startup-purge Debug failure");
        var logger = new PurgeLogger(hostileLogger, loggerFailure);
        var previousLogContext = LogContext.Current;
        Task? operation = null;
        Task? repeatedOperation = null;

        var sqs = InterfaceProxy<IAmazonSQS>.Create((method, arguments) =>
        {
            if (method.Name == nameof(IAmazonSQS.PurgeQueueAsync))
            {
                requests.Enqueue(((string)arguments![0]!, (CancellationToken)arguments[1]!));
                entered.TrySetResult();
                return provider.Task;
            }

            throw new NotSupportedException(method.Name);
        });
        var sns = InterfaceProxy<IAmazonSimpleNotificationService>.Create((method, _) =>
            throw new NotSupportedException(method.Name));
        var queue = new QueueInfo(QueueName, QueueUrl,
            new Dictionary<string, string> { [QueueAttributeName.QueueArn] = "arn:aws:sqs:us-east-1:123456789012:startup-purge-diagnostics" },
            sqs, lifetime.Token, true);
        try
        {
            var connection = InterfaceProxy<ConnectionContext>.Create((method, arguments) =>
            {
                if (method.Name == nameof(ConnectionContext.GetQueueByNameAsync))
                {
                    lookups.Enqueue(((string)arguments![0]!, (CancellationToken)arguments[1]!));
                    return Task.FromResult(queue);
                }

                throw new NotSupportedException(method.Name);
            });
            var context = new AmazonSqsClientContext(connection, sqs, sns, lifetime.Token);
            var next = InterfaceProxy<IPipe<ClientContext>>.Create((method, arguments) =>
            {
                if (method.Name == nameof(IPipe<ClientContext>.SendAsync))
                {
                    forwarded.Enqueue((ClientContext)arguments![0]!);
                    return Task.CompletedTask;
                }

                throw new NotSupportedException(method.Name);
            });
            IFilter<ClientContext> filter = new PurgeOnStartupFilter(QueueName);

            LogContext.ConfigureCurrentLogContext(logger);
            operation = filter.SendAsync(context, next);
            await entered.Task.WaitAsync(CleanupWait, CancellationToken.None);

            Assert.False(provider.Task.IsCompleted);
            Assert.False(operation.IsCompleted);
            Assert.Empty(forwarded);
            var lookup = Assert.Single(lookups);
            Assert.Equal(QueueName, lookup.Name);
            Assert.Equal(lifetime.Token, lookup.Token);
            var request = Assert.Single(requests);
            Assert.Equal(QueueUrl, request.Url);
            Assert.Equal(lifetime.Token, request.Token);

            provider.TrySetResult(response);
            var observed = await Record.ExceptionAsync(() => operation.WaitAsync(CleanupWait, CancellationToken.None));

            Assert.True(provider.Task.IsCompletedSuccessfully);
            Assert.Same(response, await provider.Task);
            Assert.True(operation.IsCompleted);
            var emission = Assert.Single(logger.Emissions);
            Assert.Equal("Purged queue {QueueName}", emission["{OriginalFormat}"]);
            Assert.Equal(QueueName, emission["QueueName"]);
            Assert.Equal(hostileLogger ? 1 : 0, logger.ThrowCount);
            if (observed is not null)
                Assert.Same(loggerFailure, observed);

            Assert.Null(observed);
            Assert.Same(context, Assert.Single(forwarded));

            repeatedOperation = filter.SendAsync(context, next);
            await repeatedOperation.WaitAsync(CleanupWait, CancellationToken.None);
            Assert.True(repeatedOperation.IsCompletedSuccessfully);
            Assert.Single(lookups);
            Assert.Single(requests);
            Assert.Single(logger.Emissions);
            Assert.Equal(2, forwarded.Count);
            Assert.All(forwarded, value => Assert.Same(context, value));
        }
        finally
        {
            provider.TrySetResult(response);
            try
            {
                await ObserveTerminalAsync(provider.Task);
            }
            finally
            {
                try
                {
                    await ObserveTerminalAsync(operation);
                }
                finally
                {
                    try
                    {
                        await ObserveTerminalAsync(repeatedOperation);
                    }
                    finally
                    {
                        try
                        {
                            await queue.DisposeAsync().AsTask().WaitAsync(CleanupWait, CancellationToken.None);
                        }
                        finally
                        {
                            LogContext.Current = previousLogContext;
                        }
                    }
                }
            }
        }
    }

    static async Task ObserveTerminalAsync(Task? task)
    {
        if (task is null)
            return;

        try
        {
            await task.WaitAsync(CleanupWait, CancellationToken.None);
        }
        catch (Exception exception) when (exception is not TimeoutException
            && ((task.IsCanceled && exception is OperationCanceledException)
                || (task.IsFaulted && task.Exception!.InnerExceptions.Any(cause => ReferenceEquals(cause, exception)))))
        {
        }
    }

    sealed class PurgeLogger(bool hostile, Exception failure) : ILogger
    {
        int _throwCount;

        public ConcurrentQueue<Dictionary<string, object?>> Emissions { get; } = new();
        public int ThrowCount => Volatile.Read(ref _throwCount);

        public bool IsEnabled(LogLevel logLevel) => logLevel == LogLevel.Debug;

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => new EmptyScope();

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (logLevel != LogLevel.Debug || state is not IEnumerable<KeyValuePair<string, object?>> fields)
                return;

            var values = fields.ToDictionary(value => value.Key, value => value.Value);
            if (!values.TryGetValue("{OriginalFormat}", out var template)
                || !Equals(template, "Purged queue {QueueName}"))
                return;

            Emissions.Enqueue(values);
            if (hostile)
            {
                Interlocked.Increment(ref _throwCount);
                throw failure;
            }
        }

        sealed class EmptyScope : IDisposable
        {
            public void Dispose()
            {
            }
        }
    }
}
