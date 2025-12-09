using System;
using System.Collections.Generic;
using System.Linq;
using Colossal.Logging;
using Game.Prefabs;
using Game.Zones;
using Unity.Collections;
using Unity.Entities;

namespace UniversalZoningSystem
{
    /// <summary>
    /// Utility class for collecting and organizing building prefabs from all regions.
    /// </summary>
    public class BuildingCollector
    {
        private static readonly ILog Log = Mod.Log;

        /// <summary>
        /// Collects all spawnable buildings and organizes them by region and zone type.
        /// </summary>
        public static BuildingCollection CollectBuildings(
            EntityQuery buildingQuery,
            ComponentLookup<SpawnableBuildingData> spawnableDataLookup,
            ComponentLookup<ZoneData> zoneDataLookup,
            PrefabSystem prefabSystem)
        {
            var collection = new BuildingCollection();
            var buildingEntities = buildingQuery.ToEntityArray(Allocator.Temp);

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

                    if (!prefabSystem.TryGetPrefab<BuildingPrefab>(entity, out var buildingPrefab))
                        continue;

                    // Get region from prefab name
                    var region = RegionPrefixManager.GetRegionFromPrefabName(buildingPrefab.name);
                    var regionInfo = RegionPrefixManager.GetRegionInfo(buildingPrefab.name);

                    // Classify zone type
                    var zoneType = ClassifyZoneType(zoneData);

                    // Add to collection
                    collection.AddBuilding(buildingPrefab, region, zoneType, regionInfo);
                }
            }
            finally
            {
                buildingEntities.Dispose();
            }

            return collection;
        }

        private static ZoneType ClassifyZoneType(ZoneData zoneData)
        {
            var areaType = zoneData.m_AreaType;
            var zoneFlags = zoneData.m_ZoneFlags;

            if (areaType == AreaType.Residential)
            {
                // Check for row housing
                if ((zoneFlags & ZoneFlags.SupportNarrow) != 0)
                    return ZoneType.ResidentialRow;

                // Determine density - this may need to be refined based on actual game data
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
    }

    /// <summary>
    /// Collection of buildings organized by region and zone type.
    /// </summary>
    public class BuildingCollection
    {
        private readonly Dictionary<string, List<BuildingEntry>> _buildingsByRegion = new Dictionary<string, List<BuildingEntry>>();
        private readonly Dictionary<ZoneType, List<BuildingEntry>> _buildingsByZoneType = new Dictionary<ZoneType, List<BuildingEntry>>();
        private readonly List<BuildingEntry> _allBuildings = new List<BuildingEntry>();

        public void AddBuilding(BuildingPrefab prefab, string region, ZoneType zoneType, RegionInfo regionInfo)
        {
            var entry = new BuildingEntry(prefab, region, zoneType, regionInfo);
            _allBuildings.Add(entry);

            // Add to region dictionary
            if (!_buildingsByRegion.ContainsKey(region))
                _buildingsByRegion[region] = new List<BuildingEntry>();
            _buildingsByRegion[region].Add(entry);

            // Add to zone type dictionary
            if (!_buildingsByZoneType.ContainsKey(zoneType))
                _buildingsByZoneType[zoneType] = new List<BuildingEntry>();
            _buildingsByZoneType[zoneType].Add(entry);
        }

        /// <summary>
        /// Gets all buildings from a specific region.
        /// </summary>
        public IReadOnlyList<BuildingEntry> GetBuildingsByRegion(string region)
        {
            if (_buildingsByRegion.TryGetValue(region, out var buildings))
                return buildings;
            return Array.Empty<BuildingEntry>();
        }

        /// <summary>
        /// Gets all buildings of a specific zone type.
        /// </summary>
        public IReadOnlyList<BuildingEntry> GetBuildingsByZoneType(ZoneType zoneType)
        {
            if (_buildingsByZoneType.TryGetValue(zoneType, out var buildings))
                return buildings;
            return Array.Empty<BuildingEntry>();
        }

        /// <summary>
        /// Gets all buildings of a specific zone type from enabled regions only.
        /// </summary>
        public IEnumerable<BuildingEntry> GetBuildingsForUniversalZone(ZoneType zoneType, IEnumerable<string> enabledRegionPrefixes)
        {
            var prefixSet = new HashSet<string>(enabledRegionPrefixes.Select(p => p.ToUpperInvariant()));

            if (!_buildingsByZoneType.TryGetValue(zoneType, out var buildings))
                yield break;

            foreach (var building in buildings)
            {
                // Check if building matches any enabled region prefix
                var upperName = building.Prefab.name.ToUpperInvariant();
                bool isEnabled = prefixSet.Count == 0; // If no prefixes specified, include all

                foreach (var prefix in prefixSet)
                {
                    if (upperName.StartsWith(prefix))
                    {
                        isEnabled = true;
                        break;
                    }
                }

                // Also include "Generic" buildings
                if (building.Region == "Generic")
                    isEnabled = true;

                if (isEnabled)
                    yield return building;
            }
        }

        /// <summary>
        /// Gets all regions that have buildings.
        /// </summary>
        public IEnumerable<string> GetRegions() => _buildingsByRegion.Keys;

        /// <summary>
        /// Gets all zone types that have buildings.
        /// </summary>
        public IEnumerable<ZoneType> GetZoneTypes() => _buildingsByZoneType.Keys;

        /// <summary>
        /// Gets the total number of buildings.
        /// </summary>
        public int TotalCount => _allBuildings.Count;

        /// <summary>
        /// Gets all buildings.
        /// </summary>
        public IReadOnlyList<BuildingEntry> AllBuildings => _allBuildings;

        /// <summary>
        /// Logs statistics about the collection.
        /// </summary>
        public void LogStatistics(ILog log)
        {
            log.Info($"Building Collection Statistics:");
            log.Info($"  Total buildings: {TotalCount}");

            log.Info($"  By Region:");
            foreach (var kvp in _buildingsByRegion.OrderByDescending(x => x.Value.Count))
            {
                log.Info($"    {kvp.Key}: {kvp.Value.Count}");
            }

            log.Info($"  By Zone Type:");
            foreach (var kvp in _buildingsByZoneType.OrderByDescending(x => x.Value.Count))
            {
                log.Info($"    {kvp.Key}: {kvp.Value.Count}");
            }
        }
    }

    /// <summary>
    /// Entry representing a single building with its classification.
    /// </summary>
    public class BuildingEntry
    {
        public BuildingPrefab Prefab { get; }
        public string Region { get; }
        public ZoneType ZoneType { get; }
        public RegionInfo RegionInfo { get; }

        public BuildingEntry(BuildingPrefab prefab, string region, ZoneType zoneType, RegionInfo regionInfo)
        {
            Prefab = prefab;
            Region = region;
            ZoneType = zoneType;
            RegionInfo = regionInfo;
        }
    }
}
