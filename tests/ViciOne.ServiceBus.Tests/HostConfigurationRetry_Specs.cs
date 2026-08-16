#nullable enable
namespace ViciOne.ServiceBus.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using ViciOne.ServiceBus.Configuration;
    using ViciOne.ServiceBus.Logging;
    using ViciOne.ServiceBus.Transports;


    /// <summary>
    /// Binds both branches of the retry loop's cancellation handling.
    /// <para>
    /// The loop waits out its backoff on a token linked from the caller token and the stopping token, so
    /// a cancellation raised there carries the linked token and equals neither source. Deciding from the
    /// exception's token therefore never reached the stopping branch at all, and a caller that cancelled
    /// its own operation received a TaskCanceledException bound to a token it had never passed. The
    /// decision now asks the sources.
    /// </para>
    /// <para>
    /// The caller branch is covered by PublishTimeout_Specs against a real broker. The stopping branch
    /// had no cover at all: it was unreachable before the correction, so nothing had ever executed it.
    /// These two specs execute it directly, without a broker, because the only two members the method
    /// touches are the host address and the send transport retry policy.
    /// </para>
    /// </summary>
    [TestFixture]
    public class Retrying_a_transport_operation
    {
        [Test]
        public void Should_report_a_stopping_transport_as_a_connection_failure()
        {
            var configuration = new StubHostConfiguration();

            using var stopping = new CancellationTokenSource();
            stopping.Cancel();

            Assert.That(async () => await configuration.Retry(FailOnce, CancellationToken.None, stopping.Token),
                Throws.TypeOf<ConnectionException>(),
                "a transport that is stopping must be reported as a connection failure, not as a bare cancellation");
        }

        [Test]
        public void Should_let_stopping_win_when_both_are_cancelled()
        {
            var configuration = new StubHostConfiguration();

            using var caller = new CancellationTokenSource();
            using var stopping = new CancellationTokenSource();
            caller.Cancel();
            stopping.Cancel();

            // Same precedence the method already applies in its pre-flight check: stopping is the
            // stronger statement, because the transport is gone either way.
            Assert.That(async () => await configuration.Retry(FailOnce, caller.Token, stopping.Token),
                Throws.TypeOf<ConnectionException>(),
                "with both tokens cancelled the transport shutdown must win over the caller cancellation");
        }

        [Test]
        public void Should_report_the_caller_token_when_only_the_caller_cancels()
        {
            var configuration = new StubHostConfiguration();

            using var caller = new CancellationTokenSource();
            caller.Cancel();

            var cancelled = Assert.ThrowsAsync<OperationCanceledException>(
                async () => await configuration.Retry(FailOnce, caller.Token, CancellationToken.None));

            Assert.That(cancelled.CancellationToken, Is.EqualTo(caller.Token),
                "the cancellation was reported against a token the caller never passed");
        }

        static Task FailOnce()
        {
            throw new InvalidOperationException("the transport operation failed");
        }


        /// <summary>
        /// The smallest thing the retry loop can run against: it reads the host address for its message
        /// and the send transport retry policy for its backoff. Everything else on the interface is
        /// unreachable from this method and says so rather than returning a quiet default.
        /// </summary>
        class StubHostConfiguration :
            IHostConfiguration
        {
            public Uri HostAddress { get; } = new Uri("loopback://localhost/");

            public IRetryPolicy SendTransportRetryPolicy { get; } = Retry.None;

            public IBusConfiguration BusConfiguration => throw new NotSupportedException();
            public bool DeployTopologyOnly { get; set; }
            public bool DeployPublishTopology { get; set; }
            public ISendObserver SendObservers => throw new NotSupportedException();
            public ILogContext? LogContext { get; set; }
            public ILogContext? ReceiveLogContext => throw new NotSupportedException();
            public ILogContext? SendLogContext => throw new NotSupportedException();
            public IBusTopology Topology => throw new NotSupportedException();
            public IRetryPolicy ReceiveTransportRetryPolicy => throw new NotSupportedException();
            public TimeSpan? ConsumerStopTimeout { get; set; }
            public TimeSpan? StopTimeout { get; set; }

            public IReceiveEndpointConfiguration CreateReceiveEndpointConfiguration(string queueName,
                Action<IReceiveEndpointConfigurator>? configure = null)
            {
                throw new NotSupportedException();
            }

            public ConnectHandle ConnectReceiveEndpointContext(ReceiveEndpointContext context)
            {
                throw new NotSupportedException();
            }

            public IHost Build()
            {
                throw new NotSupportedException();
            }

            public ConnectHandle ConnectEndpointConfigurationObserver(IEndpointConfigurationObserver observer)
            {
                throw new NotSupportedException();
            }

            public ConnectHandle ConnectReceiveObserver(IReceiveObserver observer)
            {
                throw new NotSupportedException();
            }

            public ConnectHandle ConnectConsumeObserver(IConsumeObserver observer)
            {
                throw new NotSupportedException();
            }

            public ConnectHandle ConnectConsumeMessageObserver<T>(IConsumeMessageObserver<T> observer)
                where T : class
            {
                throw new NotSupportedException();
            }

            public ConnectHandle ConnectPublishObserver(IPublishObserver observer)
            {
                throw new NotSupportedException();
            }

            public ConnectHandle ConnectSendObserver(ISendObserver observer)
            {
                throw new NotSupportedException();
            }

            public IEnumerable<ValidationResult> Validate()
            {
                throw new NotSupportedException();
            }
        }
    }
}
