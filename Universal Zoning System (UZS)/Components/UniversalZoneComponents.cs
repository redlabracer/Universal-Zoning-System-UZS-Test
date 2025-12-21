using Unity.Entities;

namespace UniversalZoningSystem.Components
{
    /// <summary>
    /// Component attached to District entities to store UZS settings.
    /// </summary>
    public struct UniversalZoneDistrictSettings : IComponentData
    {
        public bool EnableNorthAmerica;
        public bool EnableEuropean;
        public bool EnableUnitedKingdom;
        public bool EnableGermany;
        public bool EnableFrance;
        public bool EnableNetherlands;
        public bool EnableEasternEurope;
        public bool EnableJapan;
        public bool EnableChina;

        public bool EnableDetached;
        public bool EnableAttached;
        public bool EnableMixed;
        
        // Default constructor sets everything to true
        public static UniversalZoneDistrictSettings Default()
        {
            return new UniversalZoneDistrictSettings
            {
                EnableNorthAmerica = true,
                EnableEuropean = true,
                EnableUnitedKingdom = true,
                EnableGermany = true,
                EnableFrance = true,
                EnableNetherlands = true,
                EnableEasternEurope = true,
                EnableJapan = true,
                EnableChina = true,
                EnableDetached = true,
                EnableAttached = true,
        EnableMixed = true
            };
        }
    }

    public struct UniversalBuildingData : IComponentData
    {
        public RegionType Region;
        public BuildingStyleType Style;
    }

    public enum RegionType
    {
        Unknown = 0,
        NorthAmerica,
        European,
        UnitedKingdom,
        Germany,
        France,
        Netherlands,
        EasternEurope,
        Japan,
        China
    }

    public enum BuildingStyleType
    {
        Unknown = 0,
        Detached,
        Attached,
        HighRise,
        Commercial,
        Office,
        Mixed
    }

    /// <summary>
    /// Marker for buildings already validated against district policies.
    /// </summary>
    public struct UniversalZoneChecked : IComponentData
    {
    }

    /// <summary>
    /// Per-district building restrictions integrated with the vanilla policy panel.
    /// </summary>
    public struct UZSDistrictPolicies : IComponentData
    {
        public bool NoAttachedHouses;
        public bool NoHighRise;
        public bool DetachedOnly;
        public bool EuropeanOnly;
        public bool NorthAmericaOnly;

        public static UZSDistrictPolicies Default()
        {
            return new UZSDistrictPolicies
            {
                NoAttachedHouses = false,
                NoHighRise = false,
                DetachedOnly = false,
                EuropeanOnly = false,
                NorthAmericaOnly = false
            };
        }
    }
}
