using System;

namespace ViciOne.ServiceBus.AmazonSqs;

static class ClientContextBatchSettings
{
    const int MaxMessageLimit = 10;
    static readonly BatchSettings _defaults = new ClientBatchSettings(MaxMessageLimit, 10, 240 * 1024, TimeSpan.FromMilliseconds(1));

    public static BatchSettings GetBatchSettings()
    {
        return _defaults;
    }


    class ClientBatchSettings :
        BatchSettings
    {
        public ClientBatchSettings(int messageLimit, int batchLimit, int sizeLimit, TimeSpan timeout)
        {
            Enabled = true;
            MessageLimit = messageLimit;
            BatchLimit = batchLimit;
            SizeLimit = sizeLimit;
            Timeout = timeout;
        }

        public bool Enabled { get; }
        public int MessageLimit { get; }
        public int BatchLimit { get; }
        public int SizeLimit { get; }
        public TimeSpan Timeout { get; }
    }
}
