
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
    static class ResourceMeterFilters
    {
        //.............................................................................
        #region FindDuplicates(), KeepLatest()
        //.............................................................................
        /// <summary>
        /// Filters future-dated-meters and retains the latest if there are multiple.
        /// </summary>
        public static IEnumerable<AzMeter> FindDuplicates(this IEnumerable<AzMeter> resourceMeters)
        {
            if (null == resourceMeters) throw new ArgumentNullException(nameof(resourceMeters));

            // Ignore future dated items.
            // Sort by descending EffectiveDate, keep the latest on top.
            // Group by alternate key, take the first entry.
            return resourceMeters
                .Where(x => x.EffectiveDate <= DateTime.UtcNow)
                .OrderByDescending(x => x.EffectiveDate)
                .GroupBy(x => new { x.MeterName, x.MeterCategory, x.MeterSubCategory, x.MeterRegion, x.Unit })
                .Where(g => g.Count() > 1)
                .SelectMany(g => g)
                ;
        }

        /// <summary>
        /// Filters future-dated-meters and retains the latest if there are multiple.
        /// </summary>
        public static IEnumerable<AzMeter> KeepLatest(this IEnumerable<AzMeter> resourceMeters)
        {
            if (null == resourceMeters) throw new ArgumentNullException(nameof(resourceMeters));

            // Ignore future dated items.
            // Sort by descending EffectiveDate, keep the latest on top.
            // Group by alternate key, take the first entry.
            return resourceMeters
                .Where(x => x.EffectiveDate <= DateTime.UtcNow)
                .OrderByDescending(x => x.EffectiveDate)
                .GroupBy(x => new { x.MeterName, x.MeterCategory, x.MeterSubCategory, x.MeterRegion, x.Unit })
                .Select(g => g.First())
                ;
        }

        #endregion

        //.............................................................................
        #region IEnumerable<AzMeter>.FilterData(filtersBaseFolder)
        //.............................................................................
        /// <summary>
        /// Applies filters specified in config folders.
        /// </summary>
        public static IEnumerable<AzMeter> FilterData(this IEnumerable<AzMeter> resourceMeters, string filtersBaseFolder)
        {
            if (null == resourceMeters) throw new ArgumentNullException(nameof(resourceMeters));
            if (null == filtersBaseFolder) throw new ArgumentNullException(nameof(filtersBaseFolder));
            if (!Directory.Exists(filtersBaseFolder)) throw new DirectoryNotFoundException($"FilterConfigurationBaseFolder not found: {filtersBaseFolder}");

            // Apply filters (if exists)
            // Syntax: ~/{propertyName}.keep|drop.txt
            resourceMeters = resourceMeters
                .FilterData(filtersBaseFolder, m => m.MeterId)
                .FilterData(filtersBaseFolder, m => m.MeterName)
                .FilterData(filtersBaseFolder, m => m.MeterCategory)
                .FilterData(filtersBaseFolder, m => m.MeterSubCategory)
                .FilterData(filtersBaseFolder, m => m.MeterRegion)
                .FilterData(filtersBaseFolder, m => m.Unit)
                ;

            // Category specific filters are organized as sub-folders.
            // The folder name is inferred as MeterCategoryName
            // Syntax: ~/{meterCategory}/{propertyName}.keep|drop.txt
            var categoryNames = new DirectoryInfo(filtersBaseFolder).GetDirectories().Select(x => x.Name);
            
            foreach (var targetCategory in categoryNames)
            {
                resourceMeters = resourceMeters
                    .FilterData(filtersBaseFolder, m => m.MeterId, targetCategory)
                    .FilterData(filtersBaseFolder, m => m.MeterName, targetCategory)
                    .FilterData(filtersBaseFolder, m => m.MeterCategory, targetCategory)
                    .FilterData(filtersBaseFolder, m => m.MeterSubCategory, targetCategory)
                    .FilterData(filtersBaseFolder, m => m.MeterRegion, targetCategory)
                    .FilterData(filtersBaseFolder, m => m.Unit, targetCategory)
                    ;
            }

            return resourceMeters;
        }

        static IEnumerable<AzMeter> FilterData(this IEnumerable<AzMeter> resourceMeters, string filtersBaseFolder, Expression<Func<AzMeter, string>> propertySelector, string targetCategory = null)
        {
            if (null == resourceMeters) throw new ArgumentNullException(nameof(resourceMeters));
            if (null == filtersBaseFolder) throw new ArgumentNullException(nameof(filtersBaseFolder));
            if (null == propertySelector) throw new ArgumentNullException(nameof(propertySelector));

            var propertyName = propertySelector.GetPropertyName();

            var whiteListFileName = null == targetCategory
                ? Path.Combine(filtersBaseFolder, $"{propertyName}.keep.txt")
                : Path.Combine(filtersBaseFolder, targetCategory, $"{propertyName}.keep.txt");

            var blackListFileName = null == targetCategory
                ? Path.Combine(filtersBaseFolder, $"{propertyName}.drop.txt")
                : Path.Combine(filtersBaseFolder, targetCategory, $"{propertyName}.drop.txt");

            // Load regex filters if the files exist.
            var rxWhiteList = File.Exists(whiteListFileName) ? ReadRegexFilters(whiteListFileName) : null;
            var rxBlackList = File.Exists(blackListFileName) ? ReadRegexFilters(blackListFileName) : null;

            var whiteListed = rxWhiteList?.Length > 0;
            var blackListed = rxBlackList?.Length > 0;

            // If neither whiteListed or blackListed, which will be most of the cases...
            if (!whiteListed && !blackListed)
            {
                // Optmiztion: Return the original sequence as-is.
                return resourceMeters;
            }
            else
            {
                // So that we know what files are applied...
                // if (whiteListed) Console.WriteLine(whiteListFileName);
                // if (blackListed) Console.WriteLine(blackListFileName);

                // Apply the filters
                var fxPropertySelector = propertySelector.Compile();
                return resourceMeters.FilterData(fxPropertySelector, rxWhiteList, rxBlackList, targetCategory);
            }
        }

        static IEnumerable<AzMeter> FilterData(this IEnumerable<AzMeter> resourceMeters, Func<AzMeter, string> propertySelector, Regex[] rxWhiteList, Regex[] rxBlacklist, string targetCategory = null)
        {
            if (null == resourceMeters) throw new ArgumentNullException(nameof(resourceMeters));
            if (null == propertySelector) throw new ArgumentNullException(nameof(propertySelector));

            targetCategory = NormalizeMeterCategoryName(targetCategory);

            foreach (var item in resourceMeters)
            {
                // Ignore NULLs.
                if (null == item) continue;

                // If target category specified...
                if (null != targetCategory)
                {
                    // If this filterset is targetted for a specific MeterCategory...
                    // Check if given resourceMeter belongs to suggested meter category.
                    var sameCategory =
                        null != item.MeterCategory && 
                        targetCategory.Equals(NormalizeMeterCategoryName(item.MeterCategory), StringComparison.OrdinalIgnoreCase);

                    // If this filterset is targetted for a specific MeterCategory...
                    // Don't drop the item, pass it along.
                    if (!sameCategory) yield return item;
                }

                // Evaluate whitelist and blacklist.
                var propertyValue = propertySelector(item);
                var shouldKeep = null == rxWhiteList || 0 == rxWhiteList.Length || propertyValue.MatchesAny(rxWhiteList);
                var shouldDrop = null != rxBlacklist && rxBlacklist.Length > 0 && propertyValue.MatchesAny(rxBlacklist);

                if (shouldKeep && !shouldDrop) yield return item;
            }

            static string NormalizeMeterCategoryName(string nameToNormalize)
            {
                // Remove spaces
                return nameToNormalize?.Replace(" ", string.Empty);
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
                .Trim()
                .IgnoreComments()
                .ToArray()
                .ToRegex(RegexOptions.Compiled | RegexOptions.IgnoreCase);
        }

        static string GetPropertyName(this Expression<Func<AzMeter, string>> propertySelector)
        {
            if (null == propertySelector) throw new ArgumentNullException(nameof(propertySelector));

            var memberExpression = propertySelector.Body as MemberExpression;
            if (null == memberExpression) throw new Exception("Invalid property selector. Not a member expression.");

            var propInfo = memberExpression.Member as PropertyInfo;
            if (null == propInfo) throw new Exception("Invalid property selector. Not a Property expression.");

            return propInfo.Name;
        }

        #endregion

    };
}
