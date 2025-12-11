using System;
using Colossal.Logging;
using Game;
using Game.Prefabs;
using Game.Zones;
using Unity.Collections;
using Unity.Entities;

namespace UniversalZoningSystem
{
    /// <summary>
    /// Debug utilities for the Universal Zoning System.
    /// Provides diagnostic output and testing functions.
    /// </summary>
    public partial class DebugSystem : GameSystemBase
    {
        private static readonly ILog Log = Mod.Log;

        private PrefabSystem _prefabSystem;
        private UniversalZonePrefabSystem _zonePrefabSystem;
        private UniversalZoneUISystem _zoneUISystem;
        private EntityQuery _buildingQuery;
        private bool _hasLoggedDiagnostics;
        private int _frameDelay;

        protected override void OnCreate()
        {
            base.OnCreate();
            _prefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();
            _zonePrefabSystem = World.GetOrCreateSystemManaged<UniversalZonePrefabSystem>();
            _zoneUISystem = World.GetOrCreateSystemManaged<UniversalZoneUISystem>();

            _buildingQuery = GetEntityQuery(
                ComponentType.ReadOnly<BuildingData>(),
                ComponentType.ReadOnly<SpawnableBuildingData>(),
                ComponentType.ReadOnly<PrefabData>()
            );

            Enabled = true; // Enable for diagnostic logging
        }

        protected override void OnUpdate()
        {
            if (_hasLoggedDiagnostics)
            {
                Enabled = false;
                return;
            }

            _frameDelay++;
            
            // Wait a long time to ensure all systems have run
            // Building duplication takes several minutes!
            if (_frameDelay < 500)
                return;

            // Also wait for zone UI system to have created zones
            if (_zoneUISystem == null || _zoneUISystem.CreatedZonePrefabs.Count == 0)
                return;

            // Wait for data to be loaded
            if (_buildingQuery.IsEmptyIgnoreFilter)
                return;

            try
            {
                VerifyUniversalZoneBuildings();
                _hasLoggedDiagnostics = true;
            }
            catch (Exception ex)
            {
                Log.Error($"Error in debug verification: {ex.Message}\n{ex.StackTrace}");
                _hasLoggedDiagnostics = true;
            }
        }

        private void VerifyUniversalZoneBuildings()
        {
            Log.Info("=== Verifying Universal Zone Building Registration ===");

            var buildingEntities = _buildingQuery.ToEntityArray(Allocator.Temp);
            var spawnableDataLookup = GetComponentLookup<SpawnableBuildingData>(true);

            try
            {
                // For each universal zone, count how many buildings reference it
                foreach (var definition in ZoneDefinitions.AllZones)
                {
                    var zoneEntity = _zoneUISystem.GetUniversalZoneEntity(definition.Id);
                    if (zoneEntity == Entity.Null)
                    {
                        Log.Info($"  {definition.Id}: Zone entity not found");
                        continue;
                    }

                    // Log all components on the zone entity
                    Log.Info($"  {definition.Id} (Entity: {zoneEntity.Index}) components:");
                    
                    if (EntityManager.HasComponent<ZoneData>(zoneEntity))
                    {
                        var zd = EntityManager.GetComponentData<ZoneData>(zoneEntity);
                        Log.Info($"    ZoneData: AreaType={zd.m_AreaType}, ZoneType={zd.m_ZoneType}, Flags={zd.m_ZoneFlags}");
                    }
                    else
                    {
                        Log.Warn($"    NO ZoneData!");
                    }

                    if (EntityManager.HasComponent<PrefabData>(zoneEntity))
                    {
                        Log.Info($"    Has PrefabData");
                    }

                    // Check for any buffer components
                    var componentTypes = EntityManager.GetComponentTypes(zoneEntity);
                    Log.Info($"    All components ({componentTypes.Length}):");
                    foreach (var ct in componentTypes)
                    {
                        Log.Info($"      - {ct.GetManagedType()?.Name ?? ct.ToString()}");
                    }
                    componentTypes.Dispose();

                    int matchingBuildings = 0;
                    int totalChecked = 0;

                    foreach (var buildingEntity in buildingEntities)
                    {
                        if (!spawnableDataLookup.HasComponent(buildingEntity))
                            continue;

                        totalChecked++;
                        var spawnData = spawnableDataLookup[buildingEntity];
                        
                        if (spawnData.m_ZonePrefab == zoneEntity)
                        {
                            matchingBuildings++;
                            
                            // Log first few matches
                            if (matchingBuildings <= 3)
                            {
                                if (_prefabSystem.TryGetPrefab<BuildingPrefab>(buildingEntity, out var prefab))
                                {
                                    Log.Info($"    Match: {prefab.name} (Entity: {buildingEntity.Index})");
                                }
                            }
                        }
                    }

                    Log.Info($"  {definition.Id}: {matchingBuildings} buildings reference this zone (checked {totalChecked})");
                }
            }
            finally
            {
                buildingEntities.Dispose();
            }

            Log.Info("=== End Verification ===");
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
        }
    }
}
