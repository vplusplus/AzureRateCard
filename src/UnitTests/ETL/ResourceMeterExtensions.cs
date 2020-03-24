
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;
using AzureRateCard.Models;

namespace UnitTests.ETL
{
    static class ResourceMeterExtensions
    {

        /// <summary>
        /// There may be multiple Resouce Meters, based on effective date.
        /// Filters future-dated-meters and retains the latest of there are multiple.
        /// </summary>
        public static IEnumerable<AzMeter> KeepLatest(this IEnumerable<AzMeter> resourceMeters)
        {
            if (null == resourceMeters) throw new ArgumentNullException(nameof(resourceMeters));

            return resourceMeters
                .Where(x => x.EffectiveDate <= DateTime.UtcNow)
                .OrderByDescending(x => x.EffectiveDate)
                .GroupBy(x => new { x.MeterName, x.MeterCategory, x.MeterSubCategory, x.MeterRegion, x.Unit })
                .Select(g => g.First())
                ;
        }

        //.............................................................................
        #region IEnumerable<AzMeter>.ApplyConfigFilters()
        //.............................................................................
        /// <summary>
        /// Applies filters specified in config folders.
        /// </summary>
        public static IEnumerable<AzMeter> ApplyConfigFilters(this IEnumerable<AzMeter> resourceMeters, string filterConfigurationBaseFolder)
        {
            if (null == resourceMeters) throw new ArgumentNullException(nameof(resourceMeters));

            var noFilters = !Directory.Exists(filterConfigurationBaseFolder);
            if (noFilters) return resourceMeters;

            // Apply filters (if exists)
            // Syntax:  ~/Config/Filters/{propertyName}.keep|drop.txt
            resourceMeters = resourceMeters
                .ApplyConfigFilters(filterConfigurationBaseFolder, m => m.MeterId)
                .ApplyConfigFilters(filterConfigurationBaseFolder, m => m.MeterName)
                .ApplyConfigFilters(filterConfigurationBaseFolder, m => m.MeterCategory)
                .ApplyConfigFilters(filterConfigurationBaseFolder, m => m.MeterSubCategory)
                .ApplyConfigFilters(filterConfigurationBaseFolder, m => m.MeterRegion)
                .ApplyConfigFilters(filterConfigurationBaseFolder, m => m.Unit)
                ;

            // Look for category specific filters.
            var categoryNames = new DirectoryInfo(filterConfigurationBaseFolder).GetDirectories().Select(x => x.Name);

            // Apply meterCategory specific filters if specified.
            // Syntax: ~/Config/Filters/{meterCategory}/{propertyName}.keep|drop.txt
            foreach (var categoryName in categoryNames)
            {
                Console.WriteLine($"??? {categoryName}");

                resourceMeters = resourceMeters
                    .ApplyConfigFilters(filterConfigurationBaseFolder, m => m.MeterId, categoryName)
                    .ApplyConfigFilters(filterConfigurationBaseFolder, m => m.MeterName, categoryName)
                    .ApplyConfigFilters(filterConfigurationBaseFolder, m => m.MeterCategory, categoryName)
                    .ApplyConfigFilters(filterConfigurationBaseFolder, m => m.MeterSubCategory, categoryName)
                    .ApplyConfigFilters(filterConfigurationBaseFolder, m => m.MeterRegion, categoryName)
                    .ApplyConfigFilters(filterConfigurationBaseFolder, m => m.Unit, categoryName)
                    ;
            }

            return resourceMeters;
        }

        static IEnumerable<AzMeter> ApplyConfigFilters(this IEnumerable<AzMeter> resourceMeters, string filtersBaseFolder, Expression<Func<AzMeter, string>> propertySelector, string meterCategory = null)
        {
            if (null == resourceMeters) throw new ArgumentNullException(nameof(resourceMeters));
            if (null == filtersBaseFolder) throw new ArgumentNullException(nameof(filtersBaseFolder));
            if (null == propertySelector) throw new ArgumentNullException(nameof(propertySelector));

            var propertyName = propertySelector.GetPropertyName();

            var whiteListFileName = null == meterCategory
                ? Path.Combine(filtersBaseFolder, $"{propertyName}.keep.txt")
                : Path.Combine(filtersBaseFolder, meterCategory, $"{propertyName}.keep.txt");

            var blackListFileName = null == meterCategory
                ? Path.Combine(filtersBaseFolder, $"{propertyName}.drop.txt")
                : Path.Combine(filtersBaseFolder, meterCategory, $"{propertyName}.drop.txt");

            // Load regex filters if the files exist.
            var rxWhiteList = File.Exists(whiteListFileName) ? ReadRegexFilters(whiteListFileName) : null;
            var rxBlackList = File.Exists(blackListFileName) ? ReadRegexFilters(blackListFileName) : null;

            var noWhiteList = null == rxWhiteList || 0 == rxWhiteList.Length;
            var noBlackList = null == rxBlackList || 0 == rxBlackList.Length;

            if (noWhiteList && noBlackList)
            {
                // Optmization:
                // If neither whiteList or blackList is specified, which will be most of the cases,
                // return the original sequence as-is.
                return resourceMeters;
            }
            else
            {
                // TODO: Avoid double-negative
                if (!noWhiteList) Console.WriteLine(whiteListFileName);
                if (!noBlackList) Console.WriteLine(blackListFileName);

                // Apply the filters
                var fxPropertySelector = propertySelector.Compile();
                return resourceMeters.ApplyConfigFilters(fxPropertySelector, rxWhiteList, rxBlackList, meterCategory);
            }
        }

        static IEnumerable<AzMeter> ApplyConfigFilters(this IEnumerable<AzMeter> resourceMeters, Func<AzMeter, string> propertySelector, Regex[] rxWhiteList, Regex[] rxBlacklist, string appliesToMeterCategory = null)
        {
            if (null == resourceMeters) throw new ArgumentNullException(nameof(resourceMeters));
            if (null == propertySelector) throw new ArgumentNullException(nameof(propertySelector));

            foreach(var item in resourceMeters)
            {
                if (null == item) continue;

                // If this filterset is targetted for a specific MeterCategory...
                // Note: The name is inferred from directory name, hence represents a normalized version.
                if (null != appliesToMeterCategory)
                {
                    var sameCategory =
                        null != item.MeterCategory && (
                            item.MeterCategory.Equals(appliesToMeterCategory, StringComparison.OrdinalIgnoreCase) ||
                            item.MeterCategory.Replace(" ", string.Empty).Equals(appliesToMeterCategory, StringComparison.OrdinalIgnoreCase) 
                        );

                    // If this filterset is targetted for a specific MeterCategory...
                    // Don't drop the item, pass t along.
                    if (!sameCategory) yield return item;
                }
                else
                {
                    // EMPTY ELSE BLOCK to avoid future logic error.
                    // Following code is expected to be a fall throough
                }

                // Evaluate whitelist and blacklist.
                var propertyValue = propertySelector(item);
                var shouldKeep = null == rxWhiteList || 0 == rxWhiteList.Length || propertyValue.MatchesAny(rxWhiteList);
                var shouldDrop = null != rxBlacklist && rxBlacklist.Length > 0 && propertyValue.MatchesAny(rxBlacklist);

                if (shouldKeep && !shouldDrop) yield return item;
            }
        }

        static bool MatchesAny(this string something, Regex[] rxPatterns)
        {
            return 
                null != something && 
                null != rxPatterns && 
                rxPatterns.Length > 0 &&
                rxPatterns.Any(x => x.IsMatch(something));
        }

        static Regex[] ReadRegexFilters(string filterFileName)
        {
            return File.ReadAllLines(filterFileName)
                .IgnoreNulls()
                .IgnoreBlankLines()
                .IgnoreComments()
                .Trim()
                .ToArray()
                .ToRegex();
        }

        static string GetPropertyName(this Expression<Func<AzMeter, string>> propertySelector)
        {
            if (null == propertySelector) throw new ArgumentNullException(nameof(propertySelector));

            MemberExpression memberExpression = propertySelector.Body as MemberExpression;
            if (null == memberExpression) throw new Exception("Invalid property selector. Not a member expression.");

            PropertyInfo propInfo = memberExpression.Member as PropertyInfo;
            if (null == propInfo) throw new Exception("Invalid property selector. Not a Property expression.");

            return propInfo.Name;
        }

        #endregion

    };
}


/*
        static IEnumerable<AzMeter> Keep(this IEnumerable<AzMeter> resourceMeters, Func<AzMeter, string> propertySelector, Regex[] rxPatterns)
        {
            if (null == resourceMeters) throw new ArgumentNullException(nameof(resourceMeters));
            if (null == propertySelector) throw new ArgumentNullException(nameof(propertySelector));

            return null == rxPatterns || 0 == rxPatterns.Length
                ? resourceMeters
                : resourceMeters.Where(m => propertySelector(m).MatchesAny(rxPatterns));
        }

        static IEnumerable<AzMeter> Drop(this IEnumerable<AzMeter> resourceMeters, Func<AzMeter, string> propertySelector, Regex[] rxPatterns)
        {
            if (null == resourceMeters) throw new ArgumentNullException(nameof(resourceMeters));
            if (null == propertySelector) throw new ArgumentNullException(nameof(propertySelector));

            return null == rxPatterns || 0 == rxPatterns.Length
                ? resourceMeters
                : resourceMeters.Where(m => !propertySelector(m).MatchesAny(rxPatterns));
        }
*/
