using System;
using Colossal.Logging;
using Colossal.UI.Binding;
using Game.Areas;
using Game.UI;
using Unity.Entities;
using UniversalZoningSystem.Components;

namespace UniversalZoningSystem.Systems
{
    /// <summary>
    /// Manages UI bindings for District policy settings.
    /// Allows the UI to read and write UZS policies for specific districts.
    /// </summary>
    public partial class DistrictSettingsUISystem : UISystemBase
    {
        private static readonly ILog Log = Mod.Log;
        private string _kGroup = "universalZoningDistrict";
        private UZSPolicySystem _policySystem;

        protected override void OnCreate()
        {
            base.OnCreate();
            _policySystem = World.GetOrCreateSystemManaged<UZSPolicySystem>();

            AddBinding(new TriggerBinding<Entity, string, bool>(_kGroup, "setPolicy", SetPolicy));
            AddBinding(new CallBinding<Entity, string>(_kGroup, "getPolicies", GetPolicies));
            
            Log.Info("DistrictSettingsUISystem initialized with policy bindings.");
        }

        protected override void OnUpdate()
        {
        }

        private void SetPolicy(Entity districtEntity, string policyName, bool enabled)
        {
            if (!EntityManager.Exists(districtEntity) || !EntityManager.HasComponent<District>(districtEntity))
            {
                Log.Warn($"SetPolicy: Invalid district entity {districtEntity.Index}");
                return;
            }

            _policySystem.SetPolicy(districtEntity, policyName, enabled);
            Log.Info($"Set policy {policyName}={enabled} for district {districtEntity.Index}");
        }

        private string GetPolicies(Entity districtEntity)
        {
            if (!EntityManager.Exists(districtEntity) || !EntityManager.HasComponent<District>(districtEntity))
                return "{}";

            var policies = _policySystem.GetPolicies(districtEntity);

            return $@"{{
                ""NoAttachedHouses"": {policies.NoAttachedHouses.ToString().ToLower()},
                ""NoHighRise"": {policies.NoHighRise.ToString().ToLower()},
                ""DetachedOnly"": {policies.DetachedOnly.ToString().ToLower()},
                ""EuropeanOnly"": {policies.EuropeanOnly.ToString().ToLower()},
                ""NorthAmericaOnly"": {policies.NorthAmericaOnly.ToString().ToLower()}
            }}";
        }
    }
}
