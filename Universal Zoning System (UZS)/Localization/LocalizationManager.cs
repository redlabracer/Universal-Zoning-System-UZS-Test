using System.Collections.Generic;
using Colossal;
using UniversalZoningSystem.Settings;

namespace UniversalZoningSystem.Localization
{
    /// <summary>
    /// Manages localization strings for the Universal Zoning System.
    /// </summary>
    public class LocalizationManager : IDictionarySource
    {
        private readonly ModSettings _settings;

        public LocalizationManager(ModSettings settings)
        {
            _settings = settings;
        }

        public IEnumerable<KeyValuePair<string, string>> ReadEntries(IList<IDictionaryEntryError> errors, Dictionary<string, int> indexCounts)
        {
            // Mod name and description
            yield return new KeyValuePair<string, string>("UniversalZoningSystem", "Universal Zoning System");
            yield return new KeyValuePair<string, string>("UniversalZoningSystem.DESCRIPTION", "Creates universal zone types that draw buildings from all regional content packs for truly diverse cityscapes.");

            // UI Category for Universal Zones (appears as tab in zone toolbar)
            yield return new KeyValuePair<string, string>("Assets.NAME[ZonesUniversal]", "Universal Zones");
            yield return new KeyValuePair<string, string>("Assets.DESCRIPTION[ZonesUniversal]", "Zone types that spawn buildings from all regions.");
            yield return new KeyValuePair<string, string>("SubServices.NAME[ZonesUniversal]", "Universal");

            // Zone prefab names (Assets.NAME[prefabName] pattern used by CS2)
            yield return new KeyValuePair<string, string>("Assets.NAME[UZS_LowResidential]", "Universal Low Residential");
            yield return new KeyValuePair<string, string>("Assets.DESCRIPTION[UZS_LowResidential]", "Single-family homes from all regions.");

            yield return new KeyValuePair<string, string>("Assets.NAME[UZS_RowResidential]", "Universal Row Residential");
            yield return new KeyValuePair<string, string>("Assets.DESCRIPTION[UZS_RowResidential]", "Townhouses and row homes worldwide.");

            yield return new KeyValuePair<string, string>("Assets.NAME[UZS_MediumResidential]", "Universal Medium Residential");
            yield return new KeyValuePair<string, string>("Assets.DESCRIPTION[UZS_MediumResidential]", "Mid-rise apartments globally.");

            yield return new KeyValuePair<string, string>("Assets.NAME[UZS_HighResidential]", "Universal High Residential");
            yield return new KeyValuePair<string, string>("Assets.DESCRIPTION[UZS_HighResidential]", "Skyscrapers and towers from every culture.");

            yield return new KeyValuePair<string, string>("Assets.NAME[UZS_LowRent]", "Universal Low Rent");
            yield return new KeyValuePair<string, string>("Assets.DESCRIPTION[UZS_LowRent]", "Affordable housing from all regions.");

            yield return new KeyValuePair<string, string>("Assets.NAME[UZS_MixedUse]", "Universal Mixed Use");
            yield return new KeyValuePair<string, string>("Assets.DESCRIPTION[UZS_MixedUse]", "Ground-floor retail with residential above.");

            yield return new KeyValuePair<string, string>("Assets.NAME[UZS_LowCommercial]", "Universal Low Commercial");
            yield return new KeyValuePair<string, string>("Assets.DESCRIPTION[UZS_LowCommercial]", "Small shops and boutiques from every region.");

            yield return new KeyValuePair<string, string>("Assets.NAME[UZS_HighCommercial]", "Universal High Commercial");
            yield return new KeyValuePair<string, string>("Assets.DESCRIPTION[UZS_HighCommercial]", "Major retail and department stores worldwide.");

            yield return new KeyValuePair<string, string>("Assets.NAME[UZS_LowOffice]", "Universal Low Office");
            yield return new KeyValuePair<string, string>("Assets.DESCRIPTION[UZS_LowOffice]", "Small professional buildings from all regions.");

            yield return new KeyValuePair<string, string>("Assets.NAME[UZS_HighOffice]", "Universal High Office");
            yield return new KeyValuePair<string, string>("Assets.DESCRIPTION[UZS_HighOffice]", "Corporate towers and business centers globally.");

            // Settings - Keys must match the format CS2 generates: "ModDisplayName.Namespace.TypeName.Property"
            // Based on assembly name "Universal Zoning System (UZS)" and the ModSettings class location
            yield return new KeyValuePair<string, string>("Options.SECTION[Universal Zoning System (UZS).UniversalZoningSystem.Mod]", "Universal Zoning System");

            yield return new KeyValuePair<string, string>("Options.GROUP[Universal Zoning System (UZS).UniversalZoningSystem.Mod.RegionSettings]", "Region Settings");
            yield return new KeyValuePair<string, string>("Options.GROUP[Universal Zoning System (UZS).UniversalZoningSystem.Mod.BuildingTypeSettings]", "Building Type Settings");
            yield return new KeyValuePair<string, string>("Options.GROUP[Universal Zoning System (UZS).UniversalZoningSystem.Mod.WeightingSettings]", "Weighting Settings");
            yield return new KeyValuePair<string, string>("Options.GROUP[Universal Zoning System (UZS).UniversalZoningSystem.Mod.AdvancedSettings]", "Advanced Settings");

            yield return new KeyValuePair<string, string>("Options.OPTION[Universal Zoning System (UZS).UniversalZoningSystem.Mod.ModSettings.EnableNorthAmerica]", "North America");
            yield return new KeyValuePair<string, string>("Options.OPTION_DESCRIPTION[Universal Zoning System (UZS).UniversalZoningSystem.Mod.ModSettings.EnableNorthAmerica]", "Include North American buildings");

            yield return new KeyValuePair<string, string>("Options.OPTION[Universal Zoning System (UZS).UniversalZoningSystem.Mod.ModSettings.EnableEuropean]", "European");
            yield return new KeyValuePair<string, string>("Options.OPTION_DESCRIPTION[Universal Zoning System (UZS).UniversalZoningSystem.Mod.ModSettings.EnableEuropean]", "Include European buildings");

            yield return new KeyValuePair<string, string>("Options.OPTION[Universal Zoning System (UZS).UniversalZoningSystem.Mod.ModSettings.EnableUnitedKingdom]", "United Kingdom");
            yield return new KeyValuePair<string, string>("Options.OPTION_DESCRIPTION[Universal Zoning System (UZS).UniversalZoningSystem.Mod.ModSettings.EnableUnitedKingdom]", "Include UK buildings");

            yield return new KeyValuePair<string, string>("Options.OPTION[Universal Zoning System (UZS).UniversalZoningSystem.Mod.ModSettings.EnableGermany]", "Germany");
            yield return new KeyValuePair<string, string>("Options.OPTION_DESCRIPTION[Universal Zoning System (UZS).UniversalZoningSystem.Mod.ModSettings.EnableGermany]", "Include German buildings");

            yield return new KeyValuePair<string, string>("Options.OPTION[Universal Zoning System (UZS).UniversalZoningSystem.Mod.ModSettings.EnableFrance]", "France");
            yield return new KeyValuePair<string, string>("Options.OPTION_DESCRIPTION[Universal Zoning System (UZS).UniversalZoningSystem.Mod.ModSettings.EnableFrance]", "Include French buildings");

            yield return new KeyValuePair<string, string>("Options.OPTION[Universal Zoning System (UZS).UniversalZoningSystem.Mod.ModSettings.EnableNetherlands]", "Netherlands");
            yield return new KeyValuePair<string, string>("Options.OPTION_DESCRIPTION[Universal Zoning System (UZS).UniversalZoningSystem.Mod.ModSettings.EnableNetherlands]", "Include Dutch buildings");

            yield return new KeyValuePair<string, string>("Options.OPTION[Universal Zoning System (UZS).UniversalZoningSystem.Mod.ModSettings.EnableEasternEurope]", "Eastern Europe");
            yield return new KeyValuePair<string, string>("Options.OPTION_DESCRIPTION[Universal Zoning System (UZS).UniversalZoningSystem.Mod.ModSettings.EnableEasternEurope]", "Include Eastern European buildings");

            yield return new KeyValuePair<string, string>("Options.OPTION[Universal Zoning System (UZS).UniversalZoningSystem.Mod.ModSettings.EnableJapan]", "Japan");
            yield return new KeyValuePair<string, string>("Options.OPTION_DESCRIPTION[Universal Zoning System (UZS).UniversalZoningSystem.Mod.ModSettings.EnableJapan]", "Include Japanese buildings");

            yield return new KeyValuePair<string, string>("Options.OPTION[Universal Zoning System (UZS).UniversalZoningSystem.Mod.ModSettings.EnableChina]", "China");
            yield return new KeyValuePair<string, string>("Options.OPTION_DESCRIPTION[Universal Zoning System (UZS).UniversalZoningSystem.Mod.ModSettings.EnableChina]", "Include Chinese buildings");

            // Building Type Settings
            yield return new KeyValuePair<string, string>("Options.OPTION[Universal Zoning System (UZS).UniversalZoningSystem.Mod.ModSettings.EnableDetachedHouses]", "Detached Houses");
            yield return new KeyValuePair<string, string>("Options.OPTION_DESCRIPTION[Universal Zoning System (UZS).UniversalZoningSystem.Mod.ModSettings.EnableDetachedHouses]", "Single-family detached homes");

            yield return new KeyValuePair<string, string>("Options.OPTION[Universal Zoning System (UZS).UniversalZoningSystem.Mod.ModSettings.EnableAttachedHouses]", "Attached/Row Houses");
            yield return new KeyValuePair<string, string>("Options.OPTION_DESCRIPTION[Universal Zoning System (UZS).UniversalZoningSystem.Mod.ModSettings.EnableAttachedHouses]", "Terraced and row houses");

            yield return new KeyValuePair<string, string>("Options.OPTION[Universal Zoning System (UZS).UniversalZoningSystem.Mod.ModSettings.EnableApartments]", "Apartments");
            yield return new KeyValuePair<string, string>("Options.OPTION_DESCRIPTION[Universal Zoning System (UZS).UniversalZoningSystem.Mod.ModSettings.EnableApartments]", "Medium-density apartment buildings");

            yield return new KeyValuePair<string, string>("Options.OPTION[Universal Zoning System (UZS).UniversalZoningSystem.Mod.ModSettings.EnableHighRise]", "High-Rise Towers");
            yield return new KeyValuePair<string, string>("Options.OPTION_DESCRIPTION[Universal Zoning System (UZS).UniversalZoningSystem.Mod.ModSettings.EnableHighRise]", "Tall residential and office towers");

            yield return new KeyValuePair<string, string>("Options.OPTION[Universal Zoning System (UZS).UniversalZoningSystem.Mod.ModSettings.UseEqualRegionWeighting]", "Equal Region Weighting");
            yield return new KeyValuePair<string, string>("Options.OPTION_DESCRIPTION[Universal Zoning System (UZS).UniversalZoningSystem.Mod.ModSettings.UseEqualRegionWeighting]", "Give each region equal chance");

            yield return new KeyValuePair<string, string>("Options.OPTION[Universal Zoning System (UZS).UniversalZoningSystem.Mod.ModSettings.EnableVerboseLogging]", "Verbose Logging");
            yield return new KeyValuePair<string, string>("Options.OPTION_DESCRIPTION[Universal Zoning System (UZS).UniversalZoningSystem.Mod.ModSettings.EnableVerboseLogging]", "Enable detailed logging");

            yield return new KeyValuePair<string, string>("Options.OPTION[Universal Zoning System (UZS).UniversalZoningSystem.Mod.ModSettings.ResetToDefaults]", "Reset to Defaults");
            yield return new KeyValuePair<string, string>("Options.OPTION_DESCRIPTION[Universal Zoning System (UZS).UniversalZoningSystem.Mod.ModSettings.ResetToDefaults]", "Reset all settings");

            // UZS District Policies
            // Try multiple key formats to ensure localization works
            
            // Format 1: Assets.NAME (standard asset format)
            yield return new KeyValuePair<string, string>("Assets.NAME[UZS No Attached Houses]", "UZS: No Attached Houses");
            yield return new KeyValuePair<string, string>("Assets.DESCRIPTION[UZS No Attached Houses]", "Prevents row houses and terraced homes from spawning in Universal Zones");
            yield return new KeyValuePair<string, string>("Assets.NAME[UZS No High-Rise]", "UZS: No High-Rise");
            yield return new KeyValuePair<string, string>("Assets.DESCRIPTION[UZS No High-Rise]", "Prevents tall towers from spawning in Universal Zones");
            yield return new KeyValuePair<string, string>("Assets.NAME[UZS Detached Only]", "UZS: Detached Houses Only");
            yield return new KeyValuePair<string, string>("Assets.DESCRIPTION[UZS Detached Only]", "Only single-family detached houses allowed in Universal Zones");
            yield return new KeyValuePair<string, string>("Assets.NAME[UZS European Only]", "UZS: European Style Only");
            yield return new KeyValuePair<string, string>("Assets.DESCRIPTION[UZS European Only]", "Only European architecture allowed in Universal Zones");
            yield return new KeyValuePair<string, string>("Assets.NAME[UZS North America Only]", "UZS: North American Style Only");
            yield return new KeyValuePair<string, string>("Assets.DESCRIPTION[UZS North America Only]", "Only North American architecture allowed in Universal Zones");

            // Format 2: Policies.POLICY format (may be used by district policies)
            yield return new KeyValuePair<string, string>("Policies.POLICY[UZS No Attached Houses]", "UZS: No Attached Houses");
            yield return new KeyValuePair<string, string>("Policies.POLICY_DESCRIPTION[UZS No Attached Houses]", "Prevents row houses and terraced homes from spawning");
            yield return new KeyValuePair<string, string>("Policies.POLICY[UZS No High-Rise]", "UZS: No High-Rise");
            yield return new KeyValuePair<string, string>("Policies.POLICY_DESCRIPTION[UZS No High-Rise]", "Prevents tall towers from spawning");
            yield return new KeyValuePair<string, string>("Policies.POLICY[UZS Detached Only]", "UZS: Detached Only");
            yield return new KeyValuePair<string, string>("Policies.POLICY_DESCRIPTION[UZS Detached Only]", "Only detached houses allowed");
            yield return new KeyValuePair<string, string>("Policies.POLICY[UZS European Only]", "UZS: European Only");
            yield return new KeyValuePair<string, string>("Policies.POLICY_DESCRIPTION[UZS European Only]", "Only European architecture");
            yield return new KeyValuePair<string, string>("Policies.POLICY[UZS North America Only]", "UZS: NA Only");
            yield return new KeyValuePair<string, string>("Policies.POLICY_DESCRIPTION[UZS North America Only]", "Only North American architecture");

            // Format 3: Policies.NAME format (alternative)
            yield return new KeyValuePair<string, string>("Policies.NAME[UZS No Attached Houses]", "UZS: No Attached Houses");
            yield return new KeyValuePair<string, string>("Policies.NAME[UZS No High-Rise]", "UZS: No High-Rise");
            yield return new KeyValuePair<string, string>("Policies.NAME[UZS Detached Only]", "UZS: Detached Only");
            yield return new KeyValuePair<string, string>("Policies.NAME[UZS European Only]", "UZS: European Only");
            yield return new KeyValuePair<string, string>("Policies.NAME[UZS North America Only]", "UZS: NA Only");

            // Format 4: SelectedInfoPanel format (used by info panels)
            yield return new KeyValuePair<string, string>("SelectedInfoPanel.POLICY_TITLE[UZS No Attached Houses]", "UZS: No Attached Houses");
            yield return new KeyValuePair<string, string>("SelectedInfoPanel.POLICY_TITLE[UZS No High-Rise]", "UZS: No High-Rise");
            yield return new KeyValuePair<string, string>("SelectedInfoPanel.POLICY_TITLE[UZS Detached Only]", "UZS: Detached Only");
            yield return new KeyValuePair<string, string>("SelectedInfoPanel.POLICY_TITLE[UZS European Only]", "UZS: European Only");
            yield return new KeyValuePair<string, string>("SelectedInfoPanel.POLICY_TITLE[UZS North America Only]", "UZS: NA Only");
        }

        public void Unload()
        {
        }
    }
}
