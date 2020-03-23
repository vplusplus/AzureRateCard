using AzureRateCard.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UnitTests
{
    public class ResourceRate
    {
        public string SKU { get; set; }
        public string Region { get; set; }
        public double PerMonth { get; set; }
        public string Category { get; set; }
        public string SubCategory { get; set; }
        public DateTime EffectiveDate { get; set; }
    }

    public class ResourceRateByRegion
    {
        public string SKU { get; set; }
        public string Category { get; set; }
        public string SubCategory { get; set; }
        public Dictionary<string, double> RateByRegion { get; set; }
    }

    public static partial class MapAndReduceExtensions
    {
        //..................................................................................
        #region ToResouceRate() - Translate Meter to ResoureRate
        //..................................................................................
        public static ResourceRate ToResourceRate(this AzMeter meter)
        {
            var perHour = (meter.MeterRates?.FirstOrDefault().Value).GetValueOrDefault(0.0);
            var perMonth = perHour * K.HoursPerMonth;

            return new ResourceRate()
            {
                SKU = meter.MeterName,
                Category = meter.MeterCategory,
                SubCategory = meter.MeterSubCategory,
                Region = meter.MeterRegion,
                PerMonth = perMonth,
                EffectiveDate = meter.EffectiveDate
            };
        }

        public static IEnumerable<ResourceRate> ToResourceRate(this IEnumerable<AzMeter> meters)
        {
            return meters.Select(ToResourceRate);
        }

        #endregion




    }

}
