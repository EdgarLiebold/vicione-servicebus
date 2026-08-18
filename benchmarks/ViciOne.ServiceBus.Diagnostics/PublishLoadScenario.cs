namespace ViciOne.ServiceBus.Diagnostics;

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.RabbitMqTransport;


/// <summary>
/// Publishes a large number of messages concurrently and waits until every one of them is consumed.
/// <para>
/// Completion is part of the measurement, not a timeout that ends it. A rate for messages that were
/// handed to the transport says how fast the client can enqueue; the number this scenario exists for
/// is the rate at which they came out the other end, and it cannot be reported before the last one
/// did. Publisher confirmation is off on purpose: the subject is the endpoint under a burst, and
/// confirmation would measure the broker's acknowledgement path instead.
/// </para>
/// </summary>
static class PublishLoadScenario
{
    public static async Task<object> Run(int messages, int concurrencyLimit, int prefetchCount,
        TimeSpan completionLimit, CancellationToken cancellationToken)
    {
        await RunScopedBroker.CreateVirtualHost("test", cancellationToken);

        (var host, var port, var username, var password) = RunScopedBroker.Read();

        var queue = $"diagnostics-publish-load-{NewId.Next().ToString("N")}";
        var completed = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var consumed = 0;

        var bus = Bus.Factory.CreateUsingRabbitMq(cfg =>
        {
            cfg.Host(host, (ushort)port, "test", h =>
            {
                h.Username(username);
                h.Password(password);
                h.PublisherConfirmation = false;
            });

            cfg.ReceiveEndpoint(queue, e =>
            {
                e.AutoDelete = true;
                e.Durable = false;
                e.PrefetchCount = prefetchCount;

                e.UseConcurrencyLimit(concurrencyLimit);

                e.Handler<LoadPing>(_ =>
                {
                    if (Interlocked.Increment(ref consumed) == messages)
                        completed.TrySetResult(consumed);

                    return Task.CompletedTask;
                });
            });
        });

        await bus.StartAsync(cancellationToken);
        try
        {
            var elapsed = Stopwatch.StartNew();

            var publishers = new Task[messages];
            for (var index = 0; index < messages; index++)
                publishers[index] = bus.Publish(new LoadPing(), cancellationToken);

            var handedOver = elapsed.Elapsed;

            await Task.WhenAll(publishers);

            var published = elapsed.Elapsed;

            await completed.Task.WaitAsync(completionLimit, cancellationToken);

            var finished = elapsed.Elapsed;

            return new
            {
                scenario = "publish-load",
                messages,
                concurrencyLimit,
                prefetchCount,
                queue,
                handedOverMilliseconds = (long)handedOver.TotalMilliseconds,
                publishedMilliseconds = (long)published.TotalMilliseconds,
                completedMilliseconds = (long)finished.TotalMilliseconds,
                publishedPerSecond = (long)(messages / published.TotalSeconds),
                completedPerSecond = (long)(messages / finished.TotalSeconds),
                consumed = Volatile.Read(ref consumed)
            };
        }
        finally
        {
            await bus.StopAsync(CancellationToken.None);
        }
    }


    public record LoadPing;
}
