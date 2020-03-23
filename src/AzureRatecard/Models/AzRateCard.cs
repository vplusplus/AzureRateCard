using System;
using System.Collections.Generic;
using System.Text;

namespace AzureRateCard.Models
{
    public class AzRateCard
    {
        // public List<object> OfferTerms { get; set; }
        public List<AzMeter> Meters { get; set; }
        public string Currency { get; set; }
        public string Locale { get; set; }
        public bool IsTaxIncluded { get; set; }
    }
}
