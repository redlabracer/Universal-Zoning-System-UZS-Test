using System;
using System.Collections.Generic;
using Colossal.Logging;
using Game;
using Game.Prefabs;
using Game.SceneFlow;
using Game.Zones;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace UniversalZoningSystem
{
    /// <summary>
    /// System that creates and registers universal zone prefabs with a custom category in the zone toolbar.
    /// 
    /// Based on StarQ's Asset UI Manager approach:
    /// 1. Create UIAssetCategoryPrefab with proper m_Menu reference
    /// 2. Add UIObject component with icon and priority
    /// 3. Register with PrefabSystem
    /// 4. Add zones to category via UIGroupElement buffer
    /// 
    /// Key: Must run during OnGamePreload to be picked up by the UI system.
    /// </summary>
    public partial class UniversalZoneUISystem : GameSystemBase
    {
        private static readonly ILog Log = Mod.Log;

        private PrefabSystem _prefabSystem;
        private EntityQuery _zoneQuery;
        private EntityQuery _menuQuery;
        private bool _initialized;

        // Created universal zone prefabs and their entities
        private readonly List<ZonePrefab> _createdZonePrefabs = new List<ZonePrefab>();
        private readonly Dictionary<string, Entity> _universalZoneEntities = new Dictionary<string, Entity>();

        // Template zones we clone from (keyed by zone type)
        private readonly Dictionary<ZoneType, ZonePrefab> _templateZones = new Dictionary<ZoneType, ZonePrefab>();
        private readonly Dictionary<ZoneType, Entity> _templateZoneEntities = new Dictionary<ZoneType, Entity>();

        // Our custom UI category for universal zones
        private UIAssetCategoryPrefab _universalZoneCategory;
        private Entity _universalZoneCategoryEntity;

        protected override void OnCreate()
        {
            base.OnCreate();
            _prefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();

            _zoneQuery = GetEntityQuery(
                ComponentType.ReadOnly<ZoneData>(),
                ComponentType.ReadOnly<PrefabData>()
            );

            _menuQuery = GetEntityQuery(
                ComponentType.ReadOnly<UIAssetMenuData>(),
                ComponentType.ReadOnly<PrefabData>()
            );

            Log.Info("UniversalZoneUISystem created.");
        }

        /// <summary>
        /// Called during game preload - this is when StarQ's mod runs its UI creation.
        /// This happens BEFORE the main game UI is built.
        /// </summary>
        protected override void OnGamePreload(Colossal.Serialization.Entities.Purpose purpose, GameMode mode)
        {
            base.OnGamePreload(purpose, mode);
            
            Log.Info($"OnGamePreload called: Purpose={purpose}, Mode={mode}");
            
            if (!mode.IsGameOrEditor())
                return;

            InitializeUniversalZones();
        }

        protected override void OnUpdate()
        {
            if (!_initialized)
            {
                if (_zoneQuery.IsEmptyIgnoreFilter || _menuQuery.IsEmptyIgnoreFilter)
                    return;

                InitializeUniversalZones();
            }
        }

        private void InitializeUniversalZones()
        {
            if (_initialized)
                return;

            try
            {
                Log.Info("=== Starting Universal Zone UI Creation ===");

                FindTemplateZones();

                if (_templateZones.Count == 0)
                {
                    Log.Info("No templates found yet, waiting...");
                    return;
                }

                CreateUniversalZoneCategory();
                CreateUniversalZonePrefabs();
                AddZonesToCategoryBuffer();

                _initialized = true;
                Log.Info("=== Universal Zone UI Creation Complete ===");
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to initialize Universal Zone UI: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private void FindTemplateZones()
        {
            var zoneEntities = _zoneQuery.ToEntityArray(Allocator.Temp);
            Log.Info($"Searching through {zoneEntities.Length} zone entities for templates...");

            try
            {
                foreach (var entity in zoneEntities)
                {
                    if (!_prefabSystem.TryGetPrefab<ZonePrefab>(entity, out var zonePrefab))
                        continue;

                    var classification = UniversalZonePrefabSystem.GetZoneClassification(zonePrefab.name);
                    if (classification == null)
                        continue;

                    bool shouldUse = !_templateZones.ContainsKey(classification.ZoneType);
                    if (!shouldUse && (zonePrefab.name.StartsWith("NA ") || zonePrefab.name.StartsWith("EU ")))
                        shouldUse = true;

                    if (shouldUse)
                    {
                        _templateZones[classification.ZoneType] = zonePrefab;
                        _templateZoneEntities[classification.ZoneType] = entity;
                        Log.Info($"Template for {classification.ZoneType}: {zonePrefab.name}");
                    }
                }
            }
            finally
            {
                zoneEntities.Dispose();
            }

            Log.Info($"Found {_templateZones.Count} zone templates.");
        }

        private void CreateUniversalZoneCategory()
        {
            Entity zonesMenuEntity = Entity.Null;
            UIAssetMenuPrefab zonesMenuPrefab = null;
            
            var menuEntities = _menuQuery.ToEntityArray(Allocator.Temp);
            try
            {
                foreach (var entity in menuEntities)
                {
                    if (!_prefabSystem.TryGetPrefab<UIAssetMenuPrefab>(entity, out var menuPrefab))
                        continue;

                    if (menuPrefab.name == "Zones")
                    {
                        zonesMenuEntity = entity;
                        zonesMenuPrefab = menuPrefab;
                        Log.Info($"Found Zones menu: {menuPrefab.name} (Entity: {entity.Index})");
                        break;
                    }
                }
            }
            finally
            {
                menuEntities.Dispose();
            }

            if (zonesMenuPrefab == null)
            {
                Log.Warn("Could not find Zones menu!");
                return;
            }

            if (_prefabSystem.TryGetPrefab(new PrefabID("UIAssetCategoryPrefab", "ZonesUniversal"), out PrefabBase existingTab))
            {
                Log.Info("Universal Zones category already exists, reusing...");
                _universalZoneCategory = existingTab as UIAssetCategoryPrefab;
                _prefabSystem.TryGetEntity(_universalZoneCategory, out _universalZoneCategoryEntity);
                return;
            }

            // Clone from existing category to ensure proper buffer initialization
            UIAssetCategoryPrefab templateCategory = null;
            var categoryQuery = GetEntityQuery(ComponentType.ReadOnly<UIAssetCategoryData>(), ComponentType.ReadOnly<PrefabData>());
            var categoryEntities = categoryQuery.ToEntityArray(Allocator.Temp);
            try
            {
                foreach (var entity in categoryEntities)
                {
                    if (!_prefabSystem.TryGetPrefab<UIAssetCategoryPrefab>(entity, out var categoryPrefab))
                        continue;

                    if (categoryPrefab.m_Menu != null && categoryPrefab.m_Menu.name == "Zones")
                    {
                        templateCategory = categoryPrefab;
                        Log.Info($"Found template category: {categoryPrefab.name}");
                        break;
                    }
                }
            }
            finally
            {
                categoryEntities.Dispose();
            }

            if (templateCategory == null)
            {
                Log.Warn("Could not find a template zone category! Skipping category creation.");
                return;
            }

            _universalZoneCategory = UnityEngine.Object.Instantiate(templateCategory);
            _universalZoneCategory.name = "ZonesUniversal";
            
            if (_universalZoneCategory.components == null)
            {
                _universalZoneCategory.components = new List<ComponentBase>();
            }
            
            _universalZoneCategory.m_Menu = zonesMenuPrefab;

            var uiObject = _universalZoneCategory.GetComponent<UIObject>();
            if (uiObject != null)
            {
                uiObject.m_Icon = "Media/Game/Icons/Zones.svg";
                uiObject.m_Priority = 100;
                uiObject.active = true;
                uiObject.m_IsDebugObject = false;
                uiObject.m_Group = null;
            }
            else
            {
                uiObject = ScriptableObject.CreateInstance<UIObject>();
                uiObject.name = "UIObject";
                uiObject.m_Icon = "Media/Game/Icons/Zones.svg";
                uiObject.m_Priority = 100;
                uiObject.active = true;
                uiObject.m_IsDebugObject = false;
                uiObject.m_Group = null;
                _universalZoneCategory.components.Add(uiObject);
            }

            var themeObject = _universalZoneCategory.GetComponent<ThemeObject>();
            if (themeObject != null)
            {
                _universalZoneCategory.components.Remove(themeObject);
                UnityEngine.Object.Destroy(themeObject);
            }

            _prefabSystem.AddPrefab(_universalZoneCategory);
            
            _prefabSystem.TryGetEntity(_universalZoneCategory, out _universalZoneCategoryEntity);

            Log.Info($"Created Universal Zones category (cloned from {templateCategory.name}):");
            Log.Info($"  Name: {_universalZoneCategory.name}");
            Log.Info($"  Menu: {_universalZoneCategory.m_Menu?.name ?? "null"}");
            Log.Info($"  Icon: {uiObject?.m_Icon ?? "null"}");
            Log.Info($"  Priority: {uiObject?.m_Priority ?? -1}");
            Log.Info($"  Entity: {_universalZoneCategoryEntity.Index}");
        }

        private void CreateUniversalZonePrefabs()
        {
            if (_createdZonePrefabs.Count > 0)
            {
                Log.Info("Universal zone prefabs already created, skipping...");
                return;
            }

            int priority = 0;
            foreach (var definition in ZoneDefinitions.AllZones)
            {
                if (_prefabSystem.TryGetPrefab(new PrefabID("ZonePrefab", definition.Id), out _))
                {
                    Log.Info($"Zone {definition.Id} already exists, skipping...");
                    continue;
                }
                
                var primaryType = definition.SourceZoneTypes[0];
                if (!_templateZones.ContainsKey(primaryType))
                {
                    Log.Info($"No template for {definition.Id} (type: {primaryType}), skipping...");
                    continue;
                }
                
                if (CreateUniversalZonePrefab(definition, priority))
                {
                    priority++;
                }
            }

            Log.Info($"Created {_createdZonePrefabs.Count} universal zone prefabs.");
        }

        private bool CreateUniversalZonePrefab(UniversalZoneDefinition definition, int priority)
        {
            var primaryType = definition.SourceZoneTypes[0];

            if (!_templateZones.TryGetValue(primaryType, out var templateZone))
            {
                Log.Warn($"No template zone found for {definition.Name} (type: {primaryType})");
                return false;
            }

            if (!_templateZoneEntities.TryGetValue(primaryType, out var templateEntity))
            {
                Log.Warn($"No template entity found for {definition.Name}");
                return false;
            }

            try
            {
                var universalZone = UnityEngine.Object.Instantiate(templateZone);
                universalZone.name = definition.Id;

                var uiObject = universalZone.GetComponent<UIObject>();
                if (uiObject != null && _universalZoneCategoryEntity != Entity.Null)
                {
                    uiObject.m_Group = _universalZoneCategory;
                    uiObject.m_Priority = priority;
                    
                    Log.Info($"  {definition.Id}: Priority={priority}");
                }

                var themeObject = universalZone.GetComponent<ThemeObject>();
                if (themeObject != null)
                {
                    universalZone.components.Remove(themeObject);
                    UnityEngine.Object.Destroy(themeObject);
                    Log.Info($"  Removed ThemeObject from {definition.Id}");
                }

                _prefabSystem.AddPrefab(universalZone);
                _createdZonePrefabs.Add(universalZone);

                var entity = _prefabSystem.GetEntity(universalZone);
                if (entity != Entity.Null)
                {
                    _universalZoneEntities[definition.Id] = entity;
                    
                    if (EntityManager.HasComponent<ZoneData>(templateEntity))
                    {
                        var templateZoneData = EntityManager.GetComponentData<ZoneData>(templateEntity);
                        Log.Info($"  Template ZoneData: AreaType={templateZoneData.m_AreaType}, ZoneType={templateZoneData.m_ZoneType}, ZoneFlags={templateZoneData.m_ZoneFlags}");
                        
                        if (EntityManager.HasComponent<ZoneData>(entity))
                        {
                            var currentZoneData = EntityManager.GetComponentData<ZoneData>(entity);
                            Log.Info($"  Current ZoneData (before): AreaType={currentZoneData.m_AreaType}, ZoneType={currentZoneData.m_ZoneType}, ZoneFlags={currentZoneData.m_ZoneFlags}");
                            
                            EntityManager.SetComponentData(entity, templateZoneData);
                            
                            var verifiedZoneData = EntityManager.GetComponentData<ZoneData>(entity);
                            Log.Info($"  Verified ZoneData (after): AreaType={verifiedZoneData.m_AreaType}, ZoneType={verifiedZoneData.m_ZoneType}, ZoneFlags={verifiedZoneData.m_ZoneFlags}");
                            
                            if (verifiedZoneData.m_AreaType != templateZoneData.m_AreaType)
                            {
                                Log.Error($"  ZoneData copy FAILED! AreaType mismatch.");
                            }
                        }
                        else
                        {
                            Log.Warn($"  Entity {entity.Index} does not have ZoneData component - adding it!");
                            EntityManager.AddComponentData(entity, templateZoneData);
                            
                            var verifiedZoneData = EntityManager.GetComponentData<ZoneData>(entity);
                            Log.Info($"  Added ZoneData: AreaType={verifiedZoneData.m_AreaType}, ZoneType={verifiedZoneData.m_ZoneType}, ZoneFlags={verifiedZoneData.m_ZoneFlags}");
                        }
                    }
                    else
                    {
                        Log.Warn($"  Template entity {templateEntity.Index} does not have ZoneData!");
                    }

                    if (EntityManager.HasComponent<ThemeData>(entity))
                    {
                        EntityManager.RemoveComponent<ThemeData>(entity);
                        Log.Info($"  Removed ThemeData component from entity {entity.Index}");
                    }
                    
                    Log.Info($"Created universal zone: {definition.Name} (Entity: {entity.Index})");
                    return true;
                }
                
                return false;
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to create universal zone {definition.Name}: {ex.Message}");
                return false;
            }
        }

        private void AddZonesToCategoryBuffer()
        {
            if (_universalZoneCategoryEntity == Entity.Null)
            {
                Log.Warn("Cannot add zones to buffer - category entity is null");
                return;
            }

            // NOTE: We do NOT manually add zones to the category buffer.
            // The UIObject.m_Group property on each zone prefab automatically registers them.
            // Adding them manually causes duplicate entries in the UI.
            
            Log.Info($"Created {_universalZoneEntities.Count} universal zones (registered via UIObject.m_Group)");
        }

        public Entity GetUniversalZoneEntity(string definitionId)
        {
            return _universalZoneEntities.TryGetValue(definitionId, out var entity) ? entity : Entity.Null;
        }

        public ZonePrefab GetUniversalZonePrefab(string definitionId)
        {
            foreach (var prefab in _createdZonePrefabs)
            {
                if (prefab.name == definitionId)
                    return prefab;
            }
            return null;
        }

        public IReadOnlyList<ZonePrefab> CreatedZonePrefabs => _createdZonePrefabs;

        protected override void OnDestroy()
        {
            foreach (var prefab in _createdZonePrefabs)
            {
                if (prefab != null)
                    UnityEngine.Object.Destroy(prefab);
            }
            
            if (_universalZoneCategory != null)
                UnityEngine.Object.Destroy(_universalZoneCategory);

            _createdZonePrefabs.Clear();
            _templateZones.Clear();
            _templateZoneEntities.Clear();
            _universalZoneEntities.Clear();

            base.OnDestroy();
        }
    }
}
