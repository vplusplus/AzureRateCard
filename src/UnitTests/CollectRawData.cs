
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using AzureRateCard;
using Microsoft.VisualStudio.TestTools.UnitTesting;

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





    }
}
