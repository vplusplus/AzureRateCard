
using System;
using System.Security;
using System.Threading.Tasks;
using Microsoft.Identity.Client;

namespace AzureRateCard
{
    static class AccessTokenProvider
    {
        /// <summary>
        /// Obtains access token using user credentials.
        /// Not the best option, till something better come-up for non-intractive clients. 
        /// </summary>
        internal static async Task<string> GetAccessTokenAsync(string tenantId, string clientId, string userId, string password, string targetApiScope)
        {
            if (null == tenantId) throw new ArgumentNullException(nameof(tenantId));
            if (null == clientId) throw new ArgumentNullException(nameof(clientId));
            if (null == userId) throw new ArgumentNullException(nameof(userId));
            if (null == password) throw new ArgumentNullException(nameof(password));
            if (null == targetApiScope) throw new ArgumentNullException(nameof(targetApiScope));

            // REF: https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/wiki/Username-Password-Authentication

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
    }
}
