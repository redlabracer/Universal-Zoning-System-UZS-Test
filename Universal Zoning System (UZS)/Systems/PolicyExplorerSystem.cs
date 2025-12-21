using System;
using System.Text;
using Colossal.Logging;
using Game;
using Game.Areas;
using Game.Prefabs;
using Unity.Collections;
using Unity.Entities;

namespace UniversalZoningSystem.Systems
{
    /// <summary>
    /// Diagnostic system to explore how CS2's district policy system works.
    /// This helps us understand what components and prefabs we need to create
    /// to integrate with the vanilla policy panel.
    /// </summary>
    public partial class PolicyExplorerSystem : GameSystemBase
    {
        private static readonly ILog Log = Mod.Log;
        
        private PrefabSystem _prefabSystem;
        private EntityQuery _districtQuery;
        private EntityQuery _allPrefabsQuery;
        private bool _hasExplored;
        private int _frameDelay;

        protected override void OnCreate()
        {
            base.OnCreate();
            _prefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();
            
            _districtQuery = GetEntityQuery(ComponentType.ReadOnly<District>());
            _allPrefabsQuery = GetEntityQuery(ComponentType.ReadOnly<PrefabData>());
        }

        protected override void OnUpdate()
        {
            if (_hasExplored)
            {
                Enabled = false;
                return;
            }

            _frameDelay++;
            if (_frameDelay < 300)
                return;

            try
            {
                ExploreDistrictSystem();
                ExplorePolicyPrefabs();
                _hasExplored = true;
            }
            catch (Exception ex)
            {
                Log.Error($"PolicyExplorer error: {ex.Message}\n{ex.StackTrace}");
                _hasExplored = true;
            }
        }

        private void ExploreDistrictSystem()
        {
            Log.Info("=== Exploring District System ===");

            var districts = _districtQuery.ToEntityArray(Allocator.Temp);
            Log.Info($"Found {districts.Length} districts in the world.");

            if (districts.Length > 0)
            {
                var firstDistrict = districts[0];
                Log.Info($"Examining first district entity: {firstDistrict.Index}");

                var componentTypes = EntityManager.GetComponentTypes(firstDistrict);
                var sb = new StringBuilder();
                sb.AppendLine("Components on district entity:");
                
                foreach (var compType in componentTypes)
                {
                    sb.AppendLine($"  - {compType.GetManagedType()?.Name ?? compType.ToString()}");
                }
                Log.Info(sb.ToString());

                CheckComponent<District>(firstDistrict, "District");
                CheckComponent<PrefabRef>(firstDistrict, "PrefabRef");
                
                CheckBuffer(firstDistrict, "DistrictModifier");
                
                componentTypes.Dispose();
            }

            districts.Dispose();
        }

        private void CheckComponent<T>(Entity entity, string name) where T : struct, IComponentData
        {
            if (EntityManager.HasComponent<T>(entity))
            {
                Log.Info($"  ? Has {name} component");
            }
            else
            {
                Log.Info($"  ? Missing {name} component");
            }
        }

        private void CheckBuffer(Entity entity, string bufferTypeName)
        {
            // We can't easily check for arbitrary buffer types without knowing the type
            // So we log what we attempted
            Log.Info($"  ? Buffer check for {bufferTypeName} requires type knowledge");
        }

        private void ExplorePolicyPrefabs()
        {
            Log.Info("=== Exploring Policy-Related Prefabs ===");

            var prefabEntities = _allPrefabsQuery.ToEntityArray(Allocator.Temp);
            Log.Info($"Total prefabs in game: {prefabEntities.Length}");

            int policyCount = 0;
            int districtOptionCount = 0;

            foreach (var entity in prefabEntities)
            {
                if (!_prefabSystem.TryGetPrefab<PrefabBase>(entity, out var prefab))
                    continue;

                string name = prefab.name;
                string typeName = prefab.GetType().Name;

                // Look for anything related to policies, district options, or modifiers
                if (name.Contains("Policy") || 
                    name.Contains("Restriction") || 
                    name.Contains("Ban") ||
                    typeName.Contains("Policy") ||
                    typeName.Contains("DistrictOption") ||
                    typeName.Contains("Modifier"))
                {
                    Log.Info($"  Found: {name} (Type: {typeName})");
                    policyCount++;
                }

                // Also track types we recognize as district-related
                if (typeName.Contains("District"))
                {
                    districtOptionCount++;
                    if (districtOptionCount <= 10)
                    {
                        Log.Info($"  District-related: {name} (Type: {typeName})");
                    }
                }
            }

            Log.Info($"Found {policyCount} policy-related prefabs");
            Log.Info($"Found {districtOptionCount} district-related prefabs");

            // Also log the vanilla policies by name
            LogVanillaPolicies(prefabEntities);

            prefabEntities.Dispose();
        }

        private void LogVanillaPolicies(NativeArray<Entity> prefabEntities)
        {
            Log.Info("=== Known Vanilla Policy Names ===");
            
            string[] knownPolicies = new[]
            {
                "Bicycle Traffic Restriction",
                "Combustion Engine Ban",
                "Energy Consumption Awareness",
                "Gated Community",
                "Heavy Traffic Ban",
                "Recycling",
                "Roadside Parking Fee",
                "Speed Bumps",
                "Urban Cycling Initiative"
            };

            foreach (var policyName in knownPolicies)
            {
                bool found = false;
                foreach (var entity in prefabEntities)
                {
                    if (!_prefabSystem.TryGetPrefab<PrefabBase>(entity, out var prefab))
                        continue;

                    if (prefab.name == policyName || prefab.name.Contains(policyName.Replace(" ", "")))
                    {
                        found = true;
                        Log.Info($"  ? Found '{policyName}' - Type: {prefab.GetType().Name}");
                        
                        // Log all components on this prefab
                        var componentTypes = EntityManager.GetComponentTypes(entity);
                        foreach (var comp in componentTypes)
                        {
                            var managedType = comp.GetManagedType();
                            if (managedType != null)
                            {
                                Log.Info($"      Component: {managedType.Name}");
                            }
                        }
                        componentTypes.Dispose();
                        break;
                    }
                }

                if (!found)
                {
                    Log.Info($"  ? Not found: '{policyName}'");
                }
            }
        }
    }
}
