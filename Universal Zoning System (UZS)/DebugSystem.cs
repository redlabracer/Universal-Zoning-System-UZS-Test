using System;
using System.IO;
using System.Text;
using Colossal.Logging;
using Game;
using Game.Prefabs;
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
        private UniversalZoningSystem _universalZoningSystem;
        private UniversalZonePrefabSystem _zonePrefabSystem;
        private EntityQuery _buildingQuery;
        private bool _hasLoggedDiagnostics;

        protected override void OnCreate()
        {
            base.OnCreate();
            _prefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();
            _universalZoningSystem = World.GetOrCreateSystemManaged<UniversalZoningSystem>();
            _zonePrefabSystem = World.GetOrCreateSystemManaged<UniversalZonePrefabSystem>();

            _buildingQuery = GetEntityQuery(
                ComponentType.ReadOnly<BuildingData>(),
                ComponentType.ReadOnly<SpawnableBuildingData>(),
                ComponentType.ReadOnly<PrefabData>()
            );

            // Only enable this system if verbose logging is enabled
            Enabled = false;
        }

        protected override void OnUpdate()
        {
            if (_hasLoggedDiagnostics)
            {
                Enabled = false;
                return;
            }

            // Check if verbose logging is enabled
            if (Mod.Settings == null || !Mod.Settings.EnableVerboseLogging)
            {
                Enabled = false;
                return;
            }

            // Wait for data to be loaded
            if (_buildingQuery.IsEmptyIgnoreFilter)
                return;

            try
            {
                LogDiagnostics();
                _hasLoggedDiagnostics = true;
            }
            catch (Exception ex)
            {
                Log.Error($"Error logging diagnostics: {ex.Message}");
                _hasLoggedDiagnostics = true;
            }
        }

        private void LogDiagnostics()
        {
            Log.Info("=== Universal Zoning System Diagnostics ===");

            // Log building statistics
            var spawnableDataLookup = GetComponentLookup<SpawnableBuildingData>(true);
            var zoneDataLookup = GetComponentLookup<ZoneData>(true);

            var collection = BuildingCollector.CollectBuildings(
                _buildingQuery,
                spawnableDataLookup,
                zoneDataLookup,
                _prefabSystem
            );

            collection.LogStatistics(Log);

            // Log zone templates
            Log.Info("Zone Templates:");
            foreach (var kvp in _zonePrefabSystem.ZoneTemplates)
            {
                Log.Info($"  {kvp.Key}");
            }

            // Log enabled regions from settings
            if (Mod.Settings != null)
            {
                Log.Info("Enabled Region Prefixes:");
                foreach (var prefix in Mod.Settings.GetEnabledRegionPrefixes())
                {
                    Log.Info($"  {prefix}");
                }
            }

            Log.Info("=== End Diagnostics ===");
        }

        /// <summary>
        /// Exports a full building report to a file.
        /// </summary>
        public void ExportBuildingReport(string filePath)
        {
            try
            {
                var spawnableDataLookup = GetComponentLookup<SpawnableBuildingData>(true);
                var zoneDataLookup = GetComponentLookup<ZoneData>(true);

                var collection = BuildingCollector.CollectBuildings(
                    _buildingQuery,
                    spawnableDataLookup,
                    zoneDataLookup,
                    _prefabSystem
                );

                var sb = new StringBuilder();
                sb.AppendLine("Universal Zoning System - Building Report");
                sb.AppendLine($"Generated: {DateTime.Now}");
                sb.AppendLine($"Total Buildings: {collection.TotalCount}");
                sb.AppendLine();

                sb.AppendLine("=== Buildings by Region ===");
                foreach (var region in collection.GetRegions())
                {
                    var buildings = collection.GetBuildingsByRegion(region);
                    sb.AppendLine($"\n{region} ({buildings.Count} buildings):");
                    foreach (var building in buildings)
                    {
                        sb.AppendLine($"  - {building.Prefab.name} [{building.ZoneType}]");
                    }
                }

                sb.AppendLine("\n=== Buildings by Zone Type ===");
                foreach (var zoneType in collection.GetZoneTypes())
                {
                    var buildings = collection.GetBuildingsByZoneType(zoneType);
                    sb.AppendLine($"\n{zoneType} ({buildings.Count} buildings):");
                    foreach (var building in buildings)
                    {
                        sb.AppendLine($"  - {building.Prefab.name} [{building.Region}]");
                    }
                }

                File.WriteAllText(filePath, sb.ToString());
                Log.Info($"Building report exported to: {filePath}");
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to export building report: {ex.Message}");
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
        }
    }
}
