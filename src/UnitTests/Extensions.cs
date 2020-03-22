
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace UnitTests
{
    internal static class Extensions
    {
        public static bool In(this string something, IEnumerable<string> set)
        {
            return null != something && null != set && set.Any(x => x.Equals(something));
        }

        public static string PrettyJson(this string json)
        {
            // Pretty inefficient. 
            // Good enough for testing.
            // Dont use in production.
            return string.IsNullOrWhiteSpace(json)
                ? json
                : JsonConvert.SerializeObject(JsonConvert.DeserializeObject(json), Formatting.Indented);
        }
    }
}
