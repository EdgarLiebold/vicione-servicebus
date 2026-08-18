namespace ViciOne.ServiceBus.RabbitMqTransport.Tests
{
    using System;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using RabbitMQ.Client;
    using RabbitMQ.Client.Exceptions;


    /// <summary>
    /// What RabbitMQ itself does when a second connection asks for a queue another one holds
    /// exclusively — asserted against the pinned broker with no ViciOne code in the way.
    /// <para>
    /// This exists because every other proof in this area rests on a protocol precondition that was
    /// only ever assumed: that the refusal is reply code 405, that it closes the contender's channel
    /// and not its connection, and that the contender can carry on with a fresh channel. The transport's
    /// ownership model, its classification of 405 as permanent and its retry policy all depend on those
    /// three facts. If the broker ever stops behaving this way, this spec fails first and names the
    /// reason, instead of an integration spec failing later for a reason nobody can place.
    /// </para>
    /// <para>
    /// Two direct RabbitMQ.Client connections, no bus, no harness, no management API.
    /// </para>
    /// </summary>
    [TestFixture]
    [Category("Integration")]
    public class The_broker_contract_for_an_exclusive_queue
    {
        [Test]
        public async Task Should_refuse_the_contender_with_405_and_leave_its_connection_usable()
        {
            var queueName = $"exclusive-contract-{NewToken()}";

            var factory = new ConnectionFactory
            {
                HostName = RunScopedCredentials.Host,
                Port = RunScopedCredentials.Port,
                UserName = RunScopedCredentials.User,
                Password = RunScopedCredentials.Pass,
                VirtualHost = "test"
            };

            await using var holderConnection = await factory.CreateConnectionAsync("exclusive-contract-holder");
            await using var contenderConnection = await factory.CreateConnectionAsync("exclusive-contract-contender");

            await using var holder = await holderConnection.CreateChannelAsync();
            await holder.QueueDeclareAsync(queueName, false, true, true, null);

            var contender = await contenderConnection.CreateChannelAsync();

            OperationInterruptedException refusal;
            try
            {
                refusal = Assert.CatchAsync<OperationInterruptedException>(
                    async () => await contender.QueueDeclareAsync(queueName, false, true, true, null));

                Assert.Multiple(() =>
                {
                    Assert.That(refusal.ShutdownReason.ReplyCode, Is.EqualTo(405),
                        "the broker no longer answers an exclusivity conflict with RESOURCE_LOCKED, so classifying "
                        + "405 as the permanent conflict no longer describes this broker");
                    Assert.That(refusal.ShutdownReason.Initiator, Is.EqualTo(ShutdownInitiator.Peer),
                        "the refusal did not come from the broker");
                    Assert.That(contender.IsClosed, Is.True,
                        "the refusal no longer closes the contender's channel, so the transport's ownership model "
                        + "is protecting against something that no longer happens");
                    Assert.That(contenderConnection.IsOpen, Is.True,
                        "the refusal took the whole connection down, not just the channel — the transport recovers "
                        + "by opening a new channel on the same connection and that would no longer work");
                });
            }
            finally
            {
                await contender.DisposeAsync();
            }

            // The fourth fact, and the one the retry policy depends on: after the refusal the contender's
            // connection is not merely reported open, it is usable.
            await using var second = await contenderConnection.CreateChannelAsync();

            var ownQueue = $"{queueName}-after";
            var declared = await second.QueueDeclareAsync(ownQueue, false, true, true, null);

            Assert.That(declared.QueueName, Is.EqualTo(ownQueue),
                "a new channel on the contender's connection could not be used after the conflict");
        }

        /// <summary>
        /// The same conflict once the holder is gone: the queue is auto-delete and exclusive, so it
        /// leaves with its connection and the contender's next attempt succeeds. This is what makes the
        /// conflict a matter of ownership rather than of the queue's name, and it is why the transport
        /// must not remember a refusal beyond the attempt that received it.
        /// </summary>
        [Test]
        public async Task Should_hand_the_queue_over_once_the_holder_is_gone()
        {
            var queueName = $"exclusive-contract-{NewToken()}";

            var factory = new ConnectionFactory
            {
                HostName = RunScopedCredentials.Host,
                Port = RunScopedCredentials.Port,
                UserName = RunScopedCredentials.User,
                Password = RunScopedCredentials.Pass,
                VirtualHost = "test"
            };

            var holderConnection = await factory.CreateConnectionAsync("exclusive-contract-holder-2");
            await using var contenderConnection = await factory.CreateConnectionAsync("exclusive-contract-contender-2");

            var holder = await holderConnection.CreateChannelAsync();
            await holder.QueueDeclareAsync(queueName, false, true, true, null);

            await using (var contender = await contenderConnection.CreateChannelAsync())
            {
                Assert.CatchAsync<OperationInterruptedException>(
                    async () => await contender.QueueDeclareAsync(queueName, false, true, true, null));
            }

            await holder.DisposeAsync();
            await holderConnection.DisposeAsync();

            // The queue's removal follows its connection's close, so this is retried until the broker has
            // caught up. Each attempt gets its own channel, because a refusal closes the one it was made
            // on — which is exactly the recovery the transport performs, and the reason a retry on the
            // same channel could never succeed no matter how long it waited.
            var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(20);
            while (true)
            {
                await using var successor = await contenderConnection.CreateChannelAsync();

                try
                {
                    var declared = await successor.QueueDeclareAsync(queueName, false, true, true, null);

                    Assert.That(declared.QueueName, Is.EqualTo(queueName));
                    return;
                }
                catch (OperationInterruptedException) when (DateTime.UtcNow < deadline)
                {
                    await Task.Delay(100);
                }
            }
        }

        static string NewToken()
        {
            return Guid.NewGuid().ToString("N").Substring(0, 8);
        }
    }
}
