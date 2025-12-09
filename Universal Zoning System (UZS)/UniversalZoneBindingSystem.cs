using System;
using System.Collections.Generic;
using Colossal.Logging;
using Colossal.UI.Binding;
using Game.Prefabs;
using Game.Tools;
using Game.UI;
using Game.Zones;
using Unity.Collections;
using Unity.Entities;

namespace UniversalZoningSystem
{
    /// <summary>
    /// UI binding system that exposes universal zones to the React UI.
    /// Uses existing vanilla zone prefabs based on zone name patterns.
    /// </summary>
    public partial class UniversalZoneBindingSystem : UISystemBase
    {
        private static readonly ILog Log = Mod.Log;

        private PrefabSystem _prefabSystem;
        private ToolSystem _toolSystem;
        private ZoneToolSystem _zoneToolSystem;
        
        // Cache of vanilla zone prefabs by our zone type enum
        private Dictionary<ZoneType, ZonePrefab> _vanillaZonePrefabs;
        private bool _cacheInitialized;

        protected override void OnCreate()
        {
            base.OnCreate();

            try
            {
                _prefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();
                _toolSystem = World.GetOrCreateSystemManaged<ToolSystem>();
                _zoneToolSystem = World.GetOrCreateSystemManaged<ZoneToolSystem>();
                _vanillaZonePrefabs = new Dictionary<ZoneType, ZonePrefab>();

                // Trigger binding - UI calls this to select a zone
                AddBinding(new TriggerBinding<string>(
                    "universalZoning",
                    "selectZone",
                    SelectZone
                ));

                Log.Info("UniversalZoneBindingSystem created successfully.");
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to create UniversalZoneBindingSystem: {ex.Message}");
            }
        }

        protected override void OnUpdate()
        {
            // Initialize cache once zone prefabs are available
            if (!_cacheInitialized)
            {
                InitializeZonePrefabCache();
            }
        }

        private void InitializeZonePrefabCache()
        {
            var query = GetEntityQuery(ComponentType.ReadOnly<ZoneData>(), ComponentType.ReadOnly<PrefabData>());
            if (query.IsEmptyIgnoreFilter)
                return;

            var entities = query.ToEntityArray(Allocator.Temp);
            try
            {
                foreach (var entity in entities)
                {
                    if (!_prefabSystem.TryGetPrefab<ZonePrefab>(entity, out var zonePrefab))
                        continue;

                    // Skip our custom prefabs
                    if (zonePrefab.name.StartsWith("UZS_"))
                        continue;

                    // Use name-based classification to map to our zone types
                    var classification = UniversalZonePrefabSystem.GetZoneClassification(zonePrefab.name);
                    if (classification == null)
                        continue;

                    // Prefer NA or EU prefabs as they're commonly used
                    bool shouldUse = !_vanillaZonePrefabs.ContainsKey(classification.ZoneType);
                    if (!shouldUse && (zonePrefab.name.StartsWith("NA ") || zonePrefab.name.StartsWith("EU ")))
                    {
                        shouldUse = true;
                    }

                    if (shouldUse)
                    {
                        _vanillaZonePrefabs[classification.ZoneType] = zonePrefab;
                    }
                }

                if (_vanillaZonePrefabs.Count > 0)
                {
                    _cacheInitialized = true;
                    Log.Info($"Cached {_vanillaZonePrefabs.Count} vanilla zone prefabs:");
                    foreach (var kvp in _vanillaZonePrefabs)
                    {
                        Log.Info($"  {kvp.Key}: {kvp.Value.name}");
                    }
                }
            }
            finally
            {
                entities.Dispose();
            }
        }

        private void SelectZone(string zoneId)
        {
            if (string.IsNullOrEmpty(zoneId))
                return;

            Log.Info($"SelectZone called with: {zoneId}");

            // Map the universal zone ID to our zone type
            var zoneType = GetZoneTypeFromId(zoneId);
            if (zoneType == ZoneType.None)
            {
                Log.Warn($"Unknown zone ID: {zoneId}");
                return;
            }

            // Get a vanilla zone prefab for this type
            if (!_vanillaZonePrefabs.TryGetValue(zoneType, out var zonePrefab))
            {
                Log.Warn($"No vanilla zone prefab found for type: {zoneType}");
                return;
            }

            Log.Info($"Using vanilla zone prefab: {zonePrefab.name} for type {zoneType}");

            // Activate zone tool with this prefab
            try
            {
                _toolSystem.activeTool = _zoneToolSystem;
                _zoneToolSystem.prefab = zonePrefab;
                Log.Info($"Zone tool activated with: {zonePrefab.name}");
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to activate zone tool: {ex.Message}");
            }
        }

        private ZoneType GetZoneTypeFromId(string zoneId)
        {
            return zoneId switch
            {
                "UZS_LowResidential" => ZoneType.ResidentialLow,
                "UZS_MediumResidential" => ZoneType.ResidentialMedium,
                "UZS_HighResidential" => ZoneType.ResidentialHigh,
                "UZS_MixedUse" => ZoneType.ResidentialMixed,
                "UZS_LowCommercial" => ZoneType.CommercialLow,
                "UZS_HighCommercial" => ZoneType.CommercialHigh,
                "UZS_LowOffice" => ZoneType.OfficeLow,
                "UZS_HighOffice" => ZoneType.OfficeHigh,
                _ => ZoneType.None
            };
        }
    }
}