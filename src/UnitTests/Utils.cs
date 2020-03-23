
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

using Newtonsoft.Json;
using CsvHelper;
using CsvHelper.Configuration;

namespace UnitTests
{
    internal static class Utils
    {
        public static IEnumerable<T> IgnoreNulls<T>(this IEnumerable<T> items) where T : class
        {
            return null != items
                ? items.Where(x => null != x)
                : items;
        }

        public static IEnumerable<string> IgnoreBlankLines(this IEnumerable<string> lines)
        {
            return null != lines
                ? lines.Where(x => !string.IsNullOrWhiteSpace(x))
                : lines;
        }

        public static IEnumerable<string> IgnoreComments(this IEnumerable<string> lines)
        {
            return null != lines
                ? lines.Where(x => !x.TrimStart().StartsWith("//"))
                : lines;
        }

        public static IEnumerable<string> Trim(this IEnumerable<string> lines)
        {
            return lines?.Select(x => x.Trim());
        }

        public static IEnumerable<string> Sort(this IEnumerable<string> lines)
        {
            return lines?.OrderBy(x => x);
        }

        public static bool In(this string something, IEnumerable<string> set)
        {
            return null != something && null != set && set.Any(x => x.Equals(something));
        }

        public static string ToJson<T>(this T something)
        {
            return null == something ? null : JsonConvert.SerializeObject(something, Formatting.Indented);
        }

        public static string ToPrettyJson(this string json)
        {
            // Pretty inefficient. 
            // Good enough for testing.
            // Dont use in production.
            return string.IsNullOrWhiteSpace(json)
                ? json
                : JsonConvert.SerializeObject(JsonConvert.DeserializeObject(json), Formatting.Indented);
        }

        public static void SaveAsText(this string something, string fileName)
        {
            var folder = Path.GetDirectoryName(fileName);
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
            File.WriteAllText(fileName, something);
        }

        public static void SaveAsCsv<T>(this IEnumerable<T> items, string csvFileName, ClassMap map = null)
        {
            if (null == items) throw new ArgumentNullException(nameof(items));
            if (null == csvFileName) throw new ArgumentNullException(nameof(csvFileName));

            var dir = Path.GetDirectoryName(csvFileName);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            using (var writer = new StreamWriter(csvFileName))
            using (var csvWriter = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                if (null != map) csvWriter.Configuration.RegisterClassMap(map);
                csvWriter.WriteRecords(items);
            }
        }

        public static bool ContainsAny(this string something, string[] set)
        {
            return set.Any(x => something.Contains(x));
        }

        public static TValue GetValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue defaultValue)
        {
            return dictionary.TryGetValue(key, out var value)
                ? value
                : defaultValue;
        }
    }
}

