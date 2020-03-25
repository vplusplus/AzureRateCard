using AzureRateCard.Models;
using CsvHelper.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace UnitTests.ETL
{
    class VMRate
    {
        public string MeterName { get; set; }
        public string MeterCategory { get; set; }
        public string MeterSubCategory { get; set; }

        public string SKU { get; set; }
        public string Offering { get; set; }
        public string License { get; set; }
        public string Version { get; set; }
        public string Region { get; set; }
        public double Rate { get; set; }

        internal VMRate ShallowClone()
        {
            return (VMRate)base.MemberwiseClone();
        }
    }

    class VMRateByRegion
    {
        public string SKU { get; set; }
        public string Offering { get; set; }
        public string License { get; set; }
        public string Version { get; set; }

        public Dictionary<string, double> RateByRegion { get; set; }
    }

    class VMRateTallCsvMap : ClassMap<VMRate>
    {
        public static readonly ClassMap Instance = new VMRateTallCsvMap();

        private VMRateTallCsvMap()
        {
            int index = 0;

            Map(m => m.SKU).Index(++index);
            Map(m => m.Region).Index(++index);
            Map(m => m.Rate).Index(++index);
            Map(m => m.Version).Index(++index);
            Map(m => m.MeterSubCategory).Index(++index);
        }
    }

    class VMRateWideCsvMap : ClassMap<VMRateByRegion>
    {
        internal VMRateWideCsvMap(string[] regionNames)
        {
            int index = 0;

            Map(m => m.SKU).Index(++index);
            Map(m => m.Version).Index(++index);

            foreach (var region in regionNames)
            {
                Map()
                    .Index(++index)
                    .Name(region)
                    .ConvertUsing(x =>
                    {
                        var rates = (x as VMRateByRegion).RateByRegion;
                        return rates.ContainsKey(region) ? rates[region].ToString() : "";
                    });
            }
        }
    }

    /// <summary>
    /// ETL steps to extract and process Virtual Machine Rate Card.
    /// </summary>
    static class VirtualMachines
    {
        // vm-basic|standard-windows|hybrid-wide|tall.csv

        public static void MapAndExport(IEnumerable<AzMeter> resourceMeters, string baseFolder)
        {
            // MAP / Split / Export
            var vmRates = resourceMeters
                .Where(x => x.MeterCategory.Equals("Virtual Machines"))
                .Select(ToVMRate)
                .SplitSKUs()
                .MapVersion()
                .MapLicense()
                .MapOffer()
                .OrderBy(x => x.Version).ThenBy(x => x.SKU)
                .ToList()
                ;

            vmRates.SaveAsCsv(Path.Combine(baseFolder, "vms.all.csv"));
            vmRates.SplitAndExport(baseFolder, "vms");
        }

        static VMRate ToVMRate(this AzMeter meter)
        {
            var perHour = (meter.MeterRates?.FirstOrDefault().Value).GetValueOrDefault(0.0);
            var perMonth = perHour * K.HoursPerMonth;

            return new VMRate()
            {
                MeterName = meter.MeterName,
                MeterCategory = meter.MeterCategory,
                MeterSubCategory = meter.MeterSubCategory,

                SKU = meter.MeterName,
                Region = meter.MeterRegion,
                Rate = perMonth,
            };
        }

        static IEnumerable<VMRate> SplitSKUs(this IEnumerable<VMRate> vmRates)
        {
            foreach(var item in vmRates)
            {
                var shouldSplit = item.SKU.IndexOf('/') > 0;
                
                if (shouldSplit)
                {
                    var skuParts = item
                        .SKU.Split('/')
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .Select(x => x.Trim());

                    foreach (var part in skuParts)
                    {
                        var clone = item.ShallowClone();
                        clone.SKU = part;
                        yield return clone;
                    }
                }
                else
                {
                    yield return item;
                }
            }
        }

        static IEnumerable<VMRate> MapVersion(this IEnumerable<VMRate> vmRates)
        {
            foreach(var item in vmRates)
            {
                var sku = item.SKU;
                item.Version =
                    sku.EndsWith("v1") ? "v1" :
                    sku.EndsWith("v2") ? "v2" :
                    sku.EndsWith("v3") ? "v3" :
                    sku.EndsWith("v4") ? "v4" :
                    sku.EndsWith("v5") ? "v5" :
                    null;

                yield return item;
            }
        }

        static IEnumerable<VMRate> MapOffer(this IEnumerable<VMRate> vmRates)
        {
            foreach (var item in vmRates)
            {
                item.Offering = item.MeterSubCategory.EndsWith("Basic") ? "Basic" : "Standard";
                yield return item;
            }
        }

        static IEnumerable<VMRate> MapLicense(this IEnumerable<VMRate> vmRates)
        {
            foreach (var item in vmRates)
            {
                item.License = item.MeterSubCategory.EndsWith("Windows") ? "Windows" : "BYOL";
                yield return item;
            }
        }

        private static void SplitAndExport(this IEnumerable<VMRate> vmRates, string baseFolder, string fileNamePrefix)
        {
            var basicVms = vmRates.Where(x => x.Offering.Equals("Basic")).ToList();
            var standardVms = vmRates.Where(x => x.Offering.Equals("Standard")).ToList();

            if (basicVms.Count > 0) basicVms.ExportWindowsAndByol(baseFolder, $"{fileNamePrefix}-basic");
            if (standardVms.Count > 0) standardVms.ExportWindowsAndByol(baseFolder, $"{fileNamePrefix}-standard");
        }

        static void ExportWindowsAndByol(this IEnumerable<VMRate> vmRates, string baseFolder, string fileNamePrefix)
        {
            var windowsVms = vmRates.Where(x => x.License.Equals("Windows")).ToList();
            var byolVms = vmRates.Where(x => x.License.Equals("BYOL")).ToList();

            if (windowsVms.Count > 0) windowsVms.ExportTallAndWide(baseFolder, $"{fileNamePrefix}-windows");
            if (byolVms.Count > 0) byolVms.ExportTallAndWide(baseFolder, $"{fileNamePrefix}-byol");
        }

        static void ExportTallAndWide(this IEnumerable<VMRate> vmRates, string baseFolder, string fileNamePrefix)
        {
            // Save TALL version
            var tallFileName = Path.Combine(baseFolder, $"{fileNamePrefix}-tall.csv");
            vmRates.SaveAsCsv(tallFileName, VMRateTallCsvMap.Instance);

            // List of region names
            var regionNames = vmRates.Select(x => x.Region).Distinct().Sort().ToArray();

            var ratesByRegion = vmRates
                .GroupBy(x => new { x.MeterName, x.MeterCategory, x.MeterSubCategory, x.SKU, x.Offering, x.License, x.Version })
                .Select(g => new VMRateByRegion()
                {
                    SKU = g.Key.SKU,
                    Offering = g.Key.Offering,
                    License = g.Key.License,
                    Version = g.Key.Version,
                    RateByRegion = g.ToDictionary(i => i.Region, i => i.Rate)
                })
                .ToList();

            var wideFileName = Path.Combine(baseFolder, $"{fileNamePrefix}-wide.csv");
            var wideCsvMap = new VMRateWideCsvMap(regionNames);
            ratesByRegion.SaveAsCsv(wideFileName, wideCsvMap);

        }



    }
}
