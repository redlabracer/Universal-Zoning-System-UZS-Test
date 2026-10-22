using System;
using System.Collections.Generic;
using System.Linq;
using Colossal.Logging;
using Colossal.UI.Binding;
using Game.Prefabs;
using Game.SceneFlow;
using Game.UI;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Unity.Collections;
using Unity.Entities;

namespace UniversalZoningSystem.CustomZones
{
    /// <summary>
    /// Bindings for the Custom Zone Builder panel (group "universalZoning").
    /// </summary>
    public partial class CustomZoneBindingSystem : UISystemBase
    {
        private const string Group = "universalZoning";
        private static readonly ILog Log = Mod.Log;

        private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
        {
            Converters = { new StringEnumConverter() }
        };

        private PrefabSystem _prefabSystem;
        private UniversalZoneUISystem _zoneUISystem;
        private BuildingZoneModifierSystem _buildingSystem;
        private EntityQuery _zoneQuery;
        private EntityQuery _buildingQuery;

        private ValueBinding<bool> _builderOpen;
        private ValueBinding<string> _customZones;
        private ValueBinding<string> _sources;
        private ValueBinding<bool> _restartRequired;
        private ValueBinding<int> _buttonLocation;

        private class SourceZoneDto
        {
            public string name;
            public int buildings;
        }

        private class SourceGroupDto
        {
            public string zoneType;
            public List<SourceZoneDto> zones = new List<SourceZoneDto>();
        }

        private class SaveRequest
        {
            public string id;
            public string name;
            public ZoneType baseZoneType;
            public List<string> sourceZones;
        }

        protected override void OnCreate()
        {
            base.OnCreate();

            _prefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();
            _zoneUISystem = World.GetOrCreateSystemManaged<UniversalZoneUISystem>();
            _buildingSystem = World.GetOrCreateSystemManaged<BuildingZoneModifierSystem>();

            _zoneQuery = GetEntityQuery(ComponentType.ReadOnly<ZoneData>(), ComponentType.ReadOnly<PrefabData>());
            _buildingQuery = GetEntityQuery(
                ComponentType.ReadOnly<BuildingData>(),
                ComponentType.ReadOnly<SpawnableBuildingData>(),
                ComponentType.ReadOnly<PrefabData>());

            AddBinding(_builderOpen = new ValueBinding<bool>(Group, "builderOpen", false));
            AddBinding(_customZones = new ValueBinding<string>(Group, "customZones", SerializeZones()));
            AddBinding(_sources = new ValueBinding<string>(Group, "sources", "[]"));
            AddBinding(_restartRequired = new ValueBinding<bool>(Group, "restartRequired", false));
            AddBinding(_buttonLocation = new ValueBinding<int>(Group, "buttonLocation", CurrentButtonLocation()));

            AddBinding(new TriggerBinding<bool>(Group, "setBuilderOpen", SetBuilderOpen));
            AddBinding(new TriggerBinding<string>(Group, "saveCustomZone", SaveCustomZone));
            AddBinding(new TriggerBinding<string>(Group, "deleteCustomZone", DeleteCustomZone));

            CustomZoneRegistry.Changed += OnRegistryChanged;
            Settings.ModSettings.UISettingsChanged += OnUISettingsChanged;
        }

        private string _sourcesCache;

        protected override void OnGameLoadingComplete(Colossal.Serialization.Entities.Purpose purpose, Game.GameMode mode)
        {
            base.OnGameLoadingComplete(purpose, mode);
            _sourcesCache = mode == Game.GameMode.Game ? BuildSourceCatalog() : null;
            if (_sourcesCache != null)
                _sources.Update(_sourcesCache);
        }

        private static int CurrentButtonLocation()
        {
            return (int)(Mod.Settings?.ButtonLocation ?? Settings.ButtonLocationOption.UniversalModMenu);
        }

        private void OnUISettingsChanged()
        {
            _buttonLocation?.Update(CurrentButtonLocation());
        }

        protected override void OnDestroy()
        {
            Settings.ModSettings.UISettingsChanged -= OnUISettingsChanged;
            CustomZoneRegistry.Changed -= OnRegistryChanged;
            base.OnDestroy();
        }

        private void OnRegistryChanged()
        {
            _customZones.Update(SerializeZones());
            GameManager.instance.localizationManager.ReloadActiveLocale();
        }

        private static string SerializeZones()
        {
            return JsonConvert.SerializeObject(CustomZoneRegistry.Zones, JsonSettings);
        }

        private void SetBuilderOpen(bool open)
        {
            if (open && _sourcesCache == null)
            {
                _sourcesCache = BuildSourceCatalog();
                _sources.Update(_sourcesCache);
            }

            _builderOpen.Update(open);
        }

        private string BuildSourceCatalog()
        {
            var buildingCounts = new Dictionary<Entity, int>();
            var buildings = _buildingQuery.ToComponentDataArray<SpawnableBuildingData>(Allocator.Temp);
            try
            {
                foreach (var data in buildings)
                {
                    var zone = data.m_ZonePrefab;
                    if (zone == Entity.Null)
                        continue;

                    buildingCounts.TryGetValue(zone, out var count);
                    buildingCounts[zone] = count + 1;
                }
            }
            finally
            {
                buildings.Dispose();
            }

            var groups = new Dictionary<ZoneType, SourceGroupDto>();
            var zones = _zoneQuery.ToEntityArray(Allocator.Temp);
            try
            {
                foreach (var entity in zones)
                {
                    if (!_prefabSystem.TryGetPrefab<ZonePrefab>(entity, out var zonePrefab) || zonePrefab == null)
                        continue;
                    if (zonePrefab.name.StartsWith("UZS_") || zonePrefab.name.StartsWith(CustomZoneDefinition.IdPrefix))
                        continue;

                    var classification = UniversalZonePrefabSystem.GetZoneClassification(zonePrefab.name);
                    if (classification == null || classification.ZoneType == ZoneType.None)
                        continue;

                    buildingCounts.TryGetValue(entity, out var count);
                    if (count == 0)
                        continue;

                    if (!groups.TryGetValue(classification.ZoneType, out var group))
                    {
                        group = new SourceGroupDto { zoneType = classification.ZoneType.ToString() };
                        groups[classification.ZoneType] = group;
                    }

                    group.zones.Add(new SourceZoneDto { name = zonePrefab.name, buildings = count });
                }
            }
            finally
            {
                zones.Dispose();
            }

            var result = groups.OrderBy(g => (int)g.Key).Select(g => g.Value).ToList();
            foreach (var group in result)
                group.zones.Sort((a, b) => string.CompareOrdinal(a.name, b.name));

            return JsonConvert.SerializeObject(result);
        }

        private void SaveCustomZone(string json)
        {
            try
            {
                var request = JsonConvert.DeserializeObject<SaveRequest>(json, JsonSettings);
                if (request == null || string.IsNullOrWhiteSpace(request.name) || request.baseZoneType == ZoneType.None)
                {
                    Log.Warn("SaveCustomZone: invalid request.");
                    return;
                }

                var sources = (request.sourceZones ?? new List<string>())
                    .Where(name => UniversalZonePrefabSystem.GetZoneClassification(name)?.ZoneType == request.baseZoneType)
                    .Distinct()
                    .ToList();

                if (sources.Count == 0)
                {
                    Log.Warn("SaveCustomZone: no valid source zones.");
                    return;
                }

                var existing = string.IsNullOrEmpty(request.id) ? null : CustomZoneRegistry.Get(request.id);
                var definition = new CustomZoneDefinition
                {
                    Id = existing?.Id ?? CustomZoneRegistry.NewId(),
                    Name = request.name.Trim(),
                    BaseZoneType = existing?.BaseZoneType ?? request.baseZoneType,
                    SourceZones = sources
                };

                if (existing != null && (existing.BaseZoneType != request.baseZoneType || existing.SourceZones.Except(sources).Any()))
                    _restartRequired.Update(true);

                CustomZoneRegistry.Upsert(definition);

                if (_zoneUISystem.IsInitialized)
                    _buildingSystem.CloneBuildingsForCustomZone(definition);
            }
            catch (Exception ex)
            {
                Log.Error($"SaveCustomZone failed: {ex.Message}");
            }
        }

        private void DeleteCustomZone(string id)
        {
            if (CustomZoneRegistry.Remove(id))
                _restartRequired.Update(true);
        }
    }
}
