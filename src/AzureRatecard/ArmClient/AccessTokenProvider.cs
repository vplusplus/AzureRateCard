
using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.Identity.Client;

namespace AzureRateCard
{
    public class AccessTokenProvider
    {
        /// <summary>
        /// Obtains access token using clientid and client certificate.
        /// Ref: https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/wiki/Client-credential-flows
        /// </summary>
        public static async Task<string> FromClientIdAndClientCertificate(string targetApiScope)
        {
            if (null == targetApiScope) throw new ArgumentNullException(nameof(targetApiScope));

            // THIS implementation is NOT intended as general-purpose lib.
            // THIS library is secific to RateCard client.
            var tenantId = MyConfig.RateCardTenantId;
            var clientId = MyConfig.RateCardClientId;
            var clientCertThumbprint = MyConfig.RateCardClientCertThumbprint;

            try
            {
                var authority = $"https://login.microsoftonline.com/{MyConfig.RateCardTenantId}";
                X509Certificate2 clientCertificate = ReadCertificate(clientCertThumbprint);

                // Confidential client with certificate autnetication.
                IConfidentialClientApplication app = ConfidentialClientApplicationBuilder
                    .Create(MyConfig.RateCardClientId)
                    .WithAuthority(authority)
                    .WithCertificate(clientCertificate)
                    .Build();

                // Our use-case has only one scope...
                string[] scopes = new[] { targetApiScope };

                // Obtain access token.
                var tokenResult = await app
                    .AcquireTokenForClient(scopes)
                    .ExecuteAsync();

                return tokenResult.AccessToken;
            }
            catch (Exception err)
            {
                var msg = $"Error acquiring AccessToken. Target: {targetApiScope}";
                throw new Exception(msg, err);
            }

            X509Certificate2 ReadCertificate(string thumbprint)
            {
                using (X509Store store = new X509Store(StoreName.My, StoreLocation.CurrentUser))
                {
                    store.Open(OpenFlags.ReadOnly);

                    var certs = store.Certificates.Find(X509FindType.FindByThumbprint, thumbprint, false);

                    return
                        0 == certs.Count ? throw new Exception($"Certificate NOT found for given thumbprint: {thumbprint}") :
                        1 == certs.Count ? certs[0] :
                        throw new Exception($"Though impossible... Found more than ONE certs matching thumbprint: {thumbprint}");
                }
            }
        }
    
    }
}

/*

        /// <summary>
        /// Obtains access token using user credentials.
        /// Not the best option, till something better come-up for non-intractive clients. 
        /// REF: https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/wiki/Username-Password-Authentication
        /// </summary>
        public static async Task<string> FromUserIdAndPassword(string targetApiScope)
        {
            if (null == targetApiScope) throw new ArgumentNullException(nameof(targetApiScope));

            var tenantId = MyConfig.RateCardTenantId;
            var clientId = MyConfig.RateCardClientId;
            var userId   = MyConfig.RateCardUserId;
            var password = MyConfig.RateCardPassword;

            try
            {
                var authority = $"https://login.microsoftonline.com/{tenantId}";

                // App is not using singleton, given the internal consumer takes access token only once.
                var app = PublicClientApplicationBuilder
                    .Create(clientId)
                    .WithAuthority(authority)
                    .Build();

                string[] scopes = new[] { targetApiScope };

                var tokenResult = await app
                    .AcquireTokenByUsernamePassword(scopes, userId, ToSecureString(password))
                    .ExecuteAsync();

                return tokenResult.AccessToken;
            }
            catch (Exception err)
            {
                var msg = $"Error acquiring AccessToken. Target: {targetApiScope}";
                throw new Exception(msg, err);
            }

            static SecureString ToSecureString(string sometext)
            {
                if (null == sometext) throw new ArgumentNullException(nameof(sometext));

                var secureString = new SecureString();
                foreach (var c in sometext) secureString.AppendChar(c);

                return secureString;
            }
        }

        /// <summary>
        /// Obtains access token using clientid and client secret.
        /// Ref: https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/wiki/Client-credential-flows
        /// </summary>
        public static async Task<string> FromClientIdAndClientSecret(string targetApiScope)
        {
            if (null == targetApiScope) throw new ArgumentNullException(nameof(targetApiScope));

            var tenantId = MyConfig.RateCardTenantId;
            var clientId = MyConfig.RateCardClientId;
            var clientSecret = MyConfig.RateCardClientSecret;

            try
            {
                var authority = $"https://login.microsoftonline.com/{MyConfig.RateCardTenantId}";

                IConfidentialClientApplication app = ConfidentialClientApplicationBuilder
                    .Create(MyConfig.RateCardClientId)
                    .WithAuthority(authority)
                    .WithClientSecret(clientSecret)
                    .Build();

                string[] scopes = new[] { targetApiScope };

                // NOTE: Check the token cache first. Refer online sample.
                var tokenResult = await app
                    .AcquireTokenForClient(scopes)
                    .ExecuteAsync();

                return tokenResult.AccessToken;
            }
            catch (Exception err)
            {
                var msg = $"Error acquiring AccessToken. Target: {targetApiScope}";
                throw new Exception(msg, err);
            }
        }



*/
