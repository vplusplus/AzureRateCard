using Microsoft.VisualStudio.TestTools.UnitTesting;
using AzureRateCard.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.IO;
using System.Linq;
using System;
using CsvHelper;
using System.Globalization;
using System.Collections.Generic;
using System.Threading.Tasks;
using AzureRateCard;

namespace UnitTests
{
    [TestClass]
    public class UnitTest1
    {
        const string SampleRateCardFileName = "../../../SampleJsons/SampleRateCard.json";

        static RateCard LoadSampleRateCard() 
        {
            using (var file = File.OpenText(SampleRateCardFileName))
            {
                JsonSerializer serializer = new JsonSerializer();
                return (RateCard)serializer.Deserialize(file, typeof(RateCard));
            }
        }

        static string[] IgnoreRegions => File
            .ReadAllLines("../../../Data/IgnoreRegions.txt")
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Where(x => !x.StartsWith("//"))
            .ToArray();

        static string[] AllRegions => File
            .ReadAllLines("../../../Data/AllRegions.txt")
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Where(x => !x.StartsWith("//"))
            .ToArray();


        [TestMethod]
        public void DistinctCategories()
        {
            var rc = LoadSampleRateCard();

            var categories = rc.Meters
                .Select(x => new { x.MeterCategory, x.MeterSubCategory })
                .Distinct()
                .OrderBy(x => x.MeterCategory)
                .ThenBy(x => x.MeterSubCategory)
                .ToList();

            using (var writer = new StreamWriter("../../../Data/MeterCategories.csv"))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                csv.WriteRecords(categories);
            }
        }

        [TestMethod]
        public void DistinctRegions()
        {
            var rc = LoadSampleRateCard();

            var categories = rc.Meters
                .Select(x => new { x.MeterRegion })
                .Distinct()
                .OrderBy(x => x.MeterRegion)
                .ToList();

            using (var writer = new StreamWriter("../../../Data/MeterRegions.csv"))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                csv.WriteRecords(categories);
            }
        }

        const int HoursPerMonth = 365 * 24 / 12;

        [TestMethod]
        public void JustVMs()
        {
            var rc = LoadSampleRateCard();
            var selectRegions = AllRegions.Except(IgnoreRegions).ToArray();
           

            var data = rc.Meters
                .Where(x => x.MeterCategory.Equals("Virtual Machines"))
                .Where(x =>  x.MeterRegion.In(selectRegions))
                .Select(x => new
                {
                    EffectiveDate = DateTime.Parse(x.EffectiveDate).ToString("dd-MMM-yyyy"),
                    x.MeterName,
                    x.MeterSubCategory, 
                    x.MeterRegion,
                    //x.Unit,
                    PerHour = x.MeterRates?.FirstOrDefault().Value,
                    PerMonth = x.MeterRates?.FirstOrDefault().Value * HoursPerMonth,
                    //x.MeterRates,
                    //MeterRate = x.MeterRates?.FirstOrDefault()?.Value,
                })
                .ToList();

            using (var writer = new StreamWriter("../../../Data/VirtualMachines.csv"))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                csv.WriteRecords(data);
            }
        }

        [TestMethod]
        public void PrintKeptRegions()
        {
            var keep = AllRegions.Except(IgnoreRegions).ToList();
            foreach (var name in keep) Console.WriteLine(name);
        }

        [TestMethod]
        public async Task GetVirtualMachineSKUs()
        {
            // GET https://management.azure.com/subscriptions/{subscriptionId}/providers/Microsoft.Compute/skus?api-version=2019-04-01

            var tenantID = System.Configuration.ConfigurationManager.AppSettings["RateCard.TenantId"] ?? throw new Exception("APP SETTING ISSUE");


            var subscriptionId = "396fa5f0-204b-42cb-9125-85d9775e1f77";

            using(var arm = ArmClient.Connect())
            {
                var path = $"/subscriptions/{subscriptionId}/providers/Microsoft.Compute/skus?api-version=2019-04-01";

                var json = await arm.GetStringAsync(path);
                json = json.PrettyJson();

                File.WriteAllText("../../../SampleJsons/VirtualMachineSkus.json", json);
            }

        }
    }


    internal static class MoreLinq
    {
        public static bool In(this string something, IEnumerable<string> set)
        {
            return null != something && null != set && set.Any(x => x.Equals(something));
        }

        public static string PrettyJson(this string json)
        {
            // Pretty inefficient. 
            // Good enough for testing.
            // Dont use in production.
            return string.IsNullOrWhiteSpace(json)
                ? json
                : JsonConvert.SerializeObject(JsonConvert.DeserializeObject(json), Formatting.Indented);
        }

    }


}
