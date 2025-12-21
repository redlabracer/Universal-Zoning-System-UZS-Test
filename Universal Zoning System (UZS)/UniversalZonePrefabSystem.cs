using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Colossal.Logging;
using Game;
using Game.Prefabs;
using Game.Zones;
using Unity.Collections;
using Unity.Entities;

namespace UniversalZoningSystem
{
    /// <summary>
    /// System responsible for creating and registering universal zone prefabs.
    /// This system modifies existing zone prefabs to include buildings from all regions.
    /// </summary>
    public partial class UniversalZonePrefabSystem : GameSystemBase
    {
        private static readonly ILog Log = Mod.Log;

        private PrefabSystem _prefabSystem;
        private EntityQuery _zoneQuery;
        private EntityQuery _buildingQuery;
        private bool _initialized;
        private int _frameDelay;

        // Cache of original zone prefabs by name
        private readonly Dictionary<string, ZonePrefab> _zoneTemplates = new Dictionary<string, ZonePrefab>();
        
        // Cache of zone prefabs by our zone type classification
        private readonly Dictionary<ZoneType, List<ZonePrefab>> _zonesByType = new Dictionary<ZoneType, List<ZonePrefab>>();
        
        // Buildings collected per zone type across all regions
        private readonly Dictionary<ZoneType, List<BuildingPrefab>> _buildingsByZoneType = new Dictionary<ZoneType, List<BuildingPrefab>>();

        // Map of zone names to their classifications (expanded for all known zone variants)
        private static readonly Dictionary<string, ZoneClassification> ZoneNameMappings = new Dictionary<string, ZoneClassification>(StringComparer.OrdinalIgnoreCase)
        {
            // Residential Low
            { "EU Residential Low", new ZoneClassification(ZoneType.ResidentialLow, AreaType.Residential, false) },
            { "NA Residential Low", new ZoneClassification(ZoneType.ResidentialLow, AreaType.Residential, false) },
            { "Residential Low", new ZoneClassification(ZoneType.ResidentialLow, AreaType.Residential, false) },
            { "JP Residential Low", new ZoneClassification(ZoneType.ResidentialLow, AreaType.Residential, false) },
            { "CN Residential Low", new ZoneClassification(ZoneType.ResidentialLow, AreaType.Residential, false) },
            { "EE Residential Low", new ZoneClassification(ZoneType.ResidentialLow, AreaType.Residential, false) },

            // Residential Row (Medium Row in CS2 naming)
            { "EU Residential Medium Row", new ZoneClassification(ZoneType.ResidentialRow, AreaType.Residential, true) },
            { "NA Residential Medium Row", new ZoneClassification(ZoneType.ResidentialRow, AreaType.Residential, true) },
            { "Residential Medium Row", new ZoneClassification(ZoneType.ResidentialRow, AreaType.Residential, true) },
            { "UK Residential Medium Row", new ZoneClassification(ZoneType.ResidentialRow, AreaType.Residential, true) },
            { "EE Residential Medium Row", new ZoneClassification(ZoneType.ResidentialRow, AreaType.Residential, true) },

            // Residential Medium (non-row)
            { "EU Residential Medium", new ZoneClassification(ZoneType.ResidentialMedium, AreaType.Residential, false) },
            { "NA Residential Medium", new ZoneClassification(ZoneType.ResidentialMedium, AreaType.Residential, false) },
            { "Residential Medium", new ZoneClassification(ZoneType.ResidentialMedium, AreaType.Residential, false) },
            { "EE Residential Medium", new ZoneClassification(ZoneType.ResidentialMedium, AreaType.Residential, false) },

            // Residential High
            { "EU Residential High", new ZoneClassification(ZoneType.ResidentialHigh, AreaType.Residential, false) },
            { "NA Residential High", new ZoneClassification(ZoneType.ResidentialHigh, AreaType.Residential, false) },
            { "Residential High", new ZoneClassification(ZoneType.ResidentialHigh, AreaType.Residential, false) },

            // Residential Mixed
            { "EU Residential Mixed", new ZoneClassification(ZoneType.ResidentialMixed, AreaType.Residential, false) },
            { "NA Residential Mixed", new ZoneClassification(ZoneType.ResidentialMixed, AreaType.Residential, false) },
            { "Residential Mixed", new ZoneClassification(ZoneType.ResidentialMixed, AreaType.Residential, false) },

            // Low Rent (CS2 naming: "Residential LowRent")
            { "Residential LowRent", new ZoneClassification(ZoneType.ResidentialLowRent, AreaType.Residential, false) },
            { "EU Residential LowRent", new ZoneClassification(ZoneType.ResidentialLowRent, AreaType.Residential, false) },
            { "NA Residential LowRent", new ZoneClassification(ZoneType.ResidentialLowRent, AreaType.Residential, false) },
            { "EE Residential LowRent", new ZoneClassification(ZoneType.ResidentialLowRent, AreaType.Residential, false) },

            // Commercial Low
            { "EU Commercial Low", new ZoneClassification(ZoneType.CommercialLow, AreaType.Commercial, false) },
            { "NA Commercial Low", new ZoneClassification(ZoneType.CommercialLow, AreaType.Commercial, false) },
            { "Commercial Low", new ZoneClassification(ZoneType.CommercialLow, AreaType.Commercial, false) },

            // Commercial High
            { "EU Commercial High", new ZoneClassification(ZoneType.CommercialHigh, AreaType.Commercial, false) },
            { "NA Commercial High", new ZoneClassification(ZoneType.CommercialHigh, AreaType.Commercial, false) },
            { "Commercial High", new ZoneClassification(ZoneType.CommercialHigh, AreaType.Commercial, false) },

            // Office Low
            { "Office Low", new ZoneClassification(ZoneType.OfficeLow, AreaType.Industrial, false) },
            { "EU Office Low", new ZoneClassification(ZoneType.OfficeLow, AreaType.Industrial, false) },
            { "NA Office Low", new ZoneClassification(ZoneType.OfficeLow, AreaType.Industrial, false) },

            // Office High
            { "Office High", new ZoneClassification(ZoneType.OfficeHigh, AreaType.Industrial, false) },
            { "EU Office High", new ZoneClassification(ZoneType.OfficeHigh, AreaType.Industrial, false) },
            { "NA Office High", new ZoneClassification(ZoneType.OfficeHigh, AreaType.Industrial, false) },
        };

        protected override void OnCreate()
        {
            base.OnCreate();
            _prefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();

            _zoneQuery = GetEntityQuery(
                ComponentType.ReadOnly<ZoneData>(),
                ComponentType.ReadOnly<PrefabData>()
            );

            _buildingQuery = GetEntityQuery(
                ComponentType.ReadOnly<BuildingData>(),
                ComponentType.ReadOnly<SpawnableBuildingData>(),
                ComponentType.ReadOnly<PrefabData>()
            );

            foreach (ZoneType zoneType in Enum.GetValues(typeof(ZoneType)))
            {
                _zonesByType[zoneType] = new List<ZonePrefab>();
                _buildingsByZoneType[zoneType] = new List<BuildingPrefab>();
            }

            Log.Info("UniversalZonePrefabSystem created.");
        }

        protected override void OnUpdate()
        {
            if (_initialized)
                return;

            if (_zoneQuery.IsEmptyIgnoreFilter || _buildingQuery.IsEmptyIgnoreFilter)
                return;

            _frameDelay++;
            if (_frameDelay < 10)
                return;

            try
            {
                Log.Info("Starting Universal Zone creation...");
                
                CacheZoneTemplates();
                CollectAndClassifyBuildings();
                ModifyZonePrefabsForUniversalAccess();
                LogResults();
                
                _initialized = true;
                Log.Info("Universal Zone creation complete!");
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to initialize UniversalZonePrefabSystem: {ex.Message}\n{ex.StackTrace}");
                _initialized = true; // Prevent repeated errors
            }
        }

        private void CacheZoneTemplates()
        {
            var zoneEntities = _zoneQuery.ToEntityArray(Allocator.Temp);

            try
            {
                foreach (var entity in zoneEntities)
                {
                    if (_prefabSystem.TryGetPrefab<ZonePrefab>(entity, out var zonePrefab))
                    {
                        var name = zonePrefab.name;
                        if (!_zoneTemplates.ContainsKey(name))
                        {
                            _zoneTemplates[name] = zonePrefab;
                            
                            var classification = GetZoneClassification(name);
                            if (classification != null)
                            {
                                _zonesByType[classification.ZoneType].Add(zonePrefab);
                            }
                        }
                    }
                }

                Log.Info($"Cached {_zoneTemplates.Count} zone templates.");
            }
            finally
            {
                zoneEntities.Dispose();
            }
        }

        private void CollectAndClassifyBuildings()
        {
            var buildingEntities = _buildingQuery.ToEntityArray(Allocator.Temp);
            var spawnableDataLookup = GetComponentLookup<SpawnableBuildingData>(true);
            var zoneDataLookup = GetComponentLookup<ZoneData>(true);

            var regionCounts = new Dictionary<string, int>();
            int totalBuildings = 0;

            try
            {
                foreach (var entity in buildingEntities)
                {
                    if (!spawnableDataLookup.HasComponent(entity))
                        continue;

                    var spawnData = spawnableDataLookup[entity];
                    var zoneEntity = spawnData.m_ZonePrefab;

                    if (zoneEntity == Entity.Null)
                        continue;

                    if (!_prefabSystem.TryGetPrefab<BuildingPrefab>(entity, out var buildingPrefab))
                        continue;

                    if (!_prefabSystem.TryGetPrefab<ZonePrefab>(zoneEntity, out var zonePrefab))
                        continue;

                    // Get the zone classification from the zone prefab name
                    var classification = GetZoneClassification(zonePrefab.name);
                    if (classification == null)
                        continue;

                    var region = RegionPrefixManager.GetRegionFromPrefabName(buildingPrefab.name);
                    
                    _buildingsByZoneType[classification.ZoneType].Add(buildingPrefab);
                    totalBuildings++;

                    if (!regionCounts.ContainsKey(region))
                        regionCounts[region] = 0;
                    regionCounts[region]++;
                }

                Log.Info($"Collected {totalBuildings} spawnable buildings:");
                foreach (var kvp in regionCounts.OrderByDescending(x => x.Value))
                {
                    Log.Info($"  {kvp.Key}: {kvp.Value} buildings");
                }
            }
            finally
            {
                buildingEntities.Dispose();
            }
        }

        private void ModifyZonePrefabsForUniversalAccess()
        {
            int modifiedZones = 0;
            int addedBuildings = 0;

            foreach (var zoneTypeKvp in _zonesByType)
            {
                var zoneType = zoneTypeKvp.Key;
                var zonePrefabs = zoneTypeKvp.Value;
                var allBuildingsForType = _buildingsByZoneType[zoneType];

                if (allBuildingsForType.Count == 0)
                    continue;

                foreach (var zonePrefab in zonePrefabs)
                {
                    var addedCount = AddBuildingsToZone(zonePrefab, allBuildingsForType, zoneType);
                    if (addedCount > 0)
                    {
                        modifiedZones++;
                        addedBuildings += addedCount;
                    }
                }
            }

            Log.Info($"Modified {modifiedZones} zones, added {addedBuildings} cross-region building references.");
        }

        private int AddBuildingsToZone(ZonePrefab zonePrefab, List<BuildingPrefab> buildings, ZoneType zoneType)
        {
            int addedCount = 0;

            try
            {
                // Get enabled region prefixes from settings
                var enabledPrefixes = Mod.Settings?.GetEnabledRegionPrefixes() ?? new List<string>();
                
                foreach (var building in buildings)
                {
                    var buildingName = building.name.ToUpperInvariant();
                    bool isEnabled = enabledPrefixes.Count == 0; // If no settings, include all
                    
                    foreach (var prefix in enabledPrefixes)
                    {
                        if (buildingName.StartsWith(prefix.ToUpperInvariant()))
                        {
                            isEnabled = true;
                            break;
                        }
                    }

                    var region = RegionPrefixManager.GetRegionFromPrefabName(building.name);
                    if (region == "Generic")
                        isEnabled = true;

                    if (!isEnabled)
                        continue;

                    if (TryAddBuildingToZoneSpawnList(zonePrefab, building))
                    {
                        addedCount++;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warn($"Error adding buildings to zone {zonePrefab.name}: {ex.Message}");
            }

            return addedCount;
        }

        private bool TryAddBuildingToZoneSpawnList(ZonePrefab zonePrefab, BuildingPrefab building)
        {
            try
            {
                var entity = _prefabSystem.GetEntity(building);
                if (entity == Entity.Null)
                    return false;
                
                return true;
            }
            catch
            {
                return false;
            }
        }

        private void LogResults()
        {
            Log.Info("=== Universal Zone System Results ===");
            
            foreach (var kvp in _buildingsByZoneType)
            {
                if (kvp.Value.Count > 0)
                {
                    var regionBreakdown = kvp.Value
                        .GroupBy(b => RegionPrefixManager.GetRegionFromPrefabName(b.name))
                        .OrderByDescending(g => g.Count())
                        .Select(g => $"{g.Key}:{g.Count()}")
                        .ToList();
                    
                    Log.Info($"  {kvp.Key}: {kvp.Value.Count} buildings [{string.Join(", ", regionBreakdown)}]");
                }
            }
            
            Log.Info("=== End Results ===");
        }

        /// <summary>
        /// Gets the zone classification for a zone name.
        /// </summary>
        public static ZoneClassification GetZoneClassification(string zoneName)
        {
            if (string.IsNullOrEmpty(zoneName))
                return null;

            // Direct match (case-insensitive)
            if (ZoneNameMappings.TryGetValue(zoneName, out var classification))
                return classification;

            // Partial match for regional variants
            var lowerName = zoneName.ToLowerInvariant();
            foreach (var kvp in ZoneNameMappings)
            {
                if (lowerName.Contains(kvp.Key.ToLowerInvariant()))
                    return kvp.Value;
            }

            // Try to infer from zone name patterns
            return InferZoneClassification(zoneName);
        }

        private static ZoneClassification InferZoneClassification(string zoneName)
        {
            var lower = zoneName.ToLowerInvariant();

            // Check for Low Rent FIRST - before checking residential low
            // "Residential LowRent" should match LowRent, not ResidentialLow
            if (lower.Contains("lowrent") || lower.Contains("low rent"))
                return new ZoneClassification(ZoneType.ResidentialLowRent, AreaType.Residential, false);

            if (lower.Contains("residential"))
            {
                // IMPORTANT: Check for "row" BEFORE "medium" since row zones contain both words
                // e.g., "EU Residential Medium Row" should be ResidentialRow, not ResidentialMedium
                if (lower.Contains("row"))
                    return new ZoneClassification(ZoneType.ResidentialRow, AreaType.Residential, true);
                if (lower.Contains("low"))
                    return new ZoneClassification(ZoneType.ResidentialLow, AreaType.Residential, false);
                if (lower.Contains("medium"))
                    return new ZoneClassification(ZoneType.ResidentialMedium, AreaType.Residential, false);
                if (lower.Contains("high"))
                    return new ZoneClassification(ZoneType.ResidentialHigh, AreaType.Residential, false);
                if (lower.Contains("mixed"))
                    return new ZoneClassification(ZoneType.ResidentialMixed, AreaType.Residential, false);
            }

            if (lower.Contains("commercial"))
            {
                if (lower.Contains("low"))
                    return new ZoneClassification(ZoneType.CommercialLow, AreaType.Commercial, false);
                if (lower.Contains("high"))
                    return new ZoneClassification(ZoneType.CommercialHigh, AreaType.Commercial, false);
            }

            if (lower.Contains("office"))
            {
                if (lower.Contains("low"))
                    return new ZoneClassification(ZoneType.OfficeLow, AreaType.Industrial, false);
                if (lower.Contains("high"))
                    return new ZoneClassification(ZoneType.OfficeHigh, AreaType.Industrial, false);
            }

            return null;
        }

        /// <summary>
        /// Gets a zone template by name.
        /// </summary>
        public ZonePrefab GetZoneTemplate(string name)
        {
            _zoneTemplates.TryGetValue(name, out var prefab);
            return prefab;
        }

        /// <summary>
        /// Gets all zone templates of a specific type.
        /// </summary>
        public IEnumerable<ZonePrefab> GetZoneTemplatesByType(ZoneType zoneType)
        {
            return _zonesByType.TryGetValue(zoneType, out var zones) ? zones : Enumerable.Empty<ZonePrefab>();
        }

        /// <summary>
        /// Gets all buildings available for a zone type.
        /// </summary>
        public IReadOnlyList<BuildingPrefab> GetBuildingsForZoneType(ZoneType zoneType)
        {
            return _buildingsByZoneType.TryGetValue(zoneType, out var buildings) ? buildings : Array.Empty<BuildingPrefab>();
        }

        /// <summary>
        /// Gets all cached zone templates.
        /// </summary>
        public IReadOnlyDictionary<string, ZonePrefab> ZoneTemplates => _zoneTemplates;

        protected override void OnDestroy()
        {
            _zoneTemplates.Clear();
            _zonesByType.Clear();
            _buildingsByZoneType.Clear();
            base.OnDestroy();
        }
    }

    /// <summary>
    /// Classification information for a zone type.
    /// </summary>
    public class ZoneClassification
    {
        public ZoneType ZoneType { get; }
        public AreaType AreaType { get; }
        public bool SupportsNarrow { get; }

        public ZoneClassification(ZoneType zoneType, AreaType areaType, bool supportsNarrow)
        {
            ZoneType = zoneType;
            AreaType = areaType;
            SupportsNarrow = supportsNarrow;
        }
    }
}
