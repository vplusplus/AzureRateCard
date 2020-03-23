using AzureRateCard.Models;
using CsvHelper.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UnitTests
{


    class ResourceRateTallCsvMap : ClassMap<ResourceRate>
    {
        public static readonly ClassMap Instance = new ResourceRateTallCsvMap();

        private ResourceRateTallCsvMap()
        {
            int index = 0;

            Map(m => m.SKU).Index(++index);
            Map(m => m.Region).Index(++index);
            Map(m => m.PerMonth).Index(++index);
            //Map(m => m.Category).Index(++index);
            Map(m => m.SubCategory).Index(++index);
            Map(m => m.EffectiveDate).Index(++index);
        }
    }

    class ResourceRateWideCsvMap : ClassMap<ResourceRateByRegion>
    {
        public ResourceRateWideCsvMap(string[] regions)
        {
            int index = 0;

            Map(m => m.SKU).Index(++index);
            Map(m => m.SubCategory).Index(++index);

            foreach(var region in regions)
            {
                Map()
                    .Index(++index)
                    .Name(region)
                    .ConvertUsing(x => 
                    {
                        var rates = (x as ResourceRateByRegion).RateByRegion;
                        return rates.ContainsKey(region) ? rates[region].ToString() : "";
                    });
            }

        }
    }

    public static class VirtualMachinesMetersMap
    {


        public static IEnumerable<AzMeter> SplitCombinedMeterNames(this IEnumerable<AzMeter> items)
        {
            foreach(var item in items)
            {
                var shouldSplit = item.MeterName.Contains('/');
                if (shouldSplit)
                {
                    var skuParts = item.MeterName.Split('/').Select(x => x.Trim()).Distinct();

                    foreach(var sku in skuParts)
                    {
                        if (!string.IsNullOrWhiteSpace(sku))
                        {
                            yield return new AzMeter()
                            {
                                MeterId = item.MeterId,

                                MeterName = sku,
                                MeterCategory = item.MeterCategory,
                                MeterSubCategory = item.MeterSubCategory,

                                EffectiveDate = item.EffectiveDate,
                                IncludedQuantity = item.IncludedQuantity,
                                MeterRates = item.MeterRates,
                                MeterRegion = item.MeterRegion,
                                MeterTags = item.MeterTags,
                                Unit = item.Unit    
                            };
                        }
                    }
                }
                else {
                    yield return item;
                }
            }
        }



    }
}
