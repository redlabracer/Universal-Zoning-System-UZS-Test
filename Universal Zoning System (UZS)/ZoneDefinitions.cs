using System.Collections.Generic;

namespace UniversalZoningSystem
{
    /// <summary>
    /// Defines the zone types used for classification.
    /// </summary>
    public enum ZoneType
    {
        None,
        ResidentialLow,
        ResidentialRow,
        ResidentialMedium,
        ResidentialHigh,
        ResidentialLowRent,
        ResidentialMixed,
        CommercialLow,
        CommercialHigh,
        OfficeLow,
        OfficeHigh,
        Office
    }

    /// <summary>
    /// Definition for a universal zone type.
    /// </summary>
    public class UniversalZoneDefinition
    {
        public string Id { get; }
        public string Name { get; }
        public string Description { get; }
        public string Category { get; }
        public ZoneType[] SourceZoneTypes { get; }
        public string IconPath { get; }

        public UniversalZoneDefinition(string id, string name, string description, string category, ZoneType[] sourceZoneTypes, string iconPath = null)
        {
            Id = id;
            Name = name;
            Description = description;
            Category = category;
            SourceZoneTypes = sourceZoneTypes;
            IconPath = iconPath;
        }
    }

    /// <summary>
    /// Static definitions for all 10 universal zone types.
    /// </summary>
    public static class ZoneDefinitions
    {
        public static readonly UniversalZoneDefinition UniversalLowResidential = new UniversalZoneDefinition(
            "UZS_LowResidential",
            "Universal Low Residential",
            "Single-family homes from all regions - American suburbs, European villas, Japanese houses, and more.",
            "Residential",
            new[] { ZoneType.ResidentialLow }
        );

        public static readonly UniversalZoneDefinition UniversalRowResidential = new UniversalZoneDefinition(
            "UZS_RowResidential",
            "Universal Row Residential",
            "Townhouses and row homes worldwide - Boston brownstones, London terraces, Dutch canal houses.",
            "Residential",
            new[] { ZoneType.ResidentialRow }
        );

        public static readonly UniversalZoneDefinition UniversalMediumResidential = new UniversalZoneDefinition(
            "UZS_MediumResidential",
            "Universal Medium Residential",
            "Mid-rise apartments globally - Parisian Haussmann buildings, Berlin blocks, Tokyo apartments.",
            "Residential",
            new[] { ZoneType.ResidentialMedium }
        );

        public static readonly UniversalZoneDefinition UniversalHighResidential = new UniversalZoneDefinition(
            "UZS_HighResidential",
            "Universal High Residential",
            "Skyscrapers and towers from every culture - Manhattan high-rises, Hong Kong towers, Dubai residences.",
            "Residential",
            new[] { ZoneType.ResidentialHigh }
        );

        public static readonly UniversalZoneDefinition UniversalLowRent = new UniversalZoneDefinition(
            "UZS_LowRent",
            "Universal Low Rent",
            "Affordable housing from all regions - Social housing, public apartments, subsidized units.",
            "Residential",
            new[] { ZoneType.ResidentialLowRent }
        );

        public static readonly UniversalZoneDefinition UniversalMixedUse = new UniversalZoneDefinition(
            "UZS_MixedUse",
            "Universal Mixed Use",
            "Ground-floor retail with residential above - Global mixed-use developments.",
            "Residential",
            new[] { ZoneType.ResidentialMixed }
        );

        public static readonly UniversalZoneDefinition UniversalLowCommercial = new UniversalZoneDefinition(
            "UZS_LowCommercial",
            "Universal Low Commercial",
            "Small shops and boutiques - Corner stores, cafes, local businesses from around the world.",
            "Commercial",
            new[] { ZoneType.CommercialLow }
        );

        public static readonly UniversalZoneDefinition UniversalHighCommercial = new UniversalZoneDefinition(
            "UZS_HighCommercial",
            "Universal High Commercial",
            "Major retail and department stores - Shopping centers, flagship stores, commercial complexes.",
            "Commercial",
            new[] { ZoneType.CommercialHigh }
        );

        public static readonly UniversalZoneDefinition UniversalLowOffice = new UniversalZoneDefinition(
            "UZS_LowOffice",
            "Universal Low Office",
            "Small professional buildings - Local offices, clinics, small business headquarters.",
            "Office",
            new[] { ZoneType.OfficeLow }
        );

        public static readonly UniversalZoneDefinition UniversalHighOffice = new UniversalZoneDefinition(
            "UZS_HighOffice",
            "Universal High Office",
            "Corporate towers and business centers - Global headquarters, financial district buildings.",
            "Office",
            new[] { ZoneType.OfficeHigh }
        );

        /// <summary>
        /// All universal zone definitions.
        /// </summary>
        public static readonly UniversalZoneDefinition[] AllZones = new[]
        {
            UniversalLowResidential,
            UniversalRowResidential,
            UniversalMediumResidential,
            UniversalHighResidential,
            UniversalLowRent,
            UniversalMixedUse,
            UniversalLowCommercial,
            UniversalHighCommercial,
            UniversalLowOffice,
            UniversalHighOffice
        };

        /// <summary>
        /// Get a zone definition by its ID.
        /// </summary>
        public static UniversalZoneDefinition GetById(string id)
        {
            foreach (var zone in AllZones)
            {
                if (zone.Id == id)
                    return zone;
            }
            return null;
        }
    }
}
