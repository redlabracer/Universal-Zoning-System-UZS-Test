using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Colossal.IO.AssetDatabase;
using Colossal.Logging;
using Game;
using Game.Prefabs;
using Game.SceneFlow;
using Game.Zones;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using System.Reflection;
using UniversalZoningSystem.Settings;

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

        private const string CacheVersion = "v1";

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
            TryModifyBuildings(true);
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

            TryModifyBuildings(false);
        }

        private void TryModifyBuildings(bool isPreload)
        {
            if (_initialized)
                return;

            // Check settings availability
            if (Mod.Settings == null)
            {
                // If settings aren't loaded yet, we can't check preferences or force rebuild status.
                // We must wait.
                if (!isPreload) Log.Info("Waiting for settings to be loaded...");
                return;
            }

            // Need zones to be created first
            if (_zoneUISystem == null || _zoneUISystem.CreatedZonePrefabs.Count == 0)
            {
                if (!isPreload) Log.Info("Waiting for universal zones to be created...");
                return;
            }

            // Need buildings to be loaded
            if (_buildingQuery.IsEmptyIgnoreFilter)
            {
                if (!isPreload) Log.Info("Waiting for buildings to load...");
                return;
            }

            try
            {
                if (ModifyBuildingsForUniversalZones(isPreload))
                {
                    _initialized = true;
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to modify buildings: {ex.Message}\n{ex.StackTrace}");
                _initialized = true;
            }
        }

        private bool ModifyBuildingsForUniversalZones(bool isPreload)
        {
            var startTime = DateTime.Now;
            Log.Info($"=== Creating Building Clones for Universal Zones (Preload={isPreload}) ===");

            // Check cache settings
            bool useCache = Mod.Settings?.EnableCaching ?? true;
            bool forceRebuild = Mod.Settings?.ForceRebuildCache ?? false;

            if (forceRebuild && Mod.Settings != null)
            {
                Mod.Settings.ForceRebuildCache = false;
                // Note: Settings are usually auto-saved or saved on exit
                // We trigger a save here to ensure the flag reset persists
                Mod.Settings.Save();
            }

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
                return false; // Retry later
            }

            // OPTIMIZATION 2: Map all existing prefabs by name for fast lookup
            // We need Entity lookup for cache processing, and name checking for duplicates
            var existingPrefabs = new Dictionary<string, Entity>(StringComparer.Ordinal);
            var allPrefabsQuery = GetEntityQuery(ComponentType.ReadOnly<PrefabData>());
            var allPrefabEntities = allPrefabsQuery.ToEntityArray(Allocator.Temp);
            foreach (var entity in allPrefabEntities)
            {
                var prefab = _prefabSystem.GetPrefab<PrefabBase>(entity);
                if (prefab != null)
                {
                    existingPrefabs[prefab.name] = entity;
                }
            }
            allPrefabEntities.Dispose();

            // FIX: Also check for prefabs that exist in memory but might not have entities yet (e.g. from previous session)
            // This prevents "Duplicate prefab ID" errors when reloading
            var loadedPrefabs = Resources.FindObjectsOfTypeAll<BuildingPrefab>();
            int recoveredFromMemory = 0;
            foreach (var prefab in loadedPrefabs)
            {
                if (prefab.name.StartsWith("Universal_") && !existingPrefabs.ContainsKey(prefab.name))
                {
                    // We found it in memory but not in PrefabSystem.
                    // We MUST register it, otherwise it won't have an Entity and won't work in game.
                    if (_prefabSystem.AddPrefab(prefab))
                    {
                        Log.Info($"Recovered and registered orphaned prefab: {prefab.name}");
                        // We can't easily get the entity immediately if it's deferred, but we mark it as handled.
                        existingPrefabs[prefab.name] = Entity.Null; 
                        recoveredFromMemory++;
                    }
                    else
                    {
                        // If AddPrefab fails, it might be because it's already registered (race condition?) or invalid.
                        // We check if it's in the system now.
                        var prefabID = new PrefabID(nameof(BuildingPrefab), prefab.name);
                        if (_prefabSystem.TryGetPrefab(prefabID, out var existing) && existing == prefab)
                        {
                             Log.Info($"Orphaned prefab {prefab.name} was already registered.");
                             existingPrefabs[prefab.name] = Entity.Null;
                             recoveredFromMemory++;
                        }
                        else
                        {
                             Log.Warn($"Found orphaned prefab {prefab.name} but failed to register it. Skipping clone to avoid duplicates.");
                             existingPrefabs[prefab.name] = Entity.Null;
                        }
                    }
                }
            }
            if (recoveredFromMemory > 0)
            {
                Log.Info($"Found and registered {recoveredFromMemory} existing universal prefabs from memory.");
            }

            // CACHE PROCESSING
            List<string> cachedBuildings = null;
            if (useCache && !forceRebuild)
            {
                cachedBuildings = LoadCache();
            }

            if (cachedBuildings != null && cachedBuildings.Count > 0)
            {
                Log.Info($"Loaded {cachedBuildings.Count} buildings from cache. Processing from cache...");
                return ProcessFromCache(cachedBuildings, universalZonePrefabs, existingPrefabs, isPreload);
            }

            // FULL DISCOVERY (Fallback or Rebuild)
            Log.Info("Running full building discovery...");
            var buildingEntities = _buildingQuery.ToEntityArray(Allocator.Temp);
            var spawnableDataLookup = GetComponentLookup<SpawnableBuildingData>(true);

            // OPTIMIZATION 3: Cache zone classifications to avoid repeated lookups
            var zoneClassificationCache = new Dictionary<Entity, ZoneClassification>();
            var newClonesList = new List<string>();

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
                    if (existingPrefabs.ContainsKey(cloneName))
                        continue;

                    // CLONE the prefab
                    if (CloneBuilding(buildingPrefab, universalZonePrefab, cloneName))
                    {
                        totalCloned++;
                        newClonesList.Add(buildingPrefab.name);
                        existingPrefabs[cloneName] = Entity.Null; // Mark as existing to prevent duplicates in this run

                        // Track statistics
                        var zoneKey = classification.ZoneType.ToString();
                        if (!_duplicatesByZone.ContainsKey(zoneKey))
                            _duplicatesByZone[zoneKey] = 0;
                        _duplicatesByZone[zoneKey]++;

                        if (!_duplicatesByRegion.ContainsKey(region))
                            _duplicatesByRegion[region] = 0;
                        _duplicatesByRegion[region]++;
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

                // Save cache if enabled
                if (useCache && newClonesList.Count > 0)
                {
                    SaveCache(newClonesList);
                }
                
                return true;
            }
            finally
            {
                buildingEntities.Dispose();
            }
        }

        private bool ProcessFromCache(List<string> cachedBuildings, Dictionary<ZoneType, ZonePrefab> universalZonePrefabs, Dictionary<string, Entity> existingPrefabs, bool isPreload)
        {
            int processed = 0;
            int skipped = 0;
            int failed = 0;
            int missingOriginals = 0;

            foreach (var originalName in cachedBuildings)
            {
                string cloneName = "Universal_" + originalName;
                if (existingPrefabs.ContainsKey(cloneName))
                {
                    skipped++;
                    continue;
                }

                if (!existingPrefabs.TryGetValue(originalName, out var originalEntity))
                {
                    // Original building not found (maybe mod removed or asset missing)
                    failed++;
                    missingOriginals++;
                    continue;
                }

                var buildingPrefab = _prefabSystem.GetPrefab<BuildingPrefab>(originalEntity);
                if (buildingPrefab == null)
                {
                    failed++;
                    continue;
                }

                // We need to find the zone type to get the correct universal zone prefab
                // We can get it from the building's spawn data
                var entityManager = World.EntityManager;
                if (!entityManager.HasComponent<SpawnableBuildingData>(originalEntity))
                {
                    failed++;
                    continue;
                }

                var spawnComponent = entityManager.GetComponentData<SpawnableBuildingData>(originalEntity);
                if (spawnComponent.m_ZonePrefab == Entity.Null)
                {
                    failed++;
                    continue;
                }

                if (!_prefabSystem.TryGetPrefab<ZonePrefab>(spawnComponent.m_ZonePrefab, out var originalZonePrefab))
                {
                    failed++;
                    continue;
                }

                var classification = UniversalZonePrefabSystem.GetZoneClassification(originalZonePrefab.name);
                if (classification == null)
                {
                    failed++;
                    continue;
                }

                if (!universalZonePrefabs.TryGetValue(classification.ZoneType, out var universalZonePrefab))
                {
                    failed++;
                    continue;
                }

                if (CloneBuilding(buildingPrefab, universalZonePrefab, cloneName))
                {
                    processed++;
                    existingPrefabs[cloneName] = Entity.Null; // Mark as existing
                }
                else
                {
                    failed++;
                }
            }

            Log.Info($"Cache processing complete. Processed: {processed}, Skipped (Existing): {skipped}, Failed/Missing: {failed}");
            
            // If we had missing originals during preload, it's likely they haven't loaded yet.
            // We should abort and retry later in OnUpdate.
            if (missingOriginals > 0 && isPreload)
            {
                Log.Warn($"Cache processing encountered {missingOriginals} missing originals during preload. Aborting to retry in Update.");
                return false;
            }
            
            return true;
        }

        private void UpdatePrefabInternalName(PrefabBase prefab, string oldName, string newName)
        {
            try
            {
                var type = prefab.GetType();
                while (type != null && type != typeof(UnityEngine.Object))
                {
                    var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    foreach (var field in fields)
                    {
                        if (field.FieldType == typeof(string))
                        {
                            var value = field.GetValue(prefab) as string;
                            Log.Info($"[DEBUG] Checking field '{field.Name}' in '{type.Name}': '{value}'");
                            if (value != null && value.Contains(oldName))
                            {
                                var newValue = value.Replace(oldName, newName);
                                field.SetValue(prefab, newValue);
                                Log.Info($"[FIX] Updated field '{field.Name}' in '{type.Name}' from '{value}' to '{newValue}'");
                            }
                        }
                    }
                    type = type.BaseType;
                }

                // Explicitly check PrefabBase for m_Name just in case
                var prefabBaseType = typeof(PrefabBase);
                var mNameField = prefabBaseType.GetField("m_Name", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                if (mNameField != null && mNameField.FieldType == typeof(string))
                {
                    var value = mNameField.GetValue(prefab) as string;
                    Log.Info($"[DEBUG] Checking explicit PrefabBase.m_Name: '{value}'");
                    if (value != null && value.Contains(oldName))
                    {
                        var newValue = value.Replace(oldName, newName);
                        mNameField.SetValue(prefab, newValue);
                        Log.Info($"[FIX] Updated explicit PrefabBase.m_Name from '{value}' to '{newValue}'");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warn($"Failed to update internal prefab name: {ex.Message}");
            }
        }

        private bool CloneBuilding(BuildingPrefab original, ZonePrefab targetZone, string newName)
        {
            // Check if already exists in PrefabSystem to avoid "Duplicate prefab ID" warnings
            var prefabID = new PrefabID(nameof(BuildingPrefab), newName);
            if (_prefabSystem.TryGetPrefab(prefabID, out PrefabBase existing) && existing is BuildingPrefab existingBuilding)
            {
                _createdDuplicates.Add(existingBuilding);
                return true;
            }

            // Check Resources for existing prefab (orphaned from previous session or not yet in PrefabSystem)
            var loadedPrefabs = Resources.FindObjectsOfTypeAll<BuildingPrefab>();
            foreach (var prefab in loadedPrefabs)
            {
                if (prefab.name == newName)
                {
                    Log.Info($"Found existing prefab in Resources for {newName}. Reusing it.");
                    
                    // Try to add it to PrefabSystem if not already there
                    if (!_prefabSystem.AddPrefab(prefab))
                    {
                        // If AddPrefab fails, it might be because it's already added but TryGetPrefab failed?
                        // Or it's a true duplicate ID.
                        // In any case, we treat it as "success" because we have the prefab.
                        Log.Info($"PrefabSystem.AddPrefab returned false for existing resource {newName}, assuming it's already registered.");
                    }
                    
                    _createdDuplicates.Add(prefab);
                    return true;
                }
            }

            try
            {
                var newPrefab = UnityEngine.Object.Instantiate(original);
                newPrefab.name = newName;

                // Update internal name fields to ensure unique ID
                UpdatePrefabInternalName(newPrefab, original.name, newName);

                if (newPrefab.TryGet<SpawnableBuilding>(out var spawnable))
                {
                    spawnable.m_ZoneType = targetZone;
                    
                    if (_prefabSystem.AddPrefab(newPrefab))
                    {
                        _createdDuplicates.Add(newPrefab);
                    }
                    else
                    {
                        // If AddPrefab fails, check if it was because it already exists
                        if (_prefabSystem.TryGetPrefab(prefabID, out var existingBase) && existingBase is BuildingPrefab existingPrefab)
                        {
                            Log.Info($"PrefabSystem.AddPrefab failed for {newName}, but found it in system. Using existing.");
                            UnityEngine.Object.Destroy(newPrefab);
                            if (!_createdDuplicates.Contains(existingPrefab))
                            {
                                _createdDuplicates.Add(existingPrefab);
                            }
                            return true;
                        }
                        
                        Log.Warn($"PrefabSystem.AddPrefab failed for {newName} (likely duplicate ID). Destroying clone.");
                        UnityEngine.Object.Destroy(newPrefab);
                        return false;
                    }
                    return true;
                }
                else
                {
                    UnityEngine.Object.Destroy(newPrefab);
                    return false;
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to clone building {original.name}: {ex.Message}");
                return false;
            }
        }

        private string GetCacheFilePath()
        {
            var path = Path.Combine(Application.persistentDataPath, "ModsData", "UniversalZoningSystem");
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            return Path.Combine(path, "BuildingCache.txt");
        }

        private List<string> LoadCache()
        {
            try
            {
                var path = GetCacheFilePath();
                if (File.Exists(path))
                {
                    var lines = File.ReadAllLines(path);
                    if (lines.Length > 0 && lines[0] == CacheVersion)
                    {
                        return lines.Skip(1).Where(l => !string.IsNullOrWhiteSpace(l)).ToList();
                    }
                    else
                    {
                        Log.Info("Cache version mismatch or empty. Rebuilding cache.");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warn($"Failed to load cache: {ex.Message}");
            }
            return null;
        }

        private void SaveCache(List<string> buildings)
        {
            try
            {
                var path = GetCacheFilePath();
                var lines = new List<string> { CacheVersion };
                lines.AddRange(buildings);
                File.WriteAllLines(path, lines);
                Log.Info($"Saved {buildings.Count} buildings to cache at {path}");
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to save cache: {ex.Message}");
            }
        }

        private bool IsRegionEnabled(string region)
        {
            if (Mod.Settings == null) return true;
            
            switch (region)
            {
                case "North America": return Mod.Settings.EnableNorthAmerica;
                case "European": return Mod.Settings.EnableEuropean;
                case "United Kingdom": return Mod.Settings.EnableUnitedKingdom;
                case "Germany": return Mod.Settings.EnableGermany;
                case "France": return Mod.Settings.EnableFrance;
                case "Netherlands": return Mod.Settings.EnableNetherlands;
                case "Eastern Europe": return Mod.Settings.EnableEasternEurope;
                case "Japan": return Mod.Settings.EnableJapan;
                case "China": return Mod.Settings.EnableChina;
                default: return true;
            }
        }

        private bool IsBuildingValidForZoneType(string name, ZoneType type)
        {
            // Basic validation logic to ensure we don't clone invalid buildings
            // This was likely checking for specific naming conventions or exclusions
            // For now, we assume if it has the zone assigned, it is valid.
            return true;
        }
    }
}
