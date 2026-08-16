namespace ViciOne.ServiceBus.TestFramework.Logging
{
    using System;
    using System.Diagnostics;
    using ViciOne.ServiceBus.Logging;


    public class DiagnosticListenerObserver :
        IObserver<DiagnosticListener>
    {
        IDisposable _handle;

        public void OnCompleted()
        {
        }

        public void OnError(Exception error)
        {
        }

        public void OnNext(DiagnosticListener value)
        {
            if (value.Name == LogCategoryName.ViciOneServiceBus)
            {
                //_handle?.Dispose();

                _handle = value.Subscribe(new TestOutputListenerObserver());
            }
        }
    }
}
