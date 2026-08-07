// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus
{
    using System;
    using System.Collections;


    [Serializable]
    public class ExceptionInfoException :
        ViciOneServiceBusException
    {
        readonly IDictionary? _data;

        public ExceptionInfoException(ExceptionInfo exceptionInfo)
            : base(exceptionInfo.Message, exceptionInfo.InnerException != null ? new ExceptionInfoException(exceptionInfo.InnerException) : default)
        {
            ExceptionInfo = exceptionInfo;
            if (ExceptionInfo.Data != null)
                _data = (IDictionary)ExceptionInfo.Data;
        }

        public ExceptionInfo ExceptionInfo { get; }

        public override string StackTrace => ExceptionInfo.StackTrace;
        public override string Source => ExceptionInfo.Source;

        public override IDictionary Data => _data ?? base.Data;
    }
}
