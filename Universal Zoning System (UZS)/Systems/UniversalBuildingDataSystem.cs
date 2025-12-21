using System;
using Colossal.Logging;
using Game;
using Game.Prefabs;
using Unity.Collections;
using Unity.Entities;
using UniversalZoningSystem.Components;

namespace UniversalZoningSystem.Systems
{
    /// <summary>
    /// System that adds UniversalBuildingData to universal building prefabs.
    /// This allows us to identify the region and type of a building at runtime efficiently.
    /// </summary>
    public partial class UniversalBuildingDataSystem : GameSystemBase
    {
        private static readonly ILog Log = Mod.Log;
        private PrefabSystem _prefabSystem;
        private EntityQuery _query;

        protected override void OnCreate()
        {
            base.OnCreate();
            _prefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();
            
            _query = GetEntityQuery(
                ComponentType.ReadOnly<PrefabData>(),
                ComponentType.ReadOnly<BuildingData>(),
                ComponentType.Exclude<UniversalBuildingData>()
            );
        }

        protected override void OnUpdate()
        {
            if (_query.IsEmptyIgnoreFilter)
                return;

            var entities = _query.ToEntityArray(Allocator.Temp);
            foreach (var entity in entities)
            {
                var prefab = _prefabSystem.GetPrefab<PrefabBase>(entity);
                if (prefab == null || !prefab.name.StartsWith("Universal_"))
                    continue;

                string originalName = prefab.name.Substring("Universal_".Length);
                
                var data = new UniversalBuildingData
                {
                    Region = GetRegion(originalName),
                    Style = GetStyle(originalName)
                };

                EntityManager.AddComponentData(entity, data);
            }
            entities.Dispose();
        }

        private RegionType GetRegion(string name)
        {
            var regionStr = RegionPrefixManager.GetRegionFromPrefabName(name);
            
            switch (regionStr)
            {
                case "NA_": return RegionType.NorthAmerica;
                case "USNE_": return RegionType.NorthAmerica; // Sub-region
                case "USSW_": return RegionType.NorthAmerica; // Sub-region
                case "EU_": return RegionType.European;
                case "UK_": return RegionType.UnitedKingdom;
                case "GER_": return RegionType.Germany;
                case "FR_": return RegionType.France;
                case "NL_": return RegionType.Netherlands;
                case "EE_": return RegionType.EasternEurope;
                case "JP_": return RegionType.Japan;
                case "CN_": return RegionType.China;
                default: return RegionType.Unknown;
            }
        }

        private BuildingStyleType GetStyle(string name)
        {
            string lower = name.ToLowerInvariant();
            
            if (lower.Contains("row") || lower.Contains("terrace")) return BuildingStyleType.Attached;
            if (lower.Contains("detached") || lower.Contains("villa")) return BuildingStyleType.Detached;
            if (lower.Contains("high") || lower.Contains("tower") || lower.Contains("condo")) return BuildingStyleType.HighRise;
            if (lower.Contains("mixed")) return BuildingStyleType.Mixed;
            if (lower.Contains("commercial")) return BuildingStyleType.Commercial;
            if (lower.Contains("office")) return BuildingStyleType.Office;
            
            if (lower.Contains("residential") && lower.Contains("low")) return BuildingStyleType.Detached;

            return BuildingStyleType.Unknown;
        }
    }
}
