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
        public static readonly Lazy<AzRateCard> RateCard = new Lazy<AzRateCard>(LoadRateCardOnce);
        static AzRateCard LoadRateCardOnce()
        {
            using (var file = File.OpenText(K.RateCardFileName))
            {
                var serializer = new JsonSerializer();
                return (AzRateCard)serializer.Deserialize(file, typeof(AzRateCard));
            }
        }

        public static IEnumerable<string> AllRegions => LoadRegionNames(Path.Combine(K.DataFolder, "Regions.All.txt"));
        public static IEnumerable<string> SelectRegions => LoadRegionNames(Path.Combine(K.DataFolder, "Regions.keep.txt"));
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
