using System;

namespace ViciOne.ServiceBus.AmazonSqs;

static class PublishBatchSettings
{
    const int MaxMessageLimit = 10;
    static readonly BatchSettings _defaults = new SnsPublishBatchSettings(MaxMessageLimit, 10, 240 * 1024, TimeSpan.FromMilliseconds(1));

    public static BatchSettings GetBatchSettings()
    {
        return _defaults;
    }


    class SnsPublishBatchSettings :
        BatchSettings
    {
        public SnsPublishBatchSettings(int messageLimit, int batchLimit, int sizeLimit, TimeSpan timeout)
        {
            MessageLimit = messageLimit;
            BatchLimit = batchLimit;
            SizeLimit = sizeLimit;
            Timeout = timeout;
        }

        public int MessageLimit { get; }
        public int BatchLimit { get; }
        public int SizeLimit { get; }
        public TimeSpan Timeout { get; }
    }
}
