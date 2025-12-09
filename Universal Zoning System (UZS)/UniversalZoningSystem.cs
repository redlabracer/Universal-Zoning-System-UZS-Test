using System;
using System.Collections.Generic;
using System.Linq;
using Colossal.Logging;
using Game;
using Game.Prefabs;
using Game.Zones;
using Unity.Collections;
using Unity.Entities;

namespace UniversalZoningSystem
{
    /// <summary>
    /// Core system that creates and manages universal zone types.
    /// These zones draw buildings from all installed regional content packs.
    /// </summary>
    public partial class UniversalZoningSystem : GameSystemBase
    {
        private static readonly ILog Log = Mod.Log;

        private PrefabSystem _prefabSystem;
        private EntityQuery _buildingQuery;
        private EntityQuery _zoneQuery;
        private bool _initialized;

        private readonly Dictionary<string, ZonePrefab> _universalZones = new Dictionary<string, ZonePrefab>();
        private readonly BuildingMatcher _buildingMatcher = new BuildingMatcher();

        protected override void OnCreate()
        {
            base.OnCreate();
            _prefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();

            // Create queries for ECS data
            _buildingQuery = GetEntityQuery(
                ComponentType.ReadOnly<BuildingData>(),
                ComponentType.ReadOnly<SpawnableBuildingData>(),
                ComponentType.ReadOnly<PrefabData>()
            );

            _zoneQuery = GetEntityQuery(
                ComponentType.ReadOnly<ZoneData>(),
                ComponentType.ReadOnly<PrefabData>()
            );

            Log.Info("UniversalZoningSystem created.");
        }

        protected override void OnUpdate()
        {
            if (_initialized)
                return;

            // Wait until prefabs are loaded
            if (_buildingQuery.IsEmptyIgnoreFilter)
                return;

            try
            {
                InitializeUniversalZones();
                _initialized = true;
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to initialize Universal Zoning System: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private void InitializeUniversalZones()
        {
            Log.Info("Initializing Universal Zones...");

            var startTime = DateTime.Now;

            // Collect all building prefabs organized by zone type
            var buildingsByZoneType = CollectBuildingsByZoneType();

            // Create universal zones for each category
            foreach (var zoneDefinition in ZoneDefinitions.AllZones)
            {
                CreateUniversalZone(zoneDefinition, buildingsByZoneType);
            }

            // Log statistics
            var stats = _buildingMatcher.GetStatistics();
            stats.LogStatistics(Log);

            var elapsed = (DateTime.Now - startTime).TotalMilliseconds;
            Log.Info($"Universal Zones initialized in {elapsed:F0}ms. Created {_universalZones.Count} universal zone types.");
        }

        private Dictionary<ZoneType, List<BuildingPrefab>> CollectBuildingsByZoneType()
        {
            var buildingsByType = new Dictionary<ZoneType, List<BuildingPrefab>>();

            foreach (ZoneType zoneType in Enum.GetValues(typeof(ZoneType)))
            {
                buildingsByType[zoneType] = new List<BuildingPrefab>();
            }

            // Get building entities and their spawn data
            var buildingEntities = _buildingQuery.ToEntityArray(Allocator.Temp);
            var spawnableDataLookup = GetComponentLookup<SpawnableBuildingData>(true);
            var zoneDataLookup = GetComponentLookup<ZoneData>(true);

            int totalBuildings = 0;
            var regionCounts = new Dictionary<string, int>();
            var zoneTypeCounts = new Dictionary<ZoneType, int>();

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

                    if (!zoneDataLookup.HasComponent(zoneEntity))
                        continue;

                    var zoneData = zoneDataLookup[zoneEntity];

                    // Get the building prefab
                    if (!_prefabSystem.TryGetPrefab<BuildingPrefab>(entity, out var buildingPrefab))
                        continue;

                    // Get the zone prefab for name-based classification
                    ZoneType zoneType;
                    if (_prefabSystem.TryGetPrefab<ZonePrefab>(zoneEntity, out var zonePrefab))
                    {
                        zoneType = ClassifyZoneTypeFromPrefab(zonePrefab, zoneData);
                    }
                    else
                    {
                        zoneType = ClassifyZoneType(zoneData);
                    }

                    if (buildingsByType.ContainsKey(zoneType))
                    {
                        buildingsByType[zoneType].Add(buildingPrefab);
                        totalBuildings++;

                        // Track zone type statistics
                        if (!zoneTypeCounts.ContainsKey(zoneType))
                            zoneTypeCounts[zoneType] = 0;
                        zoneTypeCounts[zoneType]++;

                        // Track region statistics
                        var region = RegionPrefixManager.GetRegionFromPrefabName(buildingPrefab.name);
                        if (!regionCounts.ContainsKey(region))
                            regionCounts[region] = 0;
                        regionCounts[region]++;
                    }
                }
            }
            finally
            {
                buildingEntities.Dispose();
            }

            Log.Info($"Collected {totalBuildings} buildings across all regions:");
            foreach (var kvp in regionCounts.OrderByDescending(x => x.Value))
            {
                Log.Info($"  {kvp.Key}: {kvp.Value} buildings");
            }

            Log.Info($"Buildings by zone type:");
            foreach (var kvp in zoneTypeCounts.OrderByDescending(x => x.Value))
            {
                Log.Info($"  {kvp.Key}: {kvp.Value} buildings");
            }

            return buildingsByType;
        }

        private ZoneType ClassifyZoneType(ZoneData zoneData)
        {
            var areaType = zoneData.m_AreaType;
            var zoneFlags = zoneData.m_ZoneFlags;

            // Classify based on area type and flags
            if (areaType == AreaType.Residential)
            {
                // Check for row/narrow housing
                if ((zoneFlags & ZoneFlags.SupportNarrow) != 0)
                    return ZoneType.ResidentialRow;

                // Use zone flags to determine density
                // Note: These flag checks may need adjustment based on actual game data
                return ZoneType.ResidentialLow;
            }
            else if (areaType == AreaType.Commercial)
            {
                return ZoneType.CommercialLow;
            }
            else if (areaType == AreaType.Industrial)
            {
                return ZoneType.Office;
            }

            return ZoneType.None;
        }

        private ZoneType ClassifyZoneTypeFromPrefab(ZonePrefab zonePrefab, ZoneData zoneData)
        {
            // First try to classify from the prefab name (more accurate)
            var classification = UniversalZonePrefabSystem.GetZoneClassification(zonePrefab.name);
            if (classification != null)
                return classification.ZoneType;

            // Fall back to ZoneData-based classification
            return ClassifyZoneType(zoneData);
        }

        private void CreateUniversalZone(UniversalZoneDefinition definition, Dictionary<ZoneType, List<BuildingPrefab>> buildingsByType)
        {
            try
            {
                // Find a template zone to base our universal zone on
                var templateZone = FindTemplateZone(definition.SourceZoneTypes.FirstOrDefault());
                if (templateZone == null)
                {
                    Log.Warn($"Could not find template zone for {definition.Name}");
                    return;
                }

                // Collect buildings from all source zone types and register with matcher
                var universalBuildings = new List<BuildingPrefab>();
                foreach (var sourceType in definition.SourceZoneTypes)
                {
                    if (buildingsByType.TryGetValue(sourceType, out var buildings))
                    {
                        universalBuildings.AddRange(buildings);

                        // Register each building with the matcher
                        foreach (var building in buildings)
                        {
                            _buildingMatcher.RegisterBuilding(definition.Id, building);
                        }
                    }
                }

                if (universalBuildings.Count == 0)
                {
                    Log.Warn($"No buildings found for {definition.Name}");
                    return;
                }

                // Log the creation
                Log.Info($"Created {definition.Name} with {universalBuildings.Count} building variants from {definition.SourceZoneTypes.Length} zone types");

                // Store reference for future use
                _universalZones[definition.Id] = templateZone;
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to create universal zone {definition.Name}: {ex.Message}");
            }
        }

        private ZonePrefab FindTemplateZone(ZoneType zoneType)
        {
            var zoneEntities = _zoneQuery.ToEntityArray(Allocator.Temp);
            var zoneDataLookup = GetComponentLookup<ZoneData>(true);

            try
            {
                foreach (var entity in zoneEntities)
                {
                    if (!zoneDataLookup.HasComponent(entity))
                        continue;

                    var zoneData = zoneDataLookup[entity];
                    var classifiedType = ClassifyZoneType(zoneData);

                    if (classifiedType == zoneType)
                    {
                        if (_prefabSystem.TryGetPrefab<ZonePrefab>(entity, out var zonePrefab))
                        {
                            return zonePrefab;
                        }
                    }
                }
            }
            finally
            {
                zoneEntities.Dispose();
            }

            return null;
        }

        /// <summary>
        /// Gets the building matcher for external access.
        /// </summary>
        public BuildingMatcher BuildingMatcher => _buildingMatcher;

        protected override void OnDestroy()
        {
            _universalZones.Clear();
            _buildingMatcher.Clear();
            base.OnDestroy();
        }
    }
}
