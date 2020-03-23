using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace UnitTests.ETL
{
    static class Utils
    {
        /// <summary>
        /// Translates given reg-ex patterns to compiled regular expressions.
        /// </summary>
        internal static Regex[] ToRegex(this string[] rxPatterns, RegexOptions options = RegexOptions.Compiled | RegexOptions.IgnoreCase)
        {
            if (null == rxPatterns) throw new ArgumentNullException(nameof(rxPatterns));

            return rxPatterns
                .Where(x => !string.IsNullOrEmpty(x))
                .Select(x => x.Trim())
                .Select(x => new Regex(x, options))
                .ToArray()
                ;
        }

    }
}
