using Microsoft.VisualStudio.TestTools.UnitTesting;
using AzureRateCard.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.IO;
using System.Linq;
using System;
using CsvHelper;
using System.Globalization;

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

        [TestMethod]
        public void SaveCategories()
        {
            var rc = LoadSampleRateCard();

            var categories = rc.Meters
                .Select(x => new { x.MeterCategory, x.MeterSubCategory })
                .Distinct()
                .OrderBy(x => x.MeterCategory)
                .ThenBy(x => x.MeterSubCategory)
                .ToList();

            using (var writer = new StreamWriter("../../../CSV/MeterCategories.csv"))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                csv.WriteRecords(categories);
            }
        }
    }
}
