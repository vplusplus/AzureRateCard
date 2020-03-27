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
        public string Unit { get; set; }
        public string Region { get; set; }
        public double Rate { get; set; }

        internal VMRate ShallowClone()
        {
            return (VMRate)base.MemberwiseClone();
        }
    }

    class VMRateByRegion
    {
        public string MeterName { get; set; }
        public string MeterCategory { get; set; }
        public string MeterSubCategory { get; set; }

        public string SKU { get; set; }
        public string Unit { get; set; }
        public Dictionary<string, double> RateByRegion { get; set; }
    }

    class VMRateTallCsvMap : ClassMap<VMRate>
    {
        public static readonly ClassMap Instance = new VMRateTallCsvMap();

        private VMRateTallCsvMap()
        {
            int index = 0;

            Map(m => m.SKU).Index(++index);
            Map(m => m.MeterSubCategory).Name("Family").Index(++index);
            Map(m => m.Unit).Index(++index);
            Map(m => m.Region).Index(++index);
            Map(m => m.Rate).Index(++index);
        }
    }

    class VMRateWideCsvMap : ClassMap<VMRateByRegion>
    {
        internal VMRateWideCsvMap(string[] regionNames)
        {
            int index = 0;

            Map(m => m.SKU).Index(++index);
            Map(m => m.MeterSubCategory).Name("Family").Index(++index);
            Map(m => m.Unit).Index(++index);

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
        // Basic vs Standard
        // Windows vs Hybrid license
        // Tall vs Wide format

        public static void MapAndExport(IEnumerable<AzMeter> resourceMeters, string outputFolder)
        {
            // MAP / Split / Export
            var vmRates = resourceMeters
                .Where(x => x.MeterCategory.Equals("Virtual Machines"))
                .ToVMRate()
                .SplitSKUs()
                .MapVersion()
                .MapLicense()
                .MapOffer()
                .MapRegionName()
                .OrderBy(x => x.Version).ThenBy(x => x.SKU)
                .ToList()
                ;

            // Save all records...
            vmRates.SaveAsCsv(Path.Combine(outputFolder, "vm-all.csv"));

            // Split Basic|Standard, Windows|Hybrid; Save as Tall & Wide formats
            vmRates.ExportToCsv(outputFolder);
        }

        //...............................................................................
        #region ToVMRate(), ToVMRateByRegion
        //...............................................................................
        static IEnumerable<VMRate> ToVMRate(this IEnumerable<AzMeter> resourceMeters)
        {
            return resourceMeters
                .Select(ToVMRate)
                ;

            VMRate ToVMRate(AzMeter meter)
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
                    Unit = "1-month",
                    Rate = perMonth,

                };
            }
        }

        static IEnumerable<VMRateByRegion> ToVMRateByRegion(this IEnumerable<VMRate> vmRates)
        {
            return vmRates
                .GroupBy(x => new { x.MeterName, x.MeterCategory, x.MeterSubCategory, x.SKU, x.Offering, x.License, x.Version, x.Unit })
                .Select(g => new VMRateByRegion()
                {
                    MeterName = g.Key.MeterName,
                    MeterCategory = g.Key.MeterCategory,
                    MeterSubCategory = g.Key.MeterSubCategory,

                    SKU = g.Key.SKU,
                    Unit = g.Key.Unit,
                    RateByRegion = g.ToDictionary(i => i.Region, i => i.Rate)
                });
        }

        #endregion

        //...............................................................................
        #region SplitSKUs(), MapVersion(), MapOffer(), MapLicense()
        //...............................................................................

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

        static IEnumerable<VMRate> MapRegionName(this IEnumerable<VMRate> vmRates)
        {
            foreach (var item in vmRates)
            {
                var oldName = item.Region;
                item.Region = RawData.RegionNameMap.Value.TryGetValue(oldName, out var newName) ? newName : oldName;

                yield return item;
            }
        }


        #endregion

        //...............................................................................
        #region SplitAndExport
        //...............................................................................

        static void ExportToCsv(this IEnumerable<VMRate> vmRates, string baseFolder)
        {
            // Basic vs Standard
            //var basicVms = vmRates.Where(x => x.Offering.Equals("Basic")).ToList();
            var standardVms = vmRates.Where(x => x.Offering.Equals("Standard")).ToList();

            // Windows vs Hybrid
            var standardWindowsVms = standardVms.Where(x => x.License.Equals("Windows")).ToList();
            var standardHybridVms = standardVms.Where(x => x.License.Equals("BYOL")).ToList();

            standardWindowsVms.ExportTallAndWide(baseFolder, "vm-windows");
            standardHybridVms.ExportTallAndWide(baseFolder, "vm-hybrid");
        }

        static void ExportTallAndWide(this IEnumerable<VMRate> vmRates, string baseFolder, string fileNamePrefix)
        {
            // Save TALL version
            var tallFileName = Path.Combine(baseFolder, $"{fileNamePrefix}-tall.csv");
            vmRates.SaveAsCsv(tallFileName, VMRateTallCsvMap.Instance);

            // PIVOT the data by region.
            // Use the Region Name map as hint for specific order of pivot columns.
            var ratesByRegion = vmRates.ToVMRateByRegion().ToList();
            var regionNames = RawData.RegionNameMap.Value.Select(x => x.Value).ToArray();

            // Save WIDE version
            var wideFileName = Path.Combine(baseFolder, $"{fileNamePrefix}-wide.csv");
            var wideCsvMap = new VMRateWideCsvMap(regionNames);
            ratesByRegion.SaveAsCsv(wideFileName, wideCsvMap);
        }

        #endregion

    }
}
