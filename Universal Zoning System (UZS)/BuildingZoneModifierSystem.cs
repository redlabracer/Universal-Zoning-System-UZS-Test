using System;
using System.Collections.Generic;
using System.Linq;
using Colossal.Logging;
using Game;
using Game.Prefabs;
using Game.SceneFlow;
using Game.Zones;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace UniversalZoningSystem
{
    /// <summary>
    /// System that creates DUPLICATE building prefabs for universal zones.
    /// 
    /// IMPORTANT: Original buildings are NOT modified! They keep their original zone reference.
    /// We CREATE NEW building prefab copies that reference the universal zones.
    /// 
    /// This way:
    /// - Original zones work normally (NA Low Res spawns only NA buildings)
    /// - Universal zones spawn buildings from ALL regions (via duplicates)
    /// 
    /// PERFORMANCE OPTIMIZATIONS (v0.1.3):
    /// - Pre-caches universal zone prefabs by ZoneType
    /// - Caches zone classifications to avoid repeated lookups
    /// - Uses StringComparer.Ordinal for faster HashSet operations
    /// - Uses IndexOf with OrdinalIgnoreCase instead of ToLowerInvariant()
    /// - Reduces logging during iteration
    /// - Tracks timing for performance monitoring
    /// </summary>
    public partial class BuildingZoneModifierSystem : GameSystemBase
    {
        private static readonly ILog Log = Mod.Log;

        private PrefabSystem _prefabSystem;
        private UniversalZoneUISystem _zoneUISystem;
        private EntityQuery _buildingQuery;
        private bool _initialized;
        private int _frameDelay;

        // Track created duplicate prefabs for cleanup
        private readonly List<BuildingPrefab> _createdDuplicates = new List<BuildingPrefab>();
        
        // Statistics
        private readonly Dictionary<string, int> _duplicatesByZone = new Dictionary<string, int>();
        private readonly Dictionary<string, int> _duplicatesByRegion = new Dictionary<string, int>();

        protected override void OnCreate()
        {
            base.OnCreate();
            _prefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();
            _zoneUISystem = World.GetOrCreateSystemManaged<UniversalZoneUISystem>();

            _buildingQuery = GetEntityQuery(
                ComponentType.ReadOnly<BuildingData>(),
                ComponentType.ReadOnly<SpawnableBuildingData>(),
                ComponentType.ReadOnly<PrefabData>()
            );

            Log.Info("BuildingZoneModifierSystem created.");
        }

        /// <summary>
        /// Run during game preload to modify buildings BEFORE the spawn system caches them.
        /// </summary>
        protected override void OnGamePreload(Colossal.Serialization.Entities.Purpose purpose, GameMode mode)
        {
            base.OnGamePreload(purpose, mode);
            
            Log.Info($"BuildingZoneModifierSystem.OnGamePreload: Purpose={purpose}, Mode={mode}");
            
            if (!mode.IsGameOrEditor())
                return;

            // Try to run building modification here, before the spawn system caches buildings
            TryModifyBuildings();
        }

        protected override void OnUpdate()
        {
            if (_initialized)
                return;

            // Wait for universal zone prefabs to be created
            if (_zoneUISystem == null || _zoneUISystem.CreatedZonePrefabs.Count == 0)
                return;

            // Wait for buildings to load
            if (_buildingQuery.IsEmptyIgnoreFilter)
                return;

            // Frame delay to ensure everything is loaded
            _frameDelay++;
            if (_frameDelay < 25)
                return;

            TryModifyBuildings();
        }

        private void TryModifyBuildings()
        {
            if (_initialized)
                return;

            // Need zones to be created first
            if (_zoneUISystem == null || _zoneUISystem.CreatedZonePrefabs.Count == 0)
            {
                Log.Info("Waiting for universal zones to be created...");
                return;
            }

            // Need buildings to be loaded
            if (_buildingQuery.IsEmptyIgnoreFilter)
            {
                Log.Info("Waiting for buildings to load...");
                return;
            }

            try
            {
                ModifyBuildingsForUniversalZones();
                _initialized = true;
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to modify buildings: {ex.Message}\n{ex.StackTrace}");
                _initialized = true;
            }
        }

        private void ModifyBuildingsForUniversalZones()
        {
            var startTime = DateTime.Now;
            Log.Info("=== Creating Building Clones for Universal Zones ===");

            // OPTIMIZATION 1: Pre-cache universal zone prefabs by ZoneType
            var universalZonePrefabs = new Dictionary<ZoneType, ZonePrefab>();
            foreach (var definition in ZoneDefinitions.AllZones)
            {
                var prefab = _zoneUISystem.GetUniversalZonePrefab(definition.Id);
                if (prefab != null && definition.SourceZoneTypes.Length > 0)
                {
                    universalZonePrefabs[definition.SourceZoneTypes[0]] = prefab;
                }
            }

            if (universalZonePrefabs.Count == 0)
            {
                Log.Warn("No universal zone prefabs found!");
                return;
            }

            // OPTIMIZATION 2: Use StringComparer.Ordinal for faster HashSet lookups
            var existingPrefabNames = new HashSet<string>(StringComparer.Ordinal);
            var allPrefabsQuery = GetEntityQuery(ComponentType.ReadOnly<PrefabData>());
            var allPrefabEntities = allPrefabsQuery.ToEntityArray(Allocator.Temp);
            foreach (var entity in allPrefabEntities)
            {
                var prefab = _prefabSystem.GetPrefab<PrefabBase>(entity);
                if (prefab != null)
                {
                    existingPrefabNames.Add(prefab.name);
                }
            }
            allPrefabEntities.Dispose();

            var buildingEntities = _buildingQuery.ToEntityArray(Allocator.Temp);
            var spawnableDataLookup = GetComponentLookup<SpawnableBuildingData>(true);

            // OPTIMIZATION 3: Cache zone classifications to avoid repeated lookups
            var zoneClassificationCache = new Dictionary<Entity, ZoneClassification>();

            try
            {
                int totalCloned = 0;
                int skippedAlreadyCloned = 0;
                int skippedNoClassification = 0;
                int skippedRegionDisabled = 0;
                int skippedInvalidForZone = 0;

                foreach (var buildingEntity in buildingEntities)
                {
                    if (!spawnableDataLookup.HasComponent(buildingEntity))
                        continue;

                    var spawnData = spawnableDataLookup[buildingEntity];
                    if (spawnData.m_ZonePrefab == Entity.Null)
                        continue;

                    var buildingPrefab = _prefabSystem.GetPrefab<BuildingPrefab>(buildingEntity);
                    if (buildingPrefab == null)
                        continue;

                    // Skip if already a Universal clone
                    if (buildingPrefab.name.StartsWith("Universal_"))
                    {
                        skippedAlreadyCloned++;
                        continue;
                    }

                    // OPTIMIZATION 4: Use cached zone classification
                    if (!zoneClassificationCache.TryGetValue(spawnData.m_ZonePrefab, out var classification))
                    {
                        if (_prefabSystem.TryGetPrefab<ZonePrefab>(spawnData.m_ZonePrefab, out var originalZonePrefab))
                        {
                            classification = UniversalZonePrefabSystem.GetZoneClassification(originalZonePrefab.name);
                        }
                        zoneClassificationCache[spawnData.m_ZonePrefab] = classification;
                    }

                    if (classification == null)
                    {
                        skippedNoClassification++;
                        continue;
                    }

                    // Check region
                    var region = RegionPrefixManager.GetRegionFromPrefabName(buildingPrefab.name);
                    if (!IsRegionEnabled(region))
                    {
                        skippedRegionDisabled++;
                        continue;
                    }

                    // Validate building name matches zone type
                    if (!IsBuildingValidForZoneType(buildingPrefab.name, classification.ZoneType))
                    {
                        skippedInvalidForZone++;
                        continue;
                    }

                    // OPTIMIZATION 5: Use pre-cached universal zone prefab
                    if (!universalZonePrefabs.TryGetValue(classification.ZoneType, out var universalZonePrefab))
                        continue;

                    // Check if clone already exists
                    string cloneName = "Universal_" + buildingPrefab.name;
                    if (existingPrefabNames.Contains(cloneName))
                        continue;

                    // CLONE the prefab
                    var newPrefab = UnityEngine.Object.Instantiate(buildingPrefab);
                    newPrefab.name = cloneName;

                    if (newPrefab.TryGet<SpawnableBuilding>(out var spawnable))
                    {
                        spawnable.m_ZoneType = universalZonePrefab;

                        _prefabSystem.AddPrefab(newPrefab);
                        _createdDuplicates.Add(newPrefab);
                        existingPrefabNames.Add(cloneName);
                        totalCloned++;

                        // OPTIMIZATION 6: Only log first 3 clones
                        if (totalCloned <= 3)
                        {
                            Log.Info($"  Cloned: {cloneName} -> {universalZonePrefab.name}");
                        }

                        // Track statistics
                        var zoneKey = classification.ZoneType.ToString();
                        if (!_duplicatesByZone.ContainsKey(zoneKey))
                            _duplicatesByZone[zoneKey] = 0;
                        _duplicatesByZone[zoneKey]++;

                        if (!_duplicatesByRegion.ContainsKey(region))
                            _duplicatesByRegion[region] = 0;
                        _duplicatesByRegion[region]++;
                    }
                    else
                    {
                        UnityEngine.Object.Destroy(newPrefab);
                    }
                }

                var elapsed = (DateTime.Now - startTime).TotalMilliseconds;
                Log.Info($"=== Building Cloning Complete in {elapsed:F0}ms ===");
                Log.Info($"Total cloned: {totalCloned}, Cached zones: {zoneClassificationCache.Count}");
                Log.Info($"Skipped: AlreadyCloned={skippedAlreadyCloned}, NoClass={skippedNoClassification}, RegionOff={skippedRegionDisabled}, InvalidZone={skippedInvalidForZone}");
                
                // OPTIMIZATION 7: Compact summary logs
                if (_duplicatesByZone.Count > 0)
                {
                    Log.Info($"By Zone: {string.Join(", ", _duplicatesByZone.Select(kvp => $"{kvp.Key}:{kvp.Value}"))}");
                }
                if (_duplicatesByRegion.Count > 0)
                {
                    Log.Info($"By Region: {string.Join(", ", _duplicatesByRegion.Select(kvp => $"{kvp.Key}:{kvp.Value}"))}");
                }
            }
            finally
            {
                buildingEntities.Dispose();
            }
        }

        /// <summary>
        /// Validates that a building name is appropriate for the target zone type.
        /// This prevents cross-contamination like LowRent buildings in LowResidential zones.
        /// OPTIMIZATION: Uses IndexOf with OrdinalIgnoreCase instead of ToLowerInvariant() + Contains()
        /// </summary>
        private bool IsBuildingValidForZoneType(string buildingName, ZoneType zoneType)
        {
            switch (zoneType)
            {
                case ZoneType.ResidentialLow:
                    // Exclude buildings with "lowrent", "medium", "high", "mixed" in name
                    if (ContainsIgnoreCase(buildingName, "lowrent") || ContainsIgnoreCase(buildingName, "low_rent"))
                        return false;
                    if (ContainsIgnoreCase(buildingName, "mixed"))
                        return false;
                    return true;

                case ZoneType.ResidentialRow:
                    return true;

                case ZoneType.ResidentialMedium:
                    if (ContainsIgnoreCase(buildingName, "lowrent") || ContainsIgnoreCase(buildingName, "low_rent"))
                        return false;
                    return true;

                case ZoneType.ResidentialHigh:
                    if (ContainsIgnoreCase(buildingName, "lowrent") || ContainsIgnoreCase(buildingName, "low_rent"))
                        return false;
                    return true;

                case ZoneType.ResidentialLowRent:
                    return true;

                case ZoneType.ResidentialMixed:
                    if (ContainsIgnoreCase(buildingName, "lowrent") || ContainsIgnoreCase(buildingName, "low_rent"))
                        return false;
                    return true;

                default:
                    return true;
            }
        }

        /// <summary>
        /// Fast case-insensitive string contains check using IndexOf.
        /// More efficient than ToLowerInvariant() + Contains() as it avoids string allocation.
        /// </summary>
        private static bool ContainsIgnoreCase(string source, string value)
        {
            return source.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;
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
            // Clean up created duplicates
            foreach (var prefab in _createdDuplicates)
            {
                if (prefab != null)
                    UnityEngine.Object.Destroy(prefab);
            }
            
            _createdDuplicates.Clear();
            _duplicatesByZone.Clear();
            _duplicatesByRegion.Clear();
            
            base.OnDestroy();
        }
    }
}
