
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
        public void FindDuplicates()
        {
            string[] Blacklist = new[]
            {
                "Low Priority",
                "Expired",
                "Promo",
                "Azure NetApp Files"
            };

            var regions = RawData.SelectRegions.ToList();

            var groupedByCategory = RawData.RateCard.Value
                .Meters
                .SplitCombinedMeterNames()
                .Where(x => x.MeterRegion.In(regions))
                .Where(x => !x.MeterName.ContainsAny(Blacklist))
                .Where(x => !x.MeterCategory.ContainsAny(Blacklist))
                .Where(x => !x.MeterSubCategory.ContainsAny(Blacklist))
                .GroupBy(x => x.MeterCategory)
                .ToList();

            var ratesTallCsvMap = ResourceRateTallCsvMap.Instance;
            var ratesWideCsvMap = new ResourceRateWideCsvMap(regions.ToArray());

            foreach (var group in groupedByCategory)
            {
                var name = group.Key;
                var meters = group.OrderBy(x => x.MeterName).ToList();
                var fileNamePrefix = name.Replace(' ', '-'); //.ToLower();

                var duplicates = meters.GroupBy(x => new
                {
                    x.MeterName,
                    x.MeterCategory,
                    x.MeterSubCategory,
                    x.MeterRegion
                })
                .Where(g => g.Count() > 1)
                .SelectMany(g => g)
                .ToList();

                if (duplicates.Count > 0)
                {
                    var ratesTallCsvPath = Path.Combine(K.DataFolder, "meters", fileNamePrefix + ".duplicates.csv");
                    duplicates.SaveAsCsv(ratesTallCsvPath);
                }
            }
        }


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

/*

        [TestMethod]
        public void MeasureReadCategories()
        {
            var rc = RawData.RateCard.Value;

            var timer = Stopwatch.StartNew();
            int loopCount = 100;
            for(int i=0; i<loopCount; i++)
            {
                rc.Meters
                    .Select(x => new { x.MeterCategory, x.MeterSubCategory })
                    .Distinct()
                    .OrderBy(x => x.MeterCategory)
                    .ThenBy(x => x.MeterSubCategory)
                    .ToList();
            }
            timer.Stop();

            var elapsed = timer.Elapsed;
            Console.WriteLine($"Elapsed: {elapsed.TotalMilliseconds:#,0} mSec");
            Console.WriteLine($"Average: {elapsed.TotalMilliseconds/loopCount:#,0} mSec");
        }



        [TestMethod]
        public void SizeTest()
        {
            var keep = RawData.SelectRegions.ToList();

            var kb = RawData.RateCard.Value.ToJson().Length / 1024;
            Console.WriteLine($"Full rate card: {kb:#,0} KB");

            kb = RawData.RateCard.Value
                .Meters
                .ToJson()
                .Length / 1024;
            Console.WriteLine($"RateCard.Meters: {kb:#,0} KB");

            kb = RawData.RateCard.Value
                .Meters
                .Where(x => x.MeterRegion.In(keep))
                .ToJson()
                .Length / 1024;
            Console.WriteLine($"Select Regions only: {kb:#,0} KB");

            kb = RawData.RateCard.Value
                .Meters
                .Where(x => !x.MeterRegion.In(keep))
                .ToJson()
                .Length / 1024;
            Console.WriteLine($"Dropped: {kb:#,0} KB");

        }

        [TestMethod]
        public void MeasureFilterSelectAndSortFromRawData()
        {
            var rc = RawData.RateCard.Value;
            var keep = RawData.SelectRegions.ToList();
            var meters = rc.Meters.Where(x => x.MeterRegion.In(keep)).ToList();

            var timer = Stopwatch.StartNew();
            int loopCount = 10;
            for (int i = 0; i < loopCount; i++)
            {
                var kb = meters
                    .OrderBy(x => x.MeterCategory)
                    .ToJson()
                    .Length / 1024;

                //Console.WriteLine(kb);
            }
            timer.Stop();

            var elapsed = timer.Elapsed;
            Console.WriteLine($"Elapsed: {elapsed.TotalMilliseconds:#,0} mSec");
            Console.WriteLine($"Average: {elapsed.TotalMilliseconds / loopCount:#,0} mSec");
        }


*/
