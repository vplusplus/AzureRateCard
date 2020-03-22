using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace UnitTests
{
    internal static class K
    {
        public const int HoursPerMonth = 365 * 24 / 12;

        public const string RawDataFolder = "../../../RawData";
        public const string DataFolder = "../../../Data";

        public static string RateCardFileName => Path.Combine(RawDataFolder, "RateCard.json");
        public static string ComputeResourcesSkusFileName => Path.Combine(RawDataFolder, "ComputeResourcesSkus.json");

    }
}
