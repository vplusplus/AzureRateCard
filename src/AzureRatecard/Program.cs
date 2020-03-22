
using System;
using System.Linq;
using System.Threading.Tasks;
using AzureRateCard.Models;

namespace AzureRateCard
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            try 
            {
                using(var armClient = ArmClient.Connect())
                {
                    var path = "/subscriptions?api-version=2020-01-01";
                    var subs = await armClient.GetManyAsync<Subscription>(path);
                    var sub = subs?.FirstOrDefault();
                    if (null == sub) throw new Exception("Given credential do not have access to any Subscription. Please assign minimally READER access to atleast ONE subscription");

                    Console.WriteLine(sub.SubscriptionId);
                    Console.WriteLine(sub.DisplayName);
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
