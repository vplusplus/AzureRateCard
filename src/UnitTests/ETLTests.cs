

using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UnitTests.ETL;

namespace UnitTests
{
    [TestClass]
    public class ETLTests
    {
        [TestMethod]
        public void VM_ETL_Tests()
        {
            var meters = RawData.RateCard.Value.Meters.AsEnumerable();

            meters = meters
                .ApplyConfigFilters(K.FilterConfigurationBaseFolder)
                .KeepLatest()
                .ToList()
                ;

            VirtualMachines.MapAndExport(meters, "../../../junk");
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
