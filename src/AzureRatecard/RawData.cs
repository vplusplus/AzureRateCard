
using System;
using System.Linq;
using System.Threading.Tasks;
using AzureRateCard.Models;

namespace AzureRateCard
{
    /// <summary>
    /// Collects RAW data (json) of the RateCards and SKUs
    /// </summary>
    public static class RawData
    {
        /// <summary>
        /// Returns the SubscriptionId of first available subscription.
        /// If the caller has access to multiple subscriptions, the order is not guarneteed.
        /// Throws an exception if the caller has no subscriptions.
        /// </summary>
        public static async Task<string> GetFirstSubscriptionId(this ArmClient armClient)
        {
            if (null == armClient) throw new ArgumentNullException(nameof(armClient));

            var path = "/subscriptions?api-version=2020-01-01";
            var subs = await armClient.GetManyAsync<Subscription>(path);
            var sub = subs?.FirstOrDefault();
            if (null == sub) throw new Exception("Given credential do not have access to any Subscription. Please assign minimally READER access to atleast ONE subscription");

            return sub.SubscriptionId;
        }

        /// <summary>
        /// Returns RAW Json result, this is by design.
        /// Design intent is to cache the un-parsed json as is.
        /// See: https://docs.microsoft.com/en-us/previous-versions/azure/reference/mt219004(v=azure.100) 
        /// See: https://azure.microsoft.com/en-us/support/legal/offer-details/
        /// Also: https://docs.microsoft.com/en-us/partner-center/develop/azure-rate-card-resources
        /// Also: https://docs.microsoft.com/en-us/partner-center/develop/get-prices-for-microsoft-azure
        /// </summary>
        public static async Task<string> GetRateCardJsonAsync(this ArmClient armClient, string subscriptionId)
        {
            if (null == armClient) throw new ArgumentNullException(nameof(armClient));
            if (null == subscriptionId) throw new ArgumentNullException(nameof(subscriptionId));

            const string OfferId = "MS-AZR-0003p";      // Pay-As-You-Go 
            const string Currency = "USD";              
            const string Locale = "en-US";
            const string Region = "US";

            var path = $"/subscriptions/{subscriptionId}/providers/Microsoft.Commerce/RateCard?api-version=2016-08-31-preview&$filter=OfferDurableId eq '{OfferId}' and Currency eq '{Currency}' and Locale eq '{Locale}' and RegionInfo eq '{Region}'";

            return await armClient
                .GetStringAsync(path)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Returns RAW json of Microsoft.Compute/skus
        /// Design intent is to cache and re-use the un-parsed json as is.
        /// REF: https://docs.microsoft.com/en-us/rest/api/compute/resourceskus/list
        /// </summary>
        public static async Task<string> GetComputeResourcesSkus(this ArmClient armClient, string subscriptionId)
        {
            if (null == armClient) throw new ArgumentNullException(nameof(armClient));
            if (null == subscriptionId) throw new ArgumentNullException(nameof(subscriptionId));

            var path = $"/subscriptions/{subscriptionId}/providers/Microsoft.Compute/skus?api-version=2019-04-01";
            return await armClient.GetStringAsync(path);
        }

    }
}
