
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CsvHelper;
using AzureRateCard.Models;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using CsvHelper.Configuration;

namespace UnitTests
{
    [TestClass]
    public class UnitTest1
    {

        [TestMethod]
        public void SplitMetersByCategory()
        {

            string[] Blacklist = new[]
            {
                "Low Priority",
                "Expired",
                "Promo",
                "Azure NetApp Files"
            };

            var regions = RawData.SelectRegions.ToList();

            var allItems = RawData.RateCard.Value
                .Meters
                .Where(x => x.EffectiveDate <= DateTime.UtcNow)
                .Where(x => x.MeterRegion.In(regions))
                .Where(x => !x.MeterName.ContainsAny(Blacklist))
                .Where(x => !x.MeterCategory.ContainsAny(Blacklist))
                .Where(x => !x.MeterSubCategory.ContainsAny(Blacklist))
                .SplitCombinedMeterNames()
                .ToList();

            allItems = allItems
                .OrderByDescending(x => x.EffectiveDate)
                .GroupBy(x => new
                {
                    x.MeterName,
                    x.MeterCategory,
                    x.MeterSubCategory,
                    x.MeterRegion,
                    x.Unit
                })
                .Select(x => x.First())
                .ToList();

            var groupedByCategory = allItems
                .GroupBy(x => x.MeterCategory)
                .ToList();

            var ratesTallCsvMap = ResourceRateTallCsvMap.Instance;
            var ratesWideCsvMap = new ResourceRateWideCsvMap(regions.ToArray());

            foreach (var group in groupedByCategory)
            {
                var name = group.Key;
                var meters = group.OrderBy(x => x.MeterName).ToList();
                var rates = meters.ToResourceRate().ToList();

                var fileNamePrefix = name.Replace(' ', '-'); //.ToLower();

                //var metersFilePath = Path.Combine(K.DataFolder, "meters", fileNamePrefix + ".meter.json");
                //meters.ToJson().PrettyJson().SaveAsText(metersFilePath);

                //var ratesFilePath = Path.Combine(K.DataFolder, "meters", fileNamePrefix + ".rates.json");
                //rates.ToJson().PrettyJson().SaveAsText(ratesFilePath);

                var ratesTallCsvPath = Path.Combine(K.DataFolder, "meters", fileNamePrefix + ".tall.csv");
                rates.SaveAsCsv(ratesTallCsvPath, ratesTallCsvMap);

                var ratesByRegion = rates
                    .GroupBy(x => new { x.SKU, x.Category, x.SubCategory })
                    .Select(g => new ResourceRateByRegion()
                    {
                        SKU = g.Key.SKU,
                        Category = g.Key.Category,
                        SubCategory = g.Key.SubCategory,
                        RateByRegion = g.ToDictionary( i => i.Region, i => i.PerMonth)
                    })
                    .ToList();

                var ratesWideCsvPath = Path.Combine(K.DataFolder, "meters", fileNamePrefix + ".wide.csv");
                ratesByRegion.SaveAsCsv(ratesWideCsvPath, ratesWideCsvMap);
            }
        }


    }




}

