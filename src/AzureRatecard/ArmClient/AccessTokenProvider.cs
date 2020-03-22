using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace AzureRatecard
{
    static class AccessTokenProvider
    {
        internal static async Task<string> GetAccessTokenAsync(string tenantId, string clientId, string userId, string password, string targetApiScope)
        {
            if (null == tenantId) throw new ArgumentNullException(nameof(tenantId));
            if (null == clientId) throw new ArgumentNullException(nameof(clientId));
            if (null == userId) throw new ArgumentNullException(nameof(userId));
            if (null == password) throw new ArgumentNullException(nameof(password));
            if (null == targetApiScope) throw new ArgumentNullException(nameof(targetApiScope));

            // REF: https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/wiki/Username-Password-Authentication

            var authority = $"https://login.microsoftonline.com/{tenantId}";

            try
            {
                var app = PublicClientApplicationBuilder
                    .Create(clientId)
                    .WithAuthority(authority)
                    .Build();

                string[] scopes = new[] { targetApiScope };

                var securePassword = new SecureString();
                foreach (var c in password) securePassword.AppendChar(c);

                var tokenResult = await app
                    .AcquireTokenByUsernamePassword(scopes, userId, securePassword)
                    .ExecuteAsync();

                return tokenResult.AccessToken;
            }
            catch (Exception err)
            {
                var msg = $"Error acquiring AccessToken. Target: {targetApiScope}";
                throw new Exception(msg, err);
            }
        }


    }
}
