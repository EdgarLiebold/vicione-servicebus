namespace ViciOne.ServiceBus
{
    using System;
    using RabbitMQ.Client.Exceptions;
    using RabbitMqTransport;


    [Serializable]
    public class RabbitMqConnectionException :
        ConnectionException
    {
        public RabbitMqConnectionException()
        {
        }

        public RabbitMqConnectionException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// The transport's own failures, where this assembly decides whether waiting can resolve them.
        /// <para>
        /// Not public. The public string constructor above has said "not transient" since the freeze,
        /// and external callers must keep that answer: a signature gate cannot see a changed meaning, so
        /// a correction that is right for one internal site must not arrive at everyone else's as a
        /// silent behaviour change.
        /// </para>
        /// </summary>
        internal RabbitMqConnectionException(string message, bool isTransient)
            : base(message, isTransient)
        {
        }

        /// <summary>
        /// The connection is shutting down and cannot serve this caller.
        /// <para>
        /// Transient, because a stop is exactly the failure a later start resolves. A caller asking
        /// "can waiting fix this?" has to be told yes here; announced as permanent, a routine shutdown
        /// carries the same answer as a refused credential and every run after a stop fails specs that
        /// have nothing to do with it.
        /// </para>
        /// </summary>
        internal static RabbitMqConnectionException Stopping(string description)
        {
            return new RabbitMqConnectionException($"The connection is stopping and cannot be used: {description}", true);
        }

        public RabbitMqConnectionException(string message, Exception innerException)
            : base(message, innerException, IsExceptionTransient(innerException))
        {
        }

        /// <summary>
        /// Whether the failure is worth waiting out.
        /// <para>
        /// Transient is the default: an unreachable broker, a dropped connection and every other fault
        /// the transport recovers from. Two answers are not transient, because repeating them cannot
        /// change them — a refused credential, and a queue the broker will not hand over exclusively.
        /// Counting the second one as transient hides the endpoint start in a background retry loop and
        /// leaves the caller with the generic readiness timeout after sixty seconds instead of the
        /// broker's answer.
        /// </para>
        /// </summary>
        static bool IsExceptionTransient(Exception exception)
        {
            if (exception.IsExclusiveResourceConflict())
                return false;

            return exception switch
            {
                BrokerUnreachableException bue => bue.InnerException switch
                {
                    AuthenticationFailureException _ => false,
                    _ => true
                },
                _ => true
            };
        }
    }
}
