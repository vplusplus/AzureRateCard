
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

        #region IEnumerable<AzMeter>. Filter(), Keep(), Drop()

        /// <summary>
        /// Filters resource meters based on suggested property value.
        /// If whitelist is NOT specified, all are kept.
        /// If blacklist is NOT specified, none of them are dropped.
        /// </summary>
        public static IEnumerable<AzMeter> Filter(this IEnumerable<AzMeter> resourceMeters, Func<AzMeter, string> propertySelector, Regex[] rxWhitelist, Regex[] rxBlacklist)
        {
            if (null == resourceMeters) throw new ArgumentNullException(nameof(resourceMeters));
            if (null == propertySelector) throw new ArgumentNullException(nameof(propertySelector));
            if (null != rxWhitelist && rxWhitelist.Any(rx => null == rx)) throw new ArgumentNullException(nameof(rxWhitelist));
            if (null != rxBlacklist && rxBlacklist.Any(rx => null == rx)) throw new ArgumentNullException(nameof(rxBlacklist));

            foreach (var meter in resourceMeters)
            {
                // Ignore NULLs.
                if (null == meter) continue;

                // Pickup suggested property value.
                var propertyValue = propertySelector(meter);

                // Evaluate...
                var shouldKeep = null == rxWhitelist || 0 == rxWhitelist.Length || rxWhitelist.Any(rx => rx.IsMatch(propertyValue));
                var shouldDrop = null != rxBlacklist && rxBlacklist.Length > 0  && rxBlacklist.Any(rx => rx.IsMatch(propertyValue));

                if (shouldKeep && !shouldDrop) yield return meter;
            }
        }

        public static IEnumerable<AzMeter> Filter(this IEnumerable<AzMeter> resourceMeters, Func<AzMeter, string> propertySelector, string[] rxWhitelist, string[] rxBlacklist)
        {
            return Filter(resourceMeters, propertySelector, rxWhitelist?.ToRegex(), rxBlacklist?.ToRegex());
        }

        public static IEnumerable<AzMeter> Keep(this IEnumerable<AzMeter> resourceMeters, Func<AzMeter, string> propertySelector, string[] rxWhitelist)
        {
            return Filter(resourceMeters, propertySelector, rxWhitelist, null);
        }

        public static IEnumerable<AzMeter> Drop(this IEnumerable<AzMeter> resourceMeters, Func<AzMeter, string> propertySelector, string[] rxBlacklist)
        {
            return Filter(resourceMeters, propertySelector, null, rxBlacklist);
        }

        public static IEnumerable<AzMeter> Keep(this IEnumerable<AzMeter> resourceMeters, Func<AzMeter, string> propertySelector, string rxWhitelist)
        {
            return Filter(resourceMeters, propertySelector, new string[] { rxWhitelist }, null);
        }

        public static IEnumerable<AzMeter> Drop(this IEnumerable<AzMeter> resourceMeters, Func<AzMeter, string> propertySelector, string rxBlacklist)
        {
            return Filter(resourceMeters, propertySelector, null, new string[] { rxBlacklist });
        }

        #endregion

        //.............................................................................
        #region ApplyConfigFilters()
        //.............................................................................
        /// <summary>
        /// Applies filters specified in config folders.
        /// </summary>
        public static IEnumerable<AzMeter> ApplyConfigFilters(this IEnumerable<AzMeter> resourceMeters, string resourceType = null)
        {
            return resourceMeters
                .ApplyConfigFilters(x => x.MeterId, x => x.MeterId, resourceType)
                .ApplyConfigFilters(x => x.MeterName, x => x.MeterName, resourceType)
                .ApplyConfigFilters(x => x.MeterCategory, x => x.MeterCategory, resourceType)
                .ApplyConfigFilters(x => x.MeterSubCategory, x => x.MeterSubCategory, resourceType)
                .ApplyConfigFilters(x => x.MeterRegion, x => x.MeterRegion, resourceType)
                .ApplyConfigFilters(x => x.Unit, x => x.Unit, resourceType)
                ;
        }

        static IEnumerable<AzMeter> ApplyConfigFilters(this IEnumerable<AzMeter> resourceMeters, Expression<Func<AzMeter, string>> propertyNameSelector, Func<AzMeter, string> propertyValueSelector, string optionalResourceType)
        {
            var propertyName = GetPropertyName(propertyNameSelector);

            var configFolder = K.ConfigFolder;
            var whiteListFileName = $"{propertyName}.keep.txt";
            var blackListFileName = $"{propertyName}.drop.txt";

            var whiteListFilePath = string.IsNullOrWhiteSpace(optionalResourceType)
                ? Path.Combine(configFolder, whiteListFileName)
                : Path.Combine(configFolder, optionalResourceType, whiteListFileName);

            var blackListFilePath = string.IsNullOrWhiteSpace(optionalResourceType)
                ? Path.Combine(configFolder, blackListFileName)
                : Path.Combine(configFolder, optionalResourceType, blackListFileName);

            // Load regex filters if specified.
            var rxWhiteList = File.Exists(whiteListFilePath) ? LoadFilters(whiteListFilePath) : null;
            var rxBlackList = File.Exists(blackListFilePath) ? LoadFilters(blackListFilePath) : null;

            if (null != rxWhiteList) Console.WriteLine($"Applying filter: {whiteListFilePath}");
            if (null != rxBlackList) Console.WriteLine($"Applying filter: {blackListFilePath}");

            // If EITHER whitelisted or blacklisted, apply the filters.
            return null != rxWhiteList || null != rxBlackList
                ? resourceMeters.Filter(propertyValueSelector, rxWhiteList, rxBlackList)
                : resourceMeters;

            Regex[] LoadFilters(string filterFileName)
            {
                return File.ReadAllLines(filterFileName)
                    .IgnoreNulls()
                    .IgnoreBlankLines()
                    .IgnoreComments()
                    .Trim()
                    .ToArray()
                    .ToRegex();
            }
        }


        static string GetPropertyName(Expression<Func<AzMeter, string>> propertySelector)
        {
            MemberExpression memberExpression = propertySelector.Body as MemberExpression;
            if (null == memberExpression) throw new Exception("Invalid property selector. Not a member expression.");

            PropertyInfo propInfo = memberExpression.Member as PropertyInfo;
            if (null == propInfo) throw new Exception("Invalid property selector. Not a Property expression.");

            return propInfo.Name;
        }

        #endregion

    };
}
