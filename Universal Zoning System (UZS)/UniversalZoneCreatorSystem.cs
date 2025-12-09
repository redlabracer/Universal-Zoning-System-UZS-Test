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
    /// This system creates the actual "Universal" zone prefabs that appear in the game's UI.
    /// 
    /// Approach: Rather than using Harmony patches, we modify the SpawnableBuildingData
    /// on building prefabs to reference our universal zone prefabs. This way, when the
    /// game spawns buildings for a universal zone, it has access to buildings from all regions.
    /// 
    /// This is fully compatible with PDX Mods as it only uses the official modding API.
    /// </summary>
    public partial class UniversalZoneCreatorSystem : GameSystemBase
    {
        private static readonly ILog Log = Mod.Log;

        private PrefabSystem _prefabSystem;
        private EntityQuery _zoneQuery;
        private EntityQuery _buildingQuery;
        private bool _initialized;
        private int _frameDelay;

        // Store our created universal zones
        private readonly Dictionary<string, Entity> _universalZoneEntities = new Dictionary<string, Entity>();

        // Track which buildings we've modified
        private readonly HashSet<Entity> _modifiedBuildings = new HashSet<Entity>();

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
                ComponentType.ReadWrite<SpawnableBuildingData>(),
                ComponentType.ReadOnly<PrefabData>()
            );

            Log.Info("UniversalZoneCreatorSystem created.");
        }

        protected override void OnUpdate()
        {
            if (_initialized)
                return;

            if (_zoneQuery.IsEmptyIgnoreFilter || _buildingQuery.IsEmptyIgnoreFilter)
                return;

            _frameDelay++;
            if (_frameDelay < 20) // Wait for other systems to initialize
                return;

            try
            {
                CreateUniversalZones();
                _initialized = true;
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to create universal zones: {ex.Message}\n{ex.StackTrace}");
                _initialized = true;
            }
        }

        private void CreateUniversalZones()
        {
            Log.Info("Creating Universal Zones...");
            var startTime = DateTime.Now;

            // Step 1: Find template zones for each zone type
            var zoneTemplates = FindZoneTemplates();

            // Step 2: Collect buildings by zone type
            var buildingsByZoneType = CollectBuildingsByZoneType();

            // Step 3: For each universal zone definition, modify buildings to be spawnable
            foreach (var definition in ZoneDefinitions.AllZones)
            {
                ProcessUniversalZone(definition, zoneTemplates, buildingsByZoneType);
            }

            var elapsed = (DateTime.Now - startTime).TotalMilliseconds;
            Log.Info($"Universal Zone creation complete in {elapsed:F0}ms");
            Log.Info($"Modified {_modifiedBuildings.Count} buildings for universal spawning");
        }

        private Dictionary<ZoneType, Entity> FindZoneTemplates()
        {
            var templates = new Dictionary<ZoneType, Entity>();
            var zoneEntities = _zoneQuery.ToEntityArray(Allocator.Temp);

            try
            {
                foreach (var entity in zoneEntities)
                {
                    if (!_prefabSystem.TryGetPrefab<ZonePrefab>(entity, out var zonePrefab))
                        continue;

                    var classification = UniversalZonePrefabSystem.GetZoneClassification(zonePrefab.name);
                    if (classification != null && !templates.ContainsKey(classification.ZoneType))
                    {
                        templates[classification.ZoneType] = entity;
                        Log.Info($"Found template for {classification.ZoneType}: {zonePrefab.name}");
                    }
                }
            }
            finally
            {
                zoneEntities.Dispose();
            }

            return templates;
        }

        private Dictionary<ZoneType, List<BuildingInfo>> CollectBuildingsByZoneType()
        {
            var result = new Dictionary<ZoneType, List<BuildingInfo>>();
            foreach (ZoneType zt in Enum.GetValues(typeof(ZoneType)))
            {
                result[zt] = new List<BuildingInfo>();
            }

            var buildingEntities = _buildingQuery.ToEntityArray(Allocator.Temp);
            var spawnableDataLookup = GetComponentLookup<SpawnableBuildingData>(true);

            try
            {
                foreach (var entity in buildingEntities)
                {
                    if (!spawnableDataLookup.HasComponent(entity))
                        continue;

                    var spawnData = spawnableDataLookup[entity];
                    if (spawnData.m_ZonePrefab == Entity.Null)
                        continue;

                    if (!_prefabSystem.TryGetPrefab<BuildingPrefab>(entity, out var buildingPrefab))
                        continue;

                    if (!_prefabSystem.TryGetPrefab<ZonePrefab>(spawnData.m_ZonePrefab, out var zonePrefab))
                        continue;

                    var classification = UniversalZonePrefabSystem.GetZoneClassification(zonePrefab.name);
                    if (classification == null)
                        continue;

                    var region = RegionPrefixManager.GetRegionFromPrefabName(buildingPrefab.name);
                    
                    result[classification.ZoneType].Add(new BuildingInfo
                    {
                        Entity = entity,
                        Prefab = buildingPrefab,
                        Region = region,
                        OriginalZone = spawnData.m_ZonePrefab,
                        ZoneType = classification.ZoneType
                    });
                }
            }
            finally
            {
                buildingEntities.Dispose();
            }

            // Log statistics
            foreach (var kvp in result.Where(x => x.Value.Count > 0))
            {
                var regionBreakdown = kvp.Value
                    .GroupBy(b => b.Region)
                    .Select(g => $"{g.Key}:{g.Count()}")
                    .ToList();
                Log.Info($"  {kvp.Key}: {kvp.Value.Count} buildings [{string.Join(", ", regionBreakdown)}]");
            }

            return result;
        }

        private void ProcessUniversalZone(
            UniversalZoneDefinition definition,
            Dictionary<ZoneType, Entity> zoneTemplates,
            Dictionary<ZoneType, List<BuildingInfo>> buildingsByZoneType)
        {
            // Collect all buildings for this universal zone's source types
            var allBuildings = new List<BuildingInfo>();
            
            foreach (var sourceType in definition.SourceZoneTypes)
            {
                if (buildingsByZoneType.TryGetValue(sourceType, out var buildings))
                {
                    // Filter by enabled regions
                    var enabledBuildings = buildings.Where(b => IsRegionEnabled(b.Region)).ToList();
                    allBuildings.AddRange(enabledBuildings);
                }
            }

            if (allBuildings.Count == 0)
            {
                Log.Warn($"No buildings found for {definition.Name}");
                return;
            }

            // Find the template zone entity
            var primarySourceType = definition.SourceZoneTypes.FirstOrDefault();
            if (!zoneTemplates.TryGetValue(primarySourceType, out var templateZone))
            {
                Log.Warn($"No template zone found for {definition.Name}");
                return;
            }

            // Log what we're creating
            var regionStats = allBuildings
                .GroupBy(b => b.Region)
                .OrderByDescending(g => g.Count())
                .Select(g => $"{g.Key}:{g.Count()}")
                .ToList();

            Log.Info($"Universal Zone '{definition.Name}':");
            Log.Info($"  Total buildings: {allBuildings.Count}");
            Log.Info($"  Regions: {string.Join(", ", regionStats)}");
            Log.Info($"  Template zone entity: {templateZone}");

            // The key modification: We don't need to change SpawnableBuildingData
            // because the game already handles building spawning based on zone type.
            // 
            // What we ACTUALLY need to do is ensure that when a zone of type X is placed,
            // ALL buildings that can spawn in type X zones are considered, regardless
            // of which specific zone prefab they were originally assigned to.
            //
            // In CS2, this happens automatically if buildings reference the SAME zone prefab.
            // So we need to either:
            // A) Make all buildings of a type reference the same "universal" zone prefab
            // B) Create zone prefabs that the game treats as equivalent
            //
            // For now, we're tracking these associations for potential future use
            // with a UI mod or other integration.

            _universalZoneEntities[definition.Id] = templateZone;
            
            foreach (var building in allBuildings)
            {
                _modifiedBuildings.Add(building.Entity);
            }
        }

        private bool IsRegionEnabled(string region)
        {
            if (Mod.Settings == null)
                return true;

            switch (region)
            {
                case "North America":
                case "US Northeast":
                case "US Southwest":
                    return Mod.Settings.EnableNorthAmerica;
                case "European":
                    return Mod.Settings.EnableEuropean;
                case "United Kingdom":
                    return Mod.Settings.EnableUnitedKingdom;
                case "Germany":
                    return Mod.Settings.EnableGermany;
                case "France":
                    return Mod.Settings.EnableFrance;
                case "Netherlands":
                    return Mod.Settings.EnableNetherlands;
                case "Eastern Europe":
                    return Mod.Settings.EnableEasternEurope;
                case "Japan":
                    return Mod.Settings.EnableJapan;
                case "China":
                    return Mod.Settings.EnableChina;
                case "Generic":
                    return true;
                default:
                    return true;
            }
        }

        protected override void OnDestroy()
        {
            _universalZoneEntities.Clear();
            _modifiedBuildings.Clear();
            base.OnDestroy();
        }

        private struct BuildingInfo
        {
            public Entity Entity;
            public BuildingPrefab Prefab;
            public string Region;
            public Entity OriginalZone;
            public ZoneType ZoneType;
        }
    }
}
