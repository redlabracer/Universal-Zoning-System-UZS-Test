using Colossal.Logging;
using Game;
using Game.Areas;
using Game.Buildings;
using Game.Common;
using Game.Objects;
using Game.Prefabs;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UniversalZoningSystem.Components;

namespace UniversalZoningSystem.Systems
{
    /// <summary>
    /// Enforces district-specific zoning settings by checking UZS policies.
    /// When a building spawns in a district with UZS policies enabled, 
    /// it checks if the building type/region is allowed.
    /// </summary>
    public partial class DistrictSpawnEnforcementSystem : GameSystemBase
    {
        private static readonly ILog Log = Mod.Log;
        private EntityQuery _newBuildingQuery;
        private PrefabSystem _prefabSystem;
        private UZSPolicySystem _policySystem;

        protected override void OnCreate()
        {
            base.OnCreate();
            _prefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();
            _policySystem = World.GetOrCreateSystemManaged<UZSPolicySystem>();

            _newBuildingQuery = GetEntityQuery(
                ComponentType.ReadOnly<Building>(),
                ComponentType.ReadOnly<CurrentDistrict>(),
                ComponentType.ReadOnly<PrefabRef>(),
                ComponentType.Exclude<UniversalZoneChecked>()
            );
        }

        protected override void OnUpdate()
        {
            if (_newBuildingQuery.IsEmptyIgnoreFilter)
                return;

            var entities = _newBuildingQuery.ToEntityArray(Allocator.Temp);
            var districtRefs = _newBuildingQuery.ToComponentDataArray<CurrentDistrict>(Allocator.Temp);
            var prefabRefs = _newBuildingQuery.ToComponentDataArray<PrefabRef>(Allocator.Temp);

            for (int i = 0; i < entities.Length; i++)
            {
                var entity = entities[i];
                var districtEntity = districtRefs[i].m_District;
                var prefabEntity = prefabRefs[i].m_Prefab;
                
                EntityManager.AddComponent<UniversalZoneChecked>(entity);

                if (districtEntity == Entity.Null)
                    continue;

                if (!EntityManager.HasComponent<UniversalBuildingData>(prefabEntity))
                    continue;

                var buildingData = EntityManager.GetComponentData<UniversalBuildingData>(prefabEntity);

                if (!IsAllowedByPolicies(districtEntity, buildingData))
                {
                    EntityManager.DestroyEntity(entity);
                    
                    if (Mod.Settings.EnableVerboseLogging)
                    {
                        Log.Info($"Policy enforcement: Removed {buildingData.Region} {buildingData.Style} building from restricted district.");
                    }
                }
            }

            entities.Dispose();
            districtRefs.Dispose();
            prefabRefs.Dispose();
        }

        private bool IsAllowedByPolicies(Entity districtEntity, UniversalBuildingData data)
        {
            if (_policySystem.IsPolicyEnabled(districtEntity, UZSPolicySystem.PolicyNoAttached))
            {
                if (data.Style == BuildingStyleType.Attached)
                    return false;
            }

            if (_policySystem.IsPolicyEnabled(districtEntity, UZSPolicySystem.PolicyNoHighRise))
            {
                if (data.Style == BuildingStyleType.HighRise)
                    return false;
            }

            if (_policySystem.IsPolicyEnabled(districtEntity, UZSPolicySystem.PolicyDetachedOnly))
            {
                if (data.Style != BuildingStyleType.Detached && data.Style != BuildingStyleType.Unknown)
                    return false;
            }

            if (_policySystem.IsPolicyEnabled(districtEntity, UZSPolicySystem.PolicyEUOnly))
            {
                if (data.Region != RegionType.European && 
                    data.Region != RegionType.UnitedKingdom &&
                    data.Region != RegionType.Germany &&
                    data.Region != RegionType.France &&
                    data.Region != RegionType.Netherlands &&
                    data.Region != RegionType.EasternEurope &&
                    data.Region != RegionType.Unknown)
                    return false;
            }

            if (_policySystem.IsPolicyEnabled(districtEntity, UZSPolicySystem.PolicyNAOnly))
            {
                if (data.Region != RegionType.NorthAmerica && data.Region != RegionType.Unknown)
                    return false;
            }

            return true;
        }
    }
}
