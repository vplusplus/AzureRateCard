
using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using AzureRateCard;
using System.Linq;
using System;
using CsvHelper;
using System.Globalization;

namespace UnitTests
{
    [TestClass]
    public class CollectRawData
    {
        [TestMethod]
        public async Task RefreshRateCard()
        {
            using(var arm = ArmClient.Connect())
            {
                var subscriptionId = await arm.GetFirstSubscriptionId();
                var json = await arm.GetRateCardJsonAsync(subscriptionId);
                json = json.PrettyJson();

                File.WriteAllText("../../../RAW/RateCard.json", json);
            }
        }

        [TestMethod]
        public async Task RefreshComputeResourceSKUs()
        {
            using (var arm = ArmClient.Connect())
            {
                var subscriptionId = await arm.GetFirstSubscriptionId();
                var json = await arm.GetComputeResourcesSkus(subscriptionId);
                json = json.PrettyJson();

                File.WriteAllText("../../../RAW/ComputeResourcesSkus.json", json);
            }
        }

        [TestMethod]
        public void RefreshRegionList()
        {
            var items = RawData.RateCard.Value
                .Meters
                .Select(x => x.MeterRegion)
                .Distinct()
                .IgnoreBlankLines()
                .OrderBy(x => x)
                .ToList();

            var allRegionsFileName = Path.Combine(K.DataFolder, "AllRegions.txt");

            using(var txtOut = File.CreateText(allRegionsFileName))
            {
                txtOut.WriteLine($"// ------------------------------------------------------ ");
                txtOut.WriteLine($"// List of all regions");
                txtOut.WriteLine($"// Generated on {DateTime.Now}");
                txtOut.WriteLine($"// ------------------------------------------------------ ");

                foreach (var item in items) txtOut.WriteLine(item);
            }
        }

        [TestMethod]
        public void RefreshMeterCategories()
        {
            var csvFileName = Path.Combine(K.DataFolder, "MeterCategories.csv");

            RawData.RateCard.Value
                .Meters
                .Select(x => new { x.MeterCategory, x.MeterSubCategory })
                .Distinct()
                .OrderBy(x => x.MeterCategory)
                .ThenBy(x => x.MeterSubCategory)
                .SaveAsCsv(csvFileName);
        }

    }
}
