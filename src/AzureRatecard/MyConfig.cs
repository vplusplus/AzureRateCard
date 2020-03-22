
using System;

namespace AzureRateCard
{
    class MyConfig
    {
        internal static string MyTenantId => GetAppSettings("RateCard.TenantId");
        internal static string MyClientId => GetAppSettings("RateCard.ClientId");
        internal static string MyUserId => GetAppSettings("RateCard.UserId");
        internal static string MyPassword => GetAppSettings("RateCard.Password");

        static string GetAppSettings(string name)
        {
            var value = System.Configuration.ConfigurationManager.AppSettings.Get(name);
            return string.IsNullOrEmpty(value)
                ? throw new Exception($"AppSettings missing config entry: {name}")
                : value;
        }

        static string GetAppSettings(string name, string defaultValue)
        {
            var value = System.Configuration.ConfigurationManager.AppSettings.Get(name);
            return string.IsNullOrEmpty(value)
                ? defaultValue
                : value;
        }
    }
}
