// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Middleware.Rescue
{
    using System;
    using Context;
    using Events;
    using Serialization;


    public class RescueExceptionReceiveContext :
        ReceiveContextProxy,
        ExceptionReceiveContext
    {
        readonly DictionarySendHeaders _headers;
        ExceptionInfo _exceptionInfo;

        public RescueExceptionReceiveContext(ReceiveContext context, Exception exception)
            : base(context)
        {
            Exception = exception;
            ExceptionTimestamp = DateTime.UtcNow;

            _headers = new DictionarySendHeaders();

            _headers.SetExceptionHeaders(this);
        }

        public Exception Exception { get; }
        public DateTime ExceptionTimestamp { get; }

        public ExceptionInfo ExceptionInfo
        {
            get { return _exceptionInfo ??= new FaultExceptionInfo(Exception); }
        }

        public SendHeaders ExceptionHeaders => _headers;
    }
}
