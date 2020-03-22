
using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace AzureRateCard
{
    /// <summary>
    /// Can translate ARM REST API error to local exception.
    /// </summary>
    internal sealed class ArmErrorHandler : DelegatingHandler
    {
        internal ArmErrorHandler(HttpMessageHandler innerHandler) : base(innerHandler)
        {
            //
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (null == request) throw new ArgumentNullException(nameof(request));

            // Process the request...
            var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);

            // In addition to default SuccessStatusCodes, NotFound and NotModified are ALSO treated acceptable.
            var good = response.IsSuccessStatusCode
                || response.StatusCode == HttpStatusCode.NotFound
                || response.StatusCode == HttpStatusCode.NotModified
                || response.StatusCode == HttpStatusCode.NoContent
                ;

            if (good)
            {
                // Console.WriteLine($"Content-type:{response.Content.Headers.ContentType}");

                //.............................................................
                // Uncomment for testing transient error handling...
                // Remember to dispose the response before throwing test error.
                //.............................................................
                // response.Dispose();
                // throw new Exception("TestTransientError");
                //.............................................................

                // Do not intercept.
                return response;
            }
            else
            {
                // Dispose the response, avoid resource leak.
                using (response)
                {
                    var statusCode = response.StatusCode;
                    var statusReason = response.ReasonPhrase;

                    ErrorResponse armError = null;
                    try
                    {
                        using (var content = response.Content)
                        {
                            var errJson = await content.ReadAsStringAsync();

                            if (!string.IsNullOrWhiteSpace(errJson))
                            {
                                armError = JsonSerializer.Deserialize<ErrorResponse>(
                                    errJson,
                                    new JsonSerializerOptions() { PropertyNameCaseInsensitive = true }
                                );
                            }
                        }
                    }
                    catch
                    {
                        armError = null;
                    }

                    var armErrMsg = armError?.Error?.Message;
                    var errMsg = null != armErrMsg
                         ? $"ArmClient-HttpError: {armErrMsg}"
                         : $"ArmClient-HttpError: [{response.StatusCode}] {response.ReasonPhrase}";

                    throw new Exception(errMsg);
                }
            }
        }

        class ErrorResponse
        {
            public ErrorCodeAndMessage Error { get; set; }
        }

        class ErrorCodeAndMessage
        {
            public string Code { get; set; }
            public string Message { get; set; }
        }
    }
}
