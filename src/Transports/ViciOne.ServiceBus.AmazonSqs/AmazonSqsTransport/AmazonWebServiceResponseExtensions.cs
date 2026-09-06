using System.Net;
using Amazon.Runtime;

namespace ViciOne.ServiceBus.AmazonSqs;

/// <summary>Provides response validation for Amazon Web Services SDK operations.</summary>
public static class AmazonWebServiceResponseExtensions
{
    /// <summary>Verifies that an AWS response has a successful HTTP status code.</summary>
    /// <param name="response">The AWS response to validate.</param>
    /// <exception cref="AmazonSqsTransportException">The response status code is outside the successful HTTP range.</exception>
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
