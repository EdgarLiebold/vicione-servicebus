using System.Net;
using Amazon.Runtime;

namespace ViciOne.ServiceBus.AmazonSqs;

/// <summary>
/// Provides extension methods for amazon web service response.
/// </summary>
public static class AmazonWebServiceResponseExtensions
{
    /// <summary>
    /// Performs the ensure successful response operation.
    /// </summary>
    /// <param name="response">The response value.</param>
    public static void EnsureSuccessfulResponse(this AmazonWebServiceResponse response)
    {
        const string documentationUri = "https://aws.amazon.com/blogs/developer/logging-with-the-aws-sdk-for-net/";

        var statusCode = response.HttpStatusCode;

        if (statusCode >= HttpStatusCode.OK && statusCode < HttpStatusCode.MultipleChoices)
            return;

        var requestId = response.ResponseMetadata?.RequestId ?? "[Missing RequestId]";

        throw new AmazonSqsTransportException(
            $"Received unsuccessful response ({statusCode}) from AWS endpoint. See AWS SDK logs ({requestId}) for more details: {documentationUri}");
    }
}
