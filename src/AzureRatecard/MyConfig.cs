
using System;

namespace AzureRateCard
{
    class MyConfig
    {
        internal static string RateCardTenantId => GetAppSettings("RateCard.TenantId");
        internal static string RateCardClientId => GetAppSettings("RateCard.ClientId");
        internal static string RateCardClientCertThumbprint => GetAppSettings("RateCard.ClientCertThumbprint");

        static string GetAppSettings(string name)
        {
            var value = System.Configuration.ConfigurationManager.AppSettings.Get(name);
            return string.IsNullOrEmpty(value)
                ? throw new Exception($"AppSettings missing config entry: {name}")
                : value;
        }
    }
}
