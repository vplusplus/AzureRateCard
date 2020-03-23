using System;
using System.Collections.Generic;
using System.Text;

namespace AzureRateCard.Models
{
    public class AzMeter
    {
        public string MeterId { get; set; }
        public string MeterName { get; set; }
        public string MeterCategory { get; set; }
        public string MeterSubCategory { get; set; }
        public string MeterRegion { get; set; }
        public string Unit { get; set; }
        public Dictionary<string, double> MeterRates { get; set; }
        public DateTime EffectiveDate { get; set; }
        public List<string> MeterTags { get; set; }
        public double IncludedQuantity { get; set; }
    }
}
