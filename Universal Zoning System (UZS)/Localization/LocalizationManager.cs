using System.Linq;
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

            yield return new KeyValuePair<string, string>("Options.OPTION[Universal Zoning System (UZS).UniversalZoningSystem.Mod.ModSettings.UseEqualRegionWeighting]", "Equal Region Weighting");
            yield return new KeyValuePair<string, string>("Options.OPTION_DESCRIPTION[Universal Zoning System (UZS).UniversalZoningSystem.Mod.ModSettings.UseEqualRegionWeighting]", "Give each region equal chance");

            yield return new KeyValuePair<string, string>("Options.OPTION[Universal Zoning System (UZS).UniversalZoningSystem.Mod.ModSettings.EnableVerboseLogging]", "Verbose Logging");
            yield return new KeyValuePair<string, string>("Options.OPTION_DESCRIPTION[Universal Zoning System (UZS).UniversalZoningSystem.Mod.ModSettings.EnableVerboseLogging]", "Enable detailed logging");

            yield return new KeyValuePair<string, string>("Options.OPTION[Universal Zoning System (UZS).UniversalZoningSystem.Mod.ModSettings.ResetToDefaults]", "Reset to Defaults");
            yield return new KeyValuePair<string, string>("Options.OPTION_DESCRIPTION[Universal Zoning System (UZS).UniversalZoningSystem.Mod.ModSettings.ResetToDefaults]", "Reset all settings");

            // Custom Zone Builder UI
            yield return new KeyValuePair<string, string>("Options.GROUP[Universal Zoning System (UZS).UniversalZoningSystem.Mod.PanelSettings]", "Custom Zone Builder");
            yield return new KeyValuePair<string, string>("Options.OPTION[Universal Zoning System (UZS).UniversalZoningSystem.Mod.ModSettings.ButtonLocation]", "Button Location");
            yield return new KeyValuePair<string, string>("Options.OPTION_DESCRIPTION[Universal Zoning System (UZS).UniversalZoningSystem.Mod.ModSettings.ButtonLocation]", "Where the Custom Zone Builder button is shown.");
            yield return new KeyValuePair<string, string>("Options.Universal Zoning System (UZS).UniversalZoningSystem.Mod.BUTTONLOCATIONOPTION[UniversalModMenu]", "Universal Mod Menu");
            yield return new KeyValuePair<string, string>("Options.Universal Zoning System (UZS).UniversalZoningSystem.Mod.BUTTONLOCATIONOPTION[TopLeft]", "Top Left");
            yield return new KeyValuePair<string, string>("Options.Universal Zoning System (UZS).UniversalZoningSystem.Mod.BUTTONLOCATIONOPTION[TopRight]", "Top Right");
            yield return new KeyValuePair<string, string>("UniversalZoning.CustomZones.BUTTON_TOOLTIP", "Open the Custom Zone Builder to combine existing zones into your own zone.");
            yield return new KeyValuePair<string, string>("UniversalZoning.CustomZones.TITLE", "UZS Zone Composer");
            yield return new KeyValuePair<string, string>("UniversalZoning.CustomZones.MENU_BUTTON", "Custom Zones");
            yield return new KeyValuePair<string, string>("UniversalZoning.CustomZones.NEW_ZONE", "New Zone");
            yield return new KeyValuePair<string, string>("UniversalZoning.CustomZones.ZONE", "Custom Zone");
            yield return new KeyValuePair<string, string>("UniversalZoning.CustomZones.NAME", "Name");
            yield return new KeyValuePair<string, string>("UniversalZoning.CustomZones.BASE_TYPE", "Zone Type");
            yield return new KeyValuePair<string, string>("UniversalZoning.CustomZones.SOURCES", "Source Zones");
            yield return new KeyValuePair<string, string>("UniversalZoning.CustomZones.BUILDINGS", "Buildings");
            yield return new KeyValuePair<string, string>("UniversalZoning.CustomZones.SAVE", "Save");
            yield return new KeyValuePair<string, string>("UniversalZoning.CustomZones.DELETE", "Delete");
            yield return new KeyValuePair<string, string>("UniversalZoning.CustomZones.EMPTY", "No custom zones yet. Create one to combine existing zones.");
            yield return new KeyValuePair<string, string>("UniversalZoning.CustomZones.RESTART_REQUIRED", "Some changes (removed zones or sources) take full effect after restarting the game.");

            // Custom zone prefab names
            foreach (var custom in CustomZones.CustomZoneRegistry.Zones)
            {
                yield return new KeyValuePair<string, string>($"Assets.NAME[{custom.Id}]", custom.Name);
                yield return new KeyValuePair<string, string>($"Assets.DESCRIPTION[{custom.Id}]", DescribeCustomZone(custom));
            }
        }

        private static string DescribeCustomZone(CustomZones.CustomZoneDefinition custom)
        {
            var dictionary = Game.SceneFlow.GameManager.instance?.localizationManager?.activeDictionary;
            var names = custom.SourceZones.Select(zone =>
                dictionary != null && dictionary.TryGetValue($"Assets.NAME[{zone}]", out var name) && !string.IsNullOrEmpty(name) ? name : zone);
            return "Custom zone combining:\n" + string.Join("\n", names.Select(n => "• " + n));
        }

        public void Unload()
        {
        }
    }
}
