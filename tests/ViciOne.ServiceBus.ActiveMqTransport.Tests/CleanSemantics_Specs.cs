namespace ViciOne.ServiceBus.ActiveMqTransport.Tests
{
    using System;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using ViciOne.ServiceBus.Testing;


    /// <summary>
    /// Binds the one behavioural change this work package made to a member that ships.
    /// <para>
    /// <see cref="ActiveMqTestHarness.Clean" /> began with <c>if (AdminPort != 8161) return;</c> in the
    /// imported baseline. The pinned fixture publishes an ephemeral loopback port per run, so that
    /// condition was true for every ViciOne run and the reset never happened: one fixture could leave
    /// entities behind for the next. Removing the condition is the fix, and it is also a visible change
    /// for any consumer who points the harness at a broker on a non-default admin port. A change that
    /// is only described in a comment is not bound to anything, so it is asserted here.
    /// </para>
    /// </summary>
    [TestFixture]
    public class Cleaning_the_broker
    {
        /// <summary>
        /// A port that is bound and immediately released, so nothing answers on it for this test.
        /// Asking the operating system for it avoids writing a literal endpoint into a spec, which is
        /// what the run-scoped fixture rules exist to prevent.
        /// </summary>
        static int ClosedLoopbackPort()
        {
            using var probe = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            probe.Bind(new IPEndPoint(IPAddress.Loopback, 0));
            return ((IPEndPoint)probe.LocalEndPoint).Port;
        }

        [Test]
        public void Should_not_disable_itself_on_a_non_default_admin_port()
        {
            var harness = new ActiveMqTestHarness { AdminPort = ClosedLoopbackPort() };

            // The old shape returned before touching anything, so this completed silently and the
            // caller could not tell a reset from a no-op. The reset now really runs, and because the
            // admin endpoint above answers nothing, it must surface that instead of reporting success.
            Assert.That(async () => await harness.Clean(), Throws.Exception,
                "Clean returned without doing anything, which is the condition this fixture exists to catch");
        }

        [Test]
        public void Should_carry_the_run_scoped_admin_port_by_default()
        {
            // The port comes from the environment the canonical runner publishes. Whatever it is, the
            // reset stays enabled for it: there is no value of AdminPort that turns Clean into a no-op.
            var harness = new ActiveMqTestHarness();

            Assert.That(harness.AdminPort, Is.EqualTo(RunScopedBroker.JolokiaPort));
        }
    }
}
