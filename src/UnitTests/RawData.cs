
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Newtonsoft.Json;
using AzureRateCard.Models;

namespace UnitTests
{
    // TODO: Decide caching strategy, reading from blob, local cache, in-memory cache, etc.

    internal static class RawData
    {
        public static readonly Lazy<AzRateCard> RateCard = new Lazy<AzRateCard>(LoadRateCardOnce);

        static AzRateCard LoadRateCardOnce()
        {
            using (var file = File.OpenText(K.RateCardFileName))
            {
                var serializer = new JsonSerializer();
                return (AzRateCard)serializer.Deserialize(file, typeof(AzRateCard));
            }
        }

        static IReadOnlyList<string> LoadRegionNames(string fileName)
        {
            return RateCard
                .Value
                .Meters
                .Select(x => x.MeterRegion)
                .Distinct()
                .Sort()
                .ToList();
        }
    }
}
