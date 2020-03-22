
using System;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace AzureRatecard
{
    /// <summary>
    /// Can retry transient errors.
    /// </summary>
    class ArmTransientErrorHandler : DelegatingHandler
    {
        const int MaxAttempts = 3;
        static readonly Random RandomDelay = new Random(Guid.NewGuid().GetHashCode());

        internal ArmTransientErrorHandler(HttpMessageHandler innerHandler) : base(innerHandler)
        {
            //
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var attempt = 0;

            tryagain:
            try
            {
                attempt += 1;
                if (attempt > 1) Console.WriteLine($"ArmTransientErrorHandler: Take #{attempt}");

                return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception err)
            {
                if (attempt >= MaxAttempts) throw;
                else if (request.Method == HttpMethod.Post || request.Method == HttpMethod.Put) throw;
                else if (!IsTransientError(err)) throw;
                else await Task.Delay(RandomDelay.Next(100, 1000)).ConfigureAwait(false);
            }
            goto tryagain;
        }

        // Indicates if given exception, or any of the inner exception represents a transient error.
        static bool IsTransientError(Exception err)
        {
            while (null != err)
            {
                if (TransientErrorSignatures.Any(x => x.IsMatch(err.Message))) return true;
                err = err.InnerException;
            }

            return false;
        }

        // Well-known transient error signatures 
        static readonly Regex[] TransientErrorSignatures = new[]
        {
                "TestTransientError",
                "RequestTimeout",
                "GatewayTimeout",
                "System.Net.Sockets.SocketException",
                "server busy",
                "serverbusy",
                "connection was closed unexpectedly",
        }
        .Select(x => new Regex(x, RegexOptions.Compiled | RegexOptions.IgnoreCase))
        .ToArray();
    }
}
