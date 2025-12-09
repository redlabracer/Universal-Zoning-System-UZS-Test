using System.Collections.Generic;

namespace UniversalZoningSystem
{
    /// <summary>
    /// Manages regional building prefixes and categorization.
    /// Identifies buildings from different content packs based on naming conventions.
    /// </summary>
    public static class RegionPrefixManager
    {
        /// <summary>
        /// Known region prefixes used in Cities: Skylines II building prefabs.
        /// </summary>
        public static readonly Dictionary<string, RegionInfo> KnownPrefixes = new Dictionary<string, RegionInfo>
        {
            // North America
            { "NA_", new RegionInfo("NA_", "North America", "Standard NA", "????") },
            { "USNE_", new RegionInfo("USNE_", "US Northeast", "Boston brownstones, New York apartments, Philadelphia row homes", "????") },
            { "USSW_", new RegionInfo("USSW_", "US Southwest", "Desert-adapted architecture, Spanish colonial influences", "????") },

            // European
            { "EU_", new RegionInfo("EU_", "European", "Core European apartment blocks, mixed-use buildings", "????") },
            { "UK_", new RegionInfo("UK_", "United Kingdom", "Victorian terraces, Georgian townhouses, British high streets", "????") },
            { "GER_", new RegionInfo("GER_", "Germany", "Bauhaus influences, German apartment blocks", "????") },
            { "DE_", new RegionInfo("DE_", "Germany", "German traditional and modern styles", "????") },
            { "FR_", new RegionInfo("FR_", "France", "Parisian Haussmann buildings, French provincial styles", "????") },
            { "NL_", new RegionInfo("NL_", "Netherlands", "Dutch canal houses, modern Amsterdam architecture", "????") },
            { "EE_", new RegionInfo("EE_", "Eastern Europe", "Post-war blocks, traditional Eastern European styles", "????") },

            // Asian
            { "JP_", new RegionInfo("JP_", "Japan", "Dense Tokyo apartments, traditional Japanese elements", "????") },
            { "CN_", new RegionInfo("CN_", "China", "High-density residential, Chinese commercial", "????") },
            { "KR_", new RegionInfo("KR_", "Korea", "Korean residential and commercial buildings", "????") },

            // Other potential regions
            { "AU_", new RegionInfo("AU_", "Australia", "Australian suburban and urban architecture", "????") },
            { "BR_", new RegionInfo("BR_", "Brazil", "Brazilian urban architecture", "????") },
            { "MX_", new RegionInfo("MX_", "Mexico", "Mexican architectural styles", "????") },
        };

        /// <summary>
        /// Alternate prefixes that map to the same region.
        /// </summary>
        public static readonly Dictionary<string, string> AlternatePrefixes = new Dictionary<string, string>
        {
            { "DE_", "GER_" },
            { "EURO_", "EU_" },
            { "EUR_", "EU_" },
            { "US_", "NA_" },
            { "JAP_", "JP_" },
            { "CHN_", "CN_" },
        };

        /// <summary>
        /// Gets the region identifier from a prefab name.
        /// </summary>
        /// <param name="prefabName">The name of the building prefab.</param>
        /// <returns>The region name or "Generic" if no known prefix is found.</returns>
        public static string GetRegionFromPrefabName(string prefabName)
        {
            if (string.IsNullOrEmpty(prefabName))
                return "Generic";

            var upperName = prefabName.ToUpperInvariant();

            foreach (var prefix in KnownPrefixes.Keys)
            {
                if (upperName.StartsWith(prefix))
                {
                    return KnownPrefixes[prefix].RegionName;
                }
            }

            // Check alternate prefixes
            foreach (var altPrefix in AlternatePrefixes.Keys)
            {
                if (upperName.StartsWith(altPrefix))
                {
                    var mainPrefix = AlternatePrefixes[altPrefix];
                    if (KnownPrefixes.TryGetValue(mainPrefix, out var info))
                    {
                        return info.RegionName;
                    }
                }
            }

            return "Generic";
        }

        /// <summary>
        /// Gets detailed region information from a prefab name.
        /// </summary>
        public static RegionInfo GetRegionInfo(string prefabName)
        {
            if (string.IsNullOrEmpty(prefabName))
                return RegionInfo.Generic;

            var upperName = prefabName.ToUpperInvariant();

            foreach (var prefix in KnownPrefixes.Keys)
            {
                if (upperName.StartsWith(prefix))
                {
                    return KnownPrefixes[prefix];
                }
            }

            return RegionInfo.Generic;
        }

        /// <summary>
        /// Checks if a prefab belongs to a specific region.
        /// </summary>
        public static bool IsFromRegion(string prefabName, string regionPrefix)
        {
            if (string.IsNullOrEmpty(prefabName) || string.IsNullOrEmpty(regionPrefix))
                return false;

            return prefabName.ToUpperInvariant().StartsWith(regionPrefix.ToUpperInvariant());
        }

        /// <summary>
        /// Gets all prefabs that match any of the given region prefixes.
        /// </summary>
        public static bool MatchesAnyRegion(string prefabName, params string[] regionPrefixes)
        {
            if (string.IsNullOrEmpty(prefabName) || regionPrefixes == null || regionPrefixes.Length == 0)
                return false;

            var upperName = prefabName.ToUpperInvariant();

            foreach (var prefix in regionPrefixes)
            {
                if (upperName.StartsWith(prefix.ToUpperInvariant()))
                    return true;
            }

            return false;
        }
    }

    /// <summary>
    /// Information about a regional content pack.
    /// </summary>
    public class RegionInfo
    {
        public string Prefix { get; }
        public string RegionName { get; }
        public string Description { get; }
        public string Flag { get; }

        public RegionInfo(string prefix, string regionName, string description, string flag)
        {
            Prefix = prefix;
            RegionName = regionName;
            Description = description;
            Flag = flag;
        }

        public static readonly RegionInfo Generic = new RegionInfo("", "Generic", "Generic buildings without regional designation", "??");
    }
}
