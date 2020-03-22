
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CsvHelper;
using AzureRateCard.Models;
using System.Diagnostics;

namespace UnitTests
{
    [TestClass]
    public class UnitTest1
    {
        const string RateCardFileName = "../../../RAW/RateCard.json";

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
        public void JustVMs()
        {
            var selectRegions = RawData.SelectRegions.ToList();
           
            var data = RawData.RateCard.Value
                .Meters
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
                    PerMonth = x.MeterRates?.FirstOrDefault().Value * K.HoursPerMonth,
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
    }




}
