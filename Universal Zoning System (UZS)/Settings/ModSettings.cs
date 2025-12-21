using System.Collections.Generic;
using Colossal.IO.AssetDatabase;
using Game.Modding;
using Game.Settings;

namespace UniversalZoningSystem.Settings
{
    /// <summary>
    /// Mod settings for the Universal Zoning System.
    /// </summary>
    [FileLocation(nameof(UniversalZoningSystem))]
    [SettingsUIGroupOrder(RegionSettingsGroup, BuildingTypeSettingsGroup, WeightingSettingsGroup, AdvancedSettingsGroup)]
    [SettingsUIShowGroupName(RegionSettingsGroup, BuildingTypeSettingsGroup, WeightingSettingsGroup, AdvancedSettingsGroup)]
    public class ModSettings : ModSetting
    {
        public const string MainSection = "Main";
        public const string RegionSettingsGroup = "RegionSettings";
        public const string BuildingTypeSettingsGroup = "BuildingTypeSettings";
        public const string WeightingSettingsGroup = "WeightingSettings";
        public const string AdvancedSettingsGroup = "AdvancedSettings";

        public ModSettings(IMod mod) : base(mod)
        {
            SetDefaults();
        }


        #region Region Settings

        [SettingsUISection(MainSection, RegionSettingsGroup)]
        public bool EnableNorthAmerica { get; set; } = true;

        [SettingsUISection(MainSection, RegionSettingsGroup)]
        public bool EnableEuropean { get; set; } = true;

        [SettingsUISection(MainSection, RegionSettingsGroup)]
        public bool EnableUnitedKingdom { get; set; } = true;

        [SettingsUISection(MainSection, RegionSettingsGroup)]
        public bool EnableGermany { get; set; } = true;

        [SettingsUISection(MainSection, RegionSettingsGroup)]
        public bool EnableFrance { get; set; } = true;

        [SettingsUISection(MainSection, RegionSettingsGroup)]
        public bool EnableNetherlands { get; set; } = true;

        [SettingsUISection(MainSection, RegionSettingsGroup)]
        public bool EnableEasternEurope { get; set; } = true;

        [SettingsUISection(MainSection, RegionSettingsGroup)]
        public bool EnableJapan { get; set; } = true;

        [SettingsUISection(MainSection, RegionSettingsGroup)]
        public bool EnableChina { get; set; } = true;

        #endregion

        #region Building Type Settings

        [SettingsUISection(MainSection, BuildingTypeSettingsGroup)]
        public bool EnableDetachedHouses { get; set; } = true;

        [SettingsUISection(MainSection, BuildingTypeSettingsGroup)]
        public bool EnableAttachedHouses { get; set; } = true;

        [SettingsUISection(MainSection, BuildingTypeSettingsGroup)]
        public bool EnableApartments { get; set; } = true;

        [SettingsUISection(MainSection, BuildingTypeSettingsGroup)]
        public bool EnableHighRise { get; set; } = true;

        #endregion

        #region Weighting Settings

        [SettingsUISection(MainSection, WeightingSettingsGroup)]
        public bool UseEqualRegionWeighting { get; set; } = true;

        #endregion

        #region Advanced Settings

        [SettingsUISection(MainSection, AdvancedSettingsGroup)]
        public bool EnableVerboseLogging { get; set; } = false;

        [SettingsUISection(MainSection, AdvancedSettingsGroup)]
        [SettingsUIButton]
        public bool ResetToDefaults
        {
            set
            {
                SetDefaults();
                ApplyAndSave();
            }
        }

        #endregion

        public override void SetDefaults()
        {
            EnableNorthAmerica = true;
            EnableEuropean = true;
            EnableUnitedKingdom = true;
            EnableGermany = true;
            EnableFrance = true;
            EnableNetherlands = true;
            EnableEasternEurope = true;
            EnableJapan = true;
            EnableChina = true;
            EnableDetachedHouses = true;
            EnableAttachedHouses = true;
            EnableApartments = true;
            EnableHighRise = true;
            UseEqualRegionWeighting = true;
            EnableVerboseLogging = false;
        }

        public List<string> GetEnabledRegionPrefixes()
        {
            var prefixes = new List<string>();

            if (EnableNorthAmerica)
            {
                prefixes.Add("NA_");
                prefixes.Add("USNE_");
                prefixes.Add("USSW_");
            }

            if (EnableEuropean)
            {
                prefixes.Add("EU_");
            }

            if (EnableUnitedKingdom)
            {
                prefixes.Add("UK_");
            }

            if (EnableGermany)
            {
                prefixes.Add("GER_");
                prefixes.Add("DE_");
            }

            if (EnableFrance)
            {
                prefixes.Add("FR_");
            }

            if (EnableNetherlands)
            {
                prefixes.Add("NL_");
            }

            if (EnableEasternEurope)
            {
                prefixes.Add("EE_");
            }

            if (EnableJapan)
            {
                prefixes.Add("JP_");
            }

            if (EnableChina)
            {
                prefixes.Add("CN_");
            }

            return prefixes;
        }

        /// <summary>
        /// Checks if a region prefix is enabled.
        /// </summary>
        public bool IsRegionEnabled(string prefix)
        {
            var upperPrefix = prefix.ToUpperInvariant();

            switch (upperPrefix)
            {
                case "NA_":
                case "USNE_":
                case "USSW_":
                    return EnableNorthAmerica;
                case "EU_":
                    return EnableEuropean;
                case "UK_":
                    return EnableUnitedKingdom;
                case "GER_":
                case "DE_":
                    return EnableGermany;
                case "FR_":
                    return EnableFrance;
                case "NL_":
                    return EnableNetherlands;
                case "EE_":
                    return EnableEasternEurope;
                case "JP_":
                    return EnableJapan;
                case "CN_":
                    return EnableChina;
                default:
                    return true; // Unknown prefixes are enabled by default
            }
        }
    }
}
