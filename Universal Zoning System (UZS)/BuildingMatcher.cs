using System;
using System.Collections.Generic;
using System.Linq;
using Colossal.Logging;
using Game.Prefabs;

namespace UniversalZoningSystem
{
    /// <summary>
    /// Matches buildings from all regions to universal zone types.
    /// Handles filtering, weighting, and selection of building variants.
    /// </summary>
    public class BuildingMatcher
    {
        private static readonly ILog Log = Mod.Log;

        private readonly Dictionary<string, List<BuildingPrefab>> _buildingsByZone;
        private readonly Dictionary<string, Dictionary<string, List<BuildingPrefab>>> _buildingsByZoneAndRegion;
        private readonly Random _random;

        public BuildingMatcher()
        {
            _buildingsByZone = new Dictionary<string, List<BuildingPrefab>>();
            _buildingsByZoneAndRegion = new Dictionary<string, Dictionary<string, List<BuildingPrefab>>>();
            _random = new Random();
        }

        /// <summary>
        /// Registers a building prefab with a universal zone.
        /// </summary>
        public void RegisterBuilding(string universalZoneId, BuildingPrefab building)
        {
            if (building == null || string.IsNullOrEmpty(universalZoneId))
                return;

            // Add to zone list
            if (!_buildingsByZone.ContainsKey(universalZoneId))
                _buildingsByZone[universalZoneId] = new List<BuildingPrefab>();

            _buildingsByZone[universalZoneId].Add(building);

            // Add to zone+region list
            var region = RegionPrefixManager.GetRegionFromPrefabName(building.name);

            if (!_buildingsByZoneAndRegion.ContainsKey(universalZoneId))
                _buildingsByZoneAndRegion[universalZoneId] = new Dictionary<string, List<BuildingPrefab>>();

            if (!_buildingsByZoneAndRegion[universalZoneId].ContainsKey(region))
                _buildingsByZoneAndRegion[universalZoneId][region] = new List<BuildingPrefab>();

            _buildingsByZoneAndRegion[universalZoneId][region].Add(building);
        }

        /// <summary>
        /// Gets all buildings registered for a universal zone.
        /// </summary>
        public IReadOnlyList<BuildingPrefab> GetBuildingsForZone(string universalZoneId)
        {
            if (_buildingsByZone.TryGetValue(universalZoneId, out var buildings))
                return buildings;

            return Array.Empty<BuildingPrefab>();
        }

        /// <summary>
        /// Gets buildings for a zone filtered by region.
        /// </summary>
        public IReadOnlyList<BuildingPrefab> GetBuildingsForZoneAndRegion(string universalZoneId, string region)
        {
            if (_buildingsByZoneAndRegion.TryGetValue(universalZoneId, out var regionDict))
            {
                if (regionDict.TryGetValue(region, out var buildings))
                    return buildings;
            }

            return Array.Empty<BuildingPrefab>();
        }

        /// <summary>
        /// Gets all regions that have buildings registered for a zone.
        /// </summary>
        public IEnumerable<string> GetRegionsForZone(string universalZoneId)
        {
            if (_buildingsByZoneAndRegion.TryGetValue(universalZoneId, out var regionDict))
                return regionDict.Keys;

            return Enumerable.Empty<string>();
        }

        /// <summary>
        /// Gets a random building from a universal zone with equal region weighting.
        /// This ensures smaller regions have fair representation.
        /// </summary>
        public BuildingPrefab GetRandomBuildingEqualRegionWeight(string universalZoneId)
        {
            if (!_buildingsByZoneAndRegion.TryGetValue(universalZoneId, out var regionDict) || regionDict.Count == 0)
                return null;

            // First, pick a random region
            var regions = regionDict.Keys.ToList();
            var randomRegion = regions[_random.Next(regions.Count)];

            // Then, pick a random building from that region
            var buildings = regionDict[randomRegion];
            if (buildings.Count == 0)
                return null;

            return buildings[_random.Next(buildings.Count)];
        }

        /// <summary>
        /// Gets a random building from a universal zone with proportional weighting.
        /// Regions with more buildings are more likely to be selected.
        /// </summary>
        public BuildingPrefab GetRandomBuildingProportionalWeight(string universalZoneId)
        {
            if (!_buildingsByZone.TryGetValue(universalZoneId, out var buildings) || buildings.Count == 0)
                return null;

            return buildings[_random.Next(buildings.Count)];
        }

        /// <summary>
        /// Gets statistics about registered buildings.
        /// </summary>
        public BuildingStatistics GetStatistics()
        {
            var stats = new BuildingStatistics();

            foreach (var kvp in _buildingsByZone)
            {
                stats.TotalBuildings += kvp.Value.Count;
                stats.BuildingsByZone[kvp.Key] = kvp.Value.Count;
            }

            foreach (var zoneKvp in _buildingsByZoneAndRegion)
            {
                foreach (var regionKvp in zoneKvp.Value)
                {
                    if (!stats.BuildingsByRegion.ContainsKey(regionKvp.Key))
                        stats.BuildingsByRegion[regionKvp.Key] = 0;

                    stats.BuildingsByRegion[regionKvp.Key] += regionKvp.Value.Count;
                }
            }

            stats.TotalZones = _buildingsByZone.Count;
            stats.TotalRegions = stats.BuildingsByRegion.Count;

            return stats;
        }

        /// <summary>
        /// Clears all registered buildings.
        /// </summary>
        public void Clear()
        {
            _buildingsByZone.Clear();
            _buildingsByZoneAndRegion.Clear();
        }
    }

    /// <summary>
    /// Statistics about registered buildings.
    /// </summary>
    public class BuildingStatistics
    {
        public int TotalBuildings { get; set; }
        public int TotalZones { get; set; }
        public int TotalRegions { get; set; }
        public Dictionary<string, int> BuildingsByZone { get; } = new Dictionary<string, int>();
        public Dictionary<string, int> BuildingsByRegion { get; } = new Dictionary<string, int>();

        public void LogStatistics(ILog log)
        {
            log.Info($"Building Statistics: {TotalBuildings} total buildings across {TotalZones} zones and {TotalRegions} regions");

            log.Info("Buildings by Zone:");
            foreach (var kvp in BuildingsByZone.OrderByDescending(x => x.Value))
            {
                log.Info($"  {kvp.Key}: {kvp.Value}");
            }

            log.Info("Buildings by Region:");
            foreach (var kvp in BuildingsByRegion.OrderByDescending(x => x.Value))
            {
                log.Info($"  {kvp.Key}: {kvp.Value}");
            }
        }
    }
}
