
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
        public static readonly Lazy<IDictionary<string, string>> RegionNameMap = new Lazy<IDictionary<string, string>>(LoadRegionNameMapOnce);

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

        static IDictionary<string, string> LoadRegionNameMapOnce()
        {
            var regionNameMapFileName = Path.Combine(K.ConfigFolder, "Maps", "MeterRegion.map.txt");

            return !File.Exists(regionNameMapFileName) ? new Dictionary<string, string>() :
                File.ReadAllLines(regionNameMapFileName)
                    .IgnoreNulls()
                    .IgnoreBlankLines()
                    .IgnoreComments()
                    .Select(x => x.Split('|'))
                    .Where(x => x.Length == 2)
                    .Where(x => !string.IsNullOrWhiteSpace(x[0]) && !string.IsNullOrWhiteSpace(x[1]))
                    .ToDictionary(x => x[0].Trim(), x => x[1].Trim(), StringComparer.OrdinalIgnoreCase)
                    ;
        }

    }
}
