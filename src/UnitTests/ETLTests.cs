

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
