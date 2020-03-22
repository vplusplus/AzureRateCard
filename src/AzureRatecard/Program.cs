using AzureRateCard.Models;
using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace AzureRateCard
{
    // REF: https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/wiki

    class Program
    {
        static string PrettyJson(string json)
        {
            var something = Newtonsoft.Json.JsonConvert.DeserializeObject(json);
            return Newtonsoft.Json.JsonConvert.SerializeObject(something, Newtonsoft.Json.Formatting.Indented);
        }

        static async Task Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            try 
            {
                using(var armClient = ArmClient.Connect(MyConfig.MyTenantId, MyConfig.MyClientId, MyConfig.MyUserId, MyConfig.MyPassword))
                {
                    var path = "/subscriptions?api-version=2020-01-01";
                    var subs = await armClient.GetManyAsync<Subscription>(path);
                    var sub = subs?.FirstOrDefault();
                    if (null == sub) throw new Exception("Given credential do not have access to any Subscription. Please assign minimally READER access to atleast ONE subscription");

                    Console.WriteLine(sub.SubscriptionId);
                    Console.WriteLine(sub.DisplayName);

                    // var subscriptionId = sub.SubscriptionId;
                    // var rateCardPath = $"/subscriptions/{subscriptionId}/providers/Microsoft.Commerce/RateCard?api-version=2016-08-31-preview&$filter=OfferDurableId eq 'MS-AZR-0003p' and Currency eq 'USD' and Locale eq 'en-US' and RegionInfo eq 'US'";
                    // var rateCard = await armClient.GetAsAsync<RateCard>(rateCardPath);

                    // var json = Newtonsoft.Json.JsonConvert.SerializeObject(rateCard, Newtonsoft.Json.Formatting.Indented);
                    // File.WriteAllText("../../../SampleRateCard.json", json);
                }
            }
            catch(Exception err)
            {
                while(null != err)
                {
                    Console.WriteLine(err.Message);
                    Console.WriteLine(err.GetType().FullName);
                    err = err.InnerException;
                }
            }
        }
    }
}
