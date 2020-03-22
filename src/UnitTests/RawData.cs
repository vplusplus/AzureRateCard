using AzureRateCard.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using System.Linq;

namespace UnitTests
{
    // TODO: Decide caching strategy, reading from blob, local cache, in-memory cache, etc.

    internal static class RawData
    {
        public static readonly Lazy<RateCard> RateCard = new Lazy<RateCard>(LoadRateCardOnce);
        static RateCard LoadRateCardOnce()
        {
            using (var file = File.OpenText(K.RateCardFileName))
            {
                var serializer = new JsonSerializer();
                return (RateCard)serializer.Deserialize(file, typeof(RateCard));
            }
        }

        public static IEnumerable<string> AllRegions => LoadRegionNames(Path.Combine(K.DataFolder, "Regions.All.txt"));
        public static IEnumerable<string> SelectRegions => LoadRegionNames(Path.Combine(K.DataFolder, "Regions.txt"));
        static IReadOnlyList<string> LoadRegionNames(string fileName)
        {
            return File
                .ReadAllLines(fileName)
                .IgnoreNullsBlanksCommentsAndPleaseTrim()
                .Sort()
                .ToList();
        }
        static IEnumerable<string> IgnoreNullsBlanksCommentsAndPleaseTrim(this IEnumerable<string> items)
        {
            return items?
                .IgnoreNulls()
                .IgnoreBlankLines()
                .IgnoreComments()
                .Trim();
        }

    }
}
