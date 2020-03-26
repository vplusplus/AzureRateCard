

using System;
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
                .FilterData(K.FilterConfigurationBaseFolder)
                .KeepLatest()
                .ToList()
                ;

            VirtualMachines.MapAndExport(meters, "D:/junk");
        }

        [TestMethod]
        public void FindDuplicateEntries()
        {
            var meters = RawData.RateCard.Value.Meters
                .FindDuplicates()
                .ToList()
                ;

            meters.SaveAsCsv("D:/Junk/Duplicates.csv");
        }

        [TestMethod]
        public void PrintRegionMap()
        {
            var map = RawData.RegionNameMap.Value;

            foreach (var pair in map) Console.WriteLine($"{pair.Key} = {pair.Value}");
        }


    }
}
