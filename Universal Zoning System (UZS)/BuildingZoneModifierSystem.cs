using System;
using System.Collections.Generic;
using System.Linq;
using Colossal.Logging;
using Game;
using Game.Prefabs;
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

            try
            {
                CreateBuildingDuplicatesForUniversalZones();
                _initialized = true;
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to create building duplicates: {ex.Message}\n{ex.StackTrace}");
                _initialized = true;
            }
        }

        private void CreateBuildingDuplicatesForUniversalZones()
        {
            Log.Info("=== Creating Building Duplicates for Universal Zones ===");
            Log.Info("Original buildings will NOT be modified - only creating duplicates.");

            var buildingEntities = _buildingQuery.ToEntityArray(Allocator.Temp);
            var spawnableDataLookup = GetComponentLookup<SpawnableBuildingData>(true);

            try
            {
                // For each universal zone definition
                foreach (var definition in ZoneDefinitions.AllZones)
                {
                    // Get the universal zone prefab
                    var universalZonePrefab = _zoneUISystem.GetUniversalZonePrefab(definition.Id);
                    if (universalZonePrefab == null)
                    {
                        Log.Warn($"Universal zone prefab not found: {definition.Id}");
                        continue;
                    }

                    int duplicatesCreated = 0;
                    var regionCounts = new Dictionary<string, int>();

                    // Find all buildings that match this zone type
                    foreach (var buildingEntity in buildingEntities)
                    {
                        if (!spawnableDataLookup.HasComponent(buildingEntity))
                            continue;

                        var spawnData = spawnableDataLookup[buildingEntity];
                        if (spawnData.m_ZonePrefab == Entity.Null)
                            continue;

                        // Get the building prefab
                        if (!_prefabSystem.TryGetPrefab<BuildingPrefab>(buildingEntity, out var originalBuilding))
                            continue;

                        // Skip if this is already a UZS duplicate
                        if (originalBuilding.name.EndsWith("_UZS"))
                            continue;

                        // Get the original zone prefab
                        if (!_prefabSystem.TryGetPrefab<ZonePrefab>(spawnData.m_ZonePrefab, out var originalZonePrefab))
                            continue;

                        // Check if zone type matches
                        var classification = UniversalZonePrefabSystem.GetZoneClassification(originalZonePrefab.name);
                        if (classification == null)
                            continue;

                        bool matchesDefinition = definition.SourceZoneTypes.Contains(classification.ZoneType);
                        if (!matchesDefinition)
                            continue;

                        // Check region
                        var region = RegionPrefixManager.GetRegionFromPrefabName(originalBuilding.name);
                        if (!IsRegionEnabled(region))
                            continue;

                        // Create a duplicate building that references the universal zone
                        var duplicate = CreateBuildingDuplicate(originalBuilding, universalZonePrefab, definition.Id);
                        if (duplicate != null)
                        {
                            _prefabSystem.AddPrefab(duplicate);
                            _createdDuplicates.Add(duplicate);
                            
                            // Verify the entity was created correctly
                            var duplicateEntity = _prefabSystem.GetEntity(duplicate);
                            if (duplicateEntity != Entity.Null)
                            {
                                // Check if SpawnableBuildingData was set correctly
                                if (EntityManager.HasComponent<SpawnableBuildingData>(duplicateEntity))
                                {
                                    var dupSpawnData = EntityManager.GetComponentData<SpawnableBuildingData>(duplicateEntity);
                                    var expectedZoneEntity = _prefabSystem.GetEntity(universalZonePrefab);
                                    
                                    if (dupSpawnData.m_ZonePrefab != expectedZoneEntity)
                                    {
                                        // Fix the zone reference on the entity
                                        dupSpawnData.m_ZonePrefab = expectedZoneEntity;
                                        EntityManager.SetComponentData(duplicateEntity, dupSpawnData);
                                        
                                        if (Mod.Settings?.EnableVerboseLogging == true)
                                        {
                                            Log.Info($"  Fixed zone reference for {duplicate.name}: {dupSpawnData.m_ZonePrefab.Index} -> {expectedZoneEntity.Index}");
                                        }
                                    }
                                }
                            }
                            
                            duplicatesCreated++;

                            if (!regionCounts.ContainsKey(region))
                                regionCounts[region] = 0;
                            regionCounts[region]++;

                            if (!_duplicatesByRegion.ContainsKey(region))
                                _duplicatesByRegion[region] = 0;
                            _duplicatesByRegion[region]++;
                        }
                    }

                    if (duplicatesCreated > 0)
                    {
                        _duplicatesByZone[definition.Id] = duplicatesCreated;
                        
                        var regionBreakdown = regionCounts
                            .OrderByDescending(x => x.Value)
                            .Select(x => $"{x.Key}:{x.Value}")
                            .ToList();

                        Log.Info($"Created {duplicatesCreated} duplicates for {definition.Name} [{string.Join(", ", regionBreakdown)}]");
                    }
                }

                LogResults();
            }
            finally
            {
                buildingEntities.Dispose();
            }
        }

        private BuildingPrefab CreateBuildingDuplicate(BuildingPrefab original, ZonePrefab targetZone, string zoneId)
        {
            try
            {
                // Clone the entire prefab
                var duplicate = UnityEngine.Object.Instantiate(original);
                duplicate.name = $"{original.name}_UZS";

                // Find and update the SpawnableBuilding component to reference our universal zone
                var spawnableBuilding = duplicate.GetComponent<SpawnableBuilding>();
                if (spawnableBuilding != null)
                {
                    spawnableBuilding.m_ZoneType = targetZone;
                }
                else
                {
                    // If no SpawnableBuilding component, create one
                    spawnableBuilding = ScriptableObject.CreateInstance<SpawnableBuilding>();
                    spawnableBuilding.m_ZoneType = targetZone;
                    
                    if (duplicate.components == null)
                        duplicate.components = new List<ComponentBase>();
                    
                    duplicate.components.Add(spawnableBuilding);
                }

                // Remove theme restrictions so buildings spawn in all themes
                var themeObject = duplicate.GetComponent<ThemeObject>();
                if (themeObject != null)
                {
                    duplicate.components.Remove(themeObject);
                    UnityEngine.Object.Destroy(themeObject);
                }

                if (Mod.Settings?.EnableVerboseLogging == true)
                {
                    Log.Info($"  Created duplicate: {duplicate.name} -> {targetZone.name}");
                }

                return duplicate;
            }
            catch (Exception ex)
            {
                Log.Warn($"Failed to duplicate {original.name}: {ex.Message}");
                return null;
            }
        }

        private void LogResults()
        {
            Log.Info("=== Building Duplication Complete ===");
            Log.Info($"Total duplicates created: {_createdDuplicates.Count}");
            Log.Info("Original buildings are UNCHANGED - original zones work normally.");

            if (_duplicatesByZone.Count > 0)
            {
                Log.Info("Duplicates by Universal Zone:");
                foreach (var kvp in _duplicatesByZone.OrderByDescending(x => x.Value))
                {
                    Log.Info($"  {kvp.Key}: {kvp.Value} buildings");
                }
            }

            if (_duplicatesByRegion.Count > 0)
            {
                Log.Info("Duplicates by Region:");
                foreach (var kvp in _duplicatesByRegion.OrderByDescending(x => x.Value))
                {
                    Log.Info($"  {kvp.Key}: {kvp.Value} buildings");
                }
            }

            // Verify buildings are correctly linked to universal zones
            VerifyBuildingZoneLinks();
        }

        private void VerifyBuildingZoneLinks()
        {
            Log.Info("=== Verifying Building-Zone Links ===");
            
            var spawnableDataLookup = GetComponentLookup<SpawnableBuildingData>(true);
            
            foreach (var definition in ZoneDefinitions.AllZones)
            {
                var zoneEntity = _zoneUISystem.GetUniversalZoneEntity(definition.Id);
                if (zoneEntity == Entity.Null)
                    continue;

                int linkedBuildings = 0;
                
                foreach (var duplicate in _createdDuplicates)
                {
                    var entity = _prefabSystem.GetEntity(duplicate);
                    if (entity == Entity.Null)
                        continue;

                    if (!spawnableDataLookup.HasComponent(entity))
                        continue;

                    var spawnData = spawnableDataLookup[entity];
                    if (spawnData.m_ZonePrefab == zoneEntity)
                    {
                        linkedBuildings++;
                    }
                }

                Log.Info($"  {definition.Id} (Entity {zoneEntity.Index}): {linkedBuildings} buildings linked");
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
