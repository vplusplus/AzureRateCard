
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace AzureRateCard
{
    /// <summary>
    /// Just enough HttpClient for Azure Resource Manager REST APIs.
    /// </summary>
    public sealed class ArmClient : HttpClient
    {
        public const string AzureResourceManagerBaseUri = "https://management.azure.com/";
        public static readonly string AzureResourceManagerScope = $"{AzureResourceManagerBaseUri}/.default";

        //.........................................................................................
        #region ArmClient.Connect()
        //.........................................................................................
        private ArmClient(HttpMessageHandler messageHandler) : base(messageHandler ?? throw new ArgumentNullException(nameof(messageHandler)))
        {
            //
        }

        /// <summary>
        /// HttpClient with SocketsHttpHandler, ArmTransientErrorHandler and ArmErrorHandler.
        /// With ClientId/ClientCertificate based authorization.
        /// Accepts json with optional GZip.
        /// </summary>
        public static ArmClient Connect()
        {
            var accessToken = AccessTokenProvider
                .FromClientIdAndClientCertificate(AzureResourceManagerScope)
                .ConfigureAwait(false)
                .GetAwaiter()
                .GetResult();

            var authHeader = new AuthenticationHeaderValue("Bearer", accessToken);

            HttpMessageHandler handler = new SocketsHttpHandler()
            {
                AutomaticDecompression = DecompressionMethods.GZip,
            };
            handler = new ArmTransientErrorHandler(handler);
            handler = new ArmErrorHandler(handler);

            var client = new ArmClient(handler);
            client.BaseAddress = new Uri(AzureResourceManagerBaseUri);
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Authorization = authHeader;
            return client;
        }

        #endregion

        //.........................................................................................
        #region GetAsAsync<T>() and GetManyAsync<T>()
        //.........................................................................................

        static readonly JsonSerializerOptions DefaultJsonSerializerOptions = new JsonSerializerOptions()
        {
            IgnoreReadOnlyProperties = false,
            PropertyNameCaseInsensitive = true,
            AllowTrailingCommas = true,
            WriteIndented = true,
        };

        class Many<T>
        {
            [JsonPropertyName("value")]
            public List<T> Items { get; set; }
        }

        public async Task<T> GetAsAsync<T>(string resourcePath, JsonSerializerOptions options = null)
        {
            if (null == resourcePath) throw new ArgumentNullException(nameof(resourcePath));

            using (var response = await this.GetAsync(resourcePath).ConfigureAwait(false))
            {
                var statusCode = response.StatusCode;
                if (statusCode == HttpStatusCode.NotFound || statusCode == HttpStatusCode.NoContent) return default(T);

                using(var content = response.EnsureSuccessStatusCode().Content)
                using(var contentStream = await content.ReadAsStreamAsync().ConfigureAwait(false))
                {
                    return await JsonSerializer.DeserializeAsync<T>(contentStream, options ?? DefaultJsonSerializerOptions).ConfigureAwait(false);
                }
            }
        }

        public async Task<IList<T>> GetManyAsync<T>(string resourcePath)
        {
            if (null == resourcePath) throw new ArgumentNullException(nameof(resourcePath));

            var items = await this.GetAsAsync<Many<T>>(resourcePath).ConfigureAwait(false);
            return items?.Items;
        }

        #endregion

    }
}
