using System;
using System.Collections.Generic;
using Colossal.Logging;
using Game;
using Game.Areas;
using Game.Prefabs;
using Game.SceneFlow;
using Game.Policies;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UniversalZoningSystem.Components;

namespace UniversalZoningSystem.Systems
{
    /// <summary>
    /// Creates and manages UZS policy prefabs that integrate with the vanilla district policy panel.
    /// Clones existing PolicyTogglePrefab instances and registers them with the game.
    /// </summary>
    public partial class UZSPolicySystem : GameSystemBase
    {
        private static readonly ILog Log = Mod.Log;
        
        private PrefabSystem _prefabSystem;
        private EntityQuery _policyQuery;
        private bool _initialized;
        private int _frameDelay;

        public const string PolicyNoAttached = "UZS No Attached Houses";
        public const string PolicyNoHighRise = "UZS No High-Rise";
        public const string PolicyDetachedOnly = "UZS Detached Only";
        public const string PolicyEUOnly = "UZS European Only";
        public const string PolicyNAOnly = "UZS North America Only";

        private readonly Dictionary<string, Entity> _policyEntities = new Dictionary<string, Entity>();
        private readonly List<PolicyTogglePrefab> _createdPolicies = new List<PolicyTogglePrefab>();

        protected override void OnCreate()
        {
            base.OnCreate();
            _prefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();
            
            _policyQuery = GetEntityQuery(
                ComponentType.ReadOnly<PolicyData>(),
                ComponentType.ReadOnly<PrefabData>()
            );
            
            Log.Info("UZSPolicySystem created - will integrate with vanilla policy panel.");
        }

        protected override void OnGamePreload(Colossal.Serialization.Entities.Purpose purpose, GameMode mode)
        {
            base.OnGamePreload(purpose, mode);
            
            if (!mode.IsGameOrEditor())
                return;

            TryCreatePolicies();
        }

        protected override void OnUpdate()
        {
            if (_initialized)
            {
                Enabled = false;
                return;
            }

            _frameDelay++;
            if (_frameDelay < 100)
                return;

            TryCreatePolicies();
        }

        private void TryCreatePolicies()
        {
            if (_initialized)
                return;

            if (_policyQuery.IsEmptyIgnoreFilter)
            {
                Log.Info("No policy prefabs found yet, waiting...");
                return;
            }

            try
            {
                CreatePolicies();
                _initialized = true;
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to create policies: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private void CreatePolicies()
        {
            Log.Info("=== Creating UZS District Policies ===");

            // Find a PolicyTogglePrefab that has DistrictOptionData to use as template
            // This ensures our cloned policies will appear in the district panel
            PolicyTogglePrefab templatePrefab = null;
            Entity templateEntity = Entity.Null;

            var entities = _policyQuery.ToEntityArray(Allocator.Temp);
            try
            {
                foreach (var entity in entities)
                {
                    // Must have DistrictOptionData to appear in district panel
                    if (!EntityManager.HasComponent<DistrictOptionData>(entity))
                        continue;
                        
                    if (_prefabSystem.TryGetPrefab<PolicyTogglePrefab>(entity, out var prefab))
                    {
                        templatePrefab = prefab;
                        templateEntity = entity;
                        Log.Info($"Found template PolicyTogglePrefab with DistrictOptionData: {prefab.name}");
                        break;
                    }
                }
            }
            finally
            {
                entities.Dispose();
            }

            if (templatePrefab == null)
            {
                Log.Warn("No PolicyTogglePrefab with DistrictOptionData found! Policies may not appear in district panel.");
                
                // Fallback: try any PolicyTogglePrefab
                entities = _policyQuery.ToEntityArray(Allocator.Temp);
                try
                {
                    foreach (var entity in entities)
                    {
                        if (_prefabSystem.TryGetPrefab<PolicyTogglePrefab>(entity, out var prefab))
                        {
                            templatePrefab = prefab;
                            templateEntity = entity;
                            Log.Info($"Using fallback template (no DistrictOptionData): {prefab.name}");
                            break;
                        }
                    }
                }
                finally
                {
                    entities.Dispose();
                }
            }

            if (templatePrefab == null)
            {
                Log.Error("No PolicyTogglePrefab found to use as template!");
                return;
            }

            // Create our custom policies by cloning the template
            CreatePolicy(PolicyNoAttached, "Prevents row houses and terraced homes from spawning", templatePrefab);
            CreatePolicy(PolicyNoHighRise, "Prevents tall towers from spawning", templatePrefab);
            CreatePolicy(PolicyDetachedOnly, "Only single-family detached houses allowed", templatePrefab);
            CreatePolicy(PolicyEUOnly, "Only European architecture allowed", templatePrefab);
            CreatePolicy(PolicyNAOnly, "Only North American architecture allowed", templatePrefab);

            Log.Info($"=== Created {_createdPolicies.Count} UZS Policies ===");
        }

        private void CreatePolicy(string policyName, string description, PolicyTogglePrefab template)
        {
            try
            {
                // Clone the template prefab
                var newPolicy = UnityEngine.Object.Instantiate(template);
                newPolicy.name = policyName;

                // Register with the prefab system
                _prefabSystem.AddPrefab(newPolicy);

                if (_prefabSystem.TryGetEntity(newPolicy, out Entity entity))
                {
                    _policyEntities[policyName] = entity;
                    _createdPolicies.Add(newPolicy);
                    Log.Info($"Created policy: {policyName} (Entity: {entity.Index})");

                    // Log the components on our new policy
                    var componentTypes = EntityManager.GetComponentTypes(entity);
                    Log.Info($"  Components on {policyName}:");
                    foreach (var comp in componentTypes)
                    {
                        var name = comp.GetManagedType()?.Name ?? comp.ToString();
                        Log.Info($"    - {name}");
                    }
                    componentTypes.Dispose();
                    
                    // Check if policy has the required DistrictOptionData component
                    bool hasDistrictOption = EntityManager.HasComponent<DistrictOptionData>(entity);
                    bool hasDistrictModifier = EntityManager.HasComponent<DistrictModifierData>(entity);
                    Log.Info($"  HasDistrictOptionData: {hasDistrictOption}, HasDistrictModifierData: {hasDistrictModifier}");
                    
                    if (!hasDistrictOption)
                    {
                        Log.Warn($"  Policy {policyName} is missing DistrictOptionData - may not appear in district panel!");
                    }
                }
                else
                {
                    Log.Warn($"Failed to get entity for policy: {policyName}");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to create policy {policyName}: {ex.Message}");
            }
        }

        /// <summary>
        /// Check if a specific UZS policy is enabled for a district.
        /// First checks the vanilla district policy system, then falls back to our custom component.
        /// </summary>
        public bool IsPolicyEnabled(Entity districtEntity, string policyId)
        {
            if (districtEntity == Entity.Null)
                return false;

            if (!_policyEntities.TryGetValue(policyId, out Entity policyEntity))
                return false;

            // Method 1: Check vanilla district modifier buffer
            // Districts store enabled modifiers (policies) in a DistrictModifier buffer
            if (EntityManager.HasBuffer<DistrictModifier>(districtEntity))
            {
                var modifiers = EntityManager.GetBuffer<DistrictModifier>(districtEntity);
                foreach (var modifier in modifiers)
                {
                    // DistrictModifier stores the policy entity - check the struct fields
                    // The exact field depends on the game version
                    // We'll log and investigate the structure
                    if (Mod.Settings.EnableVerboseLogging)
                    {
                        Log.Info($"[PolicyCheck] District has {modifiers.Length} modifiers");
                    }
                    // For now, skip this check until we know the exact structure
                    break;
                }
            }

            // Method 2: Use our custom component for tracking
            if (EntityManager.HasComponent<UZSDistrictPolicies>(districtEntity))
            {
                var policies = EntityManager.GetComponentData<UZSDistrictPolicies>(districtEntity);
                
                switch (policyId)
                {
                    case PolicyNoAttached: return policies.NoAttachedHouses;
                    case PolicyNoHighRise: return policies.NoHighRise;
                    case PolicyDetachedOnly: return policies.DetachedOnly;
                    case PolicyEUOnly: return policies.EuropeanOnly;
                    case PolicyNAOnly: return policies.NorthAmericaOnly;
                }
            }

            return false;
        }

        /// <summary>
        /// Gets all policy entities created by this system.
        /// </summary>
        public Dictionary<string, Entity> GetPolicyEntities() => _policyEntities;

        /// <summary>
        /// Gets all policies for a district (for UI).
        /// </summary>
        public UZSDistrictPolicies GetPolicies(Entity districtEntity)
        {
            if (districtEntity == Entity.Null || !EntityManager.HasComponent<UZSDistrictPolicies>(districtEntity))
                return UZSDistrictPolicies.Default();

            return EntityManager.GetComponentData<UZSDistrictPolicies>(districtEntity);
        }

        /// <summary>
        /// Sets a policy for a district (from UI).
        /// </summary>
        public void SetPolicy(Entity districtEntity, string policyId, bool enabled)
        {
            if (districtEntity == Entity.Null)
                return;

            // Ensure component exists
            if (!EntityManager.HasComponent<UZSDistrictPolicies>(districtEntity))
            {
                EntityManager.AddComponentData(districtEntity, UZSDistrictPolicies.Default());
            }

            var policies = EntityManager.GetComponentData<UZSDistrictPolicies>(districtEntity);

            switch (policyId)
            {
                case PolicyNoAttached: policies.NoAttachedHouses = enabled; break;
                case PolicyNoHighRise: policies.NoHighRise = enabled; break;
                case PolicyDetachedOnly: policies.DetachedOnly = enabled; break;
                case PolicyEUOnly: policies.EuropeanOnly = enabled; break;
                case PolicyNAOnly: policies.NorthAmericaOnly = enabled; break;
            }

            EntityManager.SetComponentData(districtEntity, policies);
            Log.Info($"Set policy {policyId}={enabled} for district {districtEntity.Index}");
        }
    }
}
