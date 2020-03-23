using System;
using System.Collections.Generic;
using System.Text;

namespace AzureRateCard.Models
{
    public class AzSubscription
    {
        public string Id { get; set; }
        public string SubscriptionId { get; set; }
        public string TenantId { get; set; }
        public string DisplayName { get; set; }
        public string State { get; set; }
    }
}
