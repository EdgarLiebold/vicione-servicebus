namespace ViciOne.ServiceBus.Transports;

internal interface ITransportSendMetadata
{
    object CaptureNativeMetadata();

    string? ChangedNativeField(object snapshot);

    void RestoreNativeMetadata(object snapshot);
}
