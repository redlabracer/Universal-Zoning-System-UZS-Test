using System.Collections.Generic;

namespace UniversalZoningSystem.CustomZones
{
    public class CustomZoneDefinition
    {
        public const string IdPrefix = "UZSC_";

        public string Id { get; set; }

        public string Name { get; set; }

        public ZoneType BaseZoneType { get; set; }

        public List<string> SourceZones { get; set; } = new List<string>();
    }
}
