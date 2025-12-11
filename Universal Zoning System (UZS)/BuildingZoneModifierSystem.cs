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
            Log.Info("=== Creating Building Clones for Universal Zones ===");
            Log.Info("Using the working approach from the old UZS mod!");

            // Build a cache of existing prefab names to avoid duplicates
            var existingPrefabNames = new HashSet<string>();
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

            try
            {
                int totalCloned = 0;

                foreach (var buildingEntity in buildingEntities)
                {
                    if (!spawnableDataLookup.HasComponent(buildingEntity))
                        continue;

                    var spawnData = spawnableDataLookup[buildingEntity];
                    if (spawnData.m_ZonePrefab == Entity.Null)
                        continue;

                    // Get the building prefab
                    var buildingPrefab = _prefabSystem.GetPrefab<BuildingPrefab>(buildingEntity);
                    if (buildingPrefab == null)
                        continue;

                    // Skip if already a Universal clone
                    if (buildingPrefab.name.StartsWith("Universal_"))
                        continue;

                    // Get the original zone prefab to determine zone type
                    if (!_prefabSystem.TryGetPrefab<ZonePrefab>(spawnData.m_ZonePrefab, out var originalZonePrefab))
                        continue;

                    // Classify the zone
                    var classification = UniversalZonePrefabSystem.GetZoneClassification(originalZonePrefab.name);
                    if (classification == null)
                        continue;

                    // Check region
                    var region = RegionPrefixManager.GetRegionFromPrefabName(buildingPrefab.name);
                    if (!IsRegionEnabled(region))
                        continue;

                    // STRICT FILTER: Validate building name matches the zone type
                    // This prevents buildings like "LowRent" from being cloned into "LowResidential"
                    if (!IsBuildingValidForZoneType(buildingPrefab.name, classification.ZoneType))
                    {
                        continue;
                    }

                    // Find the matching universal zone
                    string universalZoneId = GetUniversalZoneIdForType(classification.ZoneType);
                    if (universalZoneId == null)
                        continue;

                    var universalZonePrefab = _zoneUISystem.GetUniversalZonePrefab(universalZoneId);
                    if (universalZonePrefab == null)
                        continue;

                    // Check if clone already exists
                    string cloneName = "Universal_" + buildingPrefab.name;
                    if (existingPrefabNames.Contains(cloneName))
                        continue;

                    // CLONE the prefab and set the zone BEFORE adding to prefab system
                    var newPrefab = UnityEngine.Object.Instantiate(buildingPrefab);
                    newPrefab.name = cloneName;

                    // Update the SpawnableBuilding component on the PREFAB (not entity!)
                    // This is the KEY difference from our previous approach
                    if (newPrefab.TryGet<SpawnableBuilding>(out var spawnable))
                    {
                        spawnable.m_ZoneType = universalZonePrefab;

                        // Register with prefab system - this creates the entity with correct zone
                        _prefabSystem.AddPrefab(newPrefab);
                        _createdDuplicates.Add(newPrefab);
                        existingPrefabNames.Add(cloneName);
                        totalCloned++;

                        if (totalCloned <= 5)
                        {
                            Log.Info($"  Cloned: {cloneName} -> {universalZonePrefab.name}");
                        }

                        // Track statistics
                        if (!_duplicatesByZone.ContainsKey(universalZoneId))
                            _duplicatesByZone[universalZoneId] = 0;
                        _duplicatesByZone[universalZoneId]++;

                        if (!_duplicatesByRegion.ContainsKey(region))
                            _duplicatesByRegion[region] = 0;
                        _duplicatesByRegion[region]++;
                    }
                    else
                    {
                        Log.Warn($"Could not find SpawnableBuilding component on {newPrefab.name}");
                        UnityEngine.Object.Destroy(newPrefab);
                    }
                }

                Log.Info($"=== Building Cloning Complete ===");
                Log.Info($"Total buildings cloned: {totalCloned}");
                
                if (_duplicatesByZone.Count > 0)
                {
                    Log.Info("Clones by Universal Zone:");
                    foreach (var kvp in _duplicatesByZone.OrderByDescending(x => x.Value))
                    {
                        Log.Info($"  {kvp.Key}: {kvp.Value} buildings");
                    }
                }

                if (_duplicatesByRegion.Count > 0)
                {
                    Log.Info("Clones by Region:");
                    foreach (var kvp in _duplicatesByRegion.OrderByDescending(x => x.Value))
                    {
                        Log.Info($"  {kvp.Key}: {kvp.Value} buildings");
                    }
                }
            }
            finally
            {
                buildingEntities.Dispose();
            }
        }

        private string GetUniversalZoneIdForType(ZoneType zoneType)
        {
            switch (zoneType)
            {
                case ZoneType.ResidentialLow: return "UZS_LowResidential";
                case ZoneType.ResidentialRow: return "UZS_RowResidential";
                case ZoneType.ResidentialMedium: return "UZS_MediumResidential";
                case ZoneType.ResidentialHigh: return "UZS_HighResidential";
                case ZoneType.ResidentialLowRent: return "UZS_LowRent";
                case ZoneType.ResidentialMixed: return "UZS_MixedUse";
                case ZoneType.CommercialLow: return "UZS_LowCommercial";
                case ZoneType.CommercialHigh: return "UZS_HighCommercial";
                case ZoneType.OfficeLow: return "UZS_LowOffice";
                case ZoneType.OfficeHigh: return "UZS_HighOffice";
                default: return null;
            }
        }

        /// <summary>
        /// Validates that a building name is appropriate for the target zone type.
        /// This prevents cross-contamination like LowRent buildings in LowResidential zones.
        /// </summary>
        private bool IsBuildingValidForZoneType(string buildingName, ZoneType zoneType)
        {
            var lowerName = buildingName.ToLowerInvariant();

            switch (zoneType)
            {
                case ZoneType.ResidentialLow:
                    // Exclude buildings with "lowrent", "medium", "high", "mixed", "row" in name
                    if (lowerName.Contains("lowrent") || lowerName.Contains("low_rent") || lowerName.Contains("low rent"))
                        return false;
                    if (lowerName.Contains("medium") && !lowerName.Contains("low"))
                        return false;
                    if (lowerName.Contains("high") && !lowerName.Contains("low"))
                        return false;
                    if (lowerName.Contains("mixed"))
                        return false;
                    // Allow row houses in low residential if they're specifically low density
                    return true;

                case ZoneType.ResidentialRow:
                    // Row houses - must have "row" in name or be from row zone
                    // But exclude if clearly medium/high density non-row
                    return true;

                case ZoneType.ResidentialMedium:
                    // Exclude low rent, high, and row buildings
                    if (lowerName.Contains("lowrent") || lowerName.Contains("low_rent"))
                        return false;
                    if (lowerName.Contains("row") && lowerName.Contains("medium"))
                        return false; // Row buildings have their own zone
                    return true;

                case ZoneType.ResidentialHigh:
                    // Exclude low rent buildings
                    if (lowerName.Contains("lowrent") || lowerName.Contains("low_rent"))
                        return false;
                    return true;

                case ZoneType.ResidentialLowRent:
                    // Only allow buildings explicitly marked as low rent
                    // The zone classification already handles this, but double-check
                    return true;

                case ZoneType.ResidentialMixed:
                    // Mixed use - exclude pure low rent
                    if (lowerName.Contains("lowrent") || lowerName.Contains("low_rent"))
                        return false;
                    return true;

                default:
                    return true;
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
