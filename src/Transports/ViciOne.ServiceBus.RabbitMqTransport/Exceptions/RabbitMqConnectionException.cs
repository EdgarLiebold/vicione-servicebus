namespace ViciOne.ServiceBus
{
    using System;
    using System.Runtime.Serialization;
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
        /// Transient, and that is the correction. A stop is exactly the failure a later start resolves,
        /// but it used to be announced with the public string constructor, whose flag is false — so a
        /// routine shutdown carried the same answer as a refused credential, and every caller asking
        /// "can waiting fix this?" was told no about a bus that was merely stopping. Measured, that made
        /// each run after a stop fail specs that had nothing to do with it.
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

        [Obsolete("Formatter-based serialization is obsolete and should not be used.")]
        protected RabbitMqConnectionException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        /// <summary>
        /// Whether the failure is worth waiting out.
        /// <para>
        /// Transient is the default, and stays the default: an unreachable broker, a dropped connection
        /// and every other fault the transport already recovers from are unchanged. Two answers are not
        /// transient, because repeating them cannot change them — a refused credential, and a queue the
        /// broker will not hand over exclusively. The second one is new. It used to be counted as
        /// transient, so the endpoint start disappeared into a background retry loop and the caller was
        /// left with the generic readiness timeout after sixty seconds instead of the broker's answer.
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
