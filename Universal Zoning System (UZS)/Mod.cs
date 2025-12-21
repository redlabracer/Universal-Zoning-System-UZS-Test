using System;
using Colossal.IO.AssetDatabase;
using Colossal.Logging;
using Game;
using Game.Modding;
using Game.SceneFlow;
using Unity.Entities;
using UniversalZoningSystem.Localization;
using UniversalZoningSystem.Settings;
using UniversalZoningSystem.Systems;

namespace UniversalZoningSystem
{
    /// <summary>
    /// Main mod entry point for the Universal Zoning System.
    /// Creates universal zone types that draw from all regional building variants.
    /// </summary>
    public class Mod : IMod
    {
        public static ILog Log { get; } = LogManager.GetLogger(nameof(UniversalZoningSystem))
            .SetShowsErrorsInUI(false);

        public static Mod Instance { get; private set; }
        public static ModSettings Settings { get; private set; }

        private LocalizationManager _localizationManager;

        public void OnLoad(UpdateSystem updateSystem)
        {
            Instance = this;
            Log.Info("Universal Zoning System loading...");

            if (GameManager.instance.modManager.TryGetExecutableAsset(this, out var asset))
            {
                Log.Info($"Mod location: {asset.path}");
            }

            Settings = new ModSettings(this);
            Settings.RegisterInOptionsUI();
            AssetDatabase.global.LoadSettings(nameof(UniversalZoningSystem), Settings, new ModSettings(this));

            _localizationManager = new LocalizationManager(Settings);
            GameManager.instance.localizationManager.AddSource("en-US", _localizationManager);

            World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<UniversalZoneUISystem>();
            World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<UniversalZoneBindingSystem>();
            World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<DistrictSettingsUISystem>();
            World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<UZSPolicySystem>();
            
            updateSystem.UpdateAt<UniversalZoneUISystem>(SystemUpdatePhase.GameSimulation);
            
            updateSystem.UpdateAt<UniversalZoneBindingSystem>(SystemUpdatePhase.UIUpdate);
            updateSystem.UpdateAt<DistrictSettingsUISystem>(SystemUpdatePhase.UIUpdate);
            Log.Info("UniversalZoneBindingSystem registered with UIUpdate phase.");
            
            updateSystem.UpdateAt<UniversalZonePrefabSystem>(SystemUpdatePhase.PrefabUpdate);
            updateSystem.UpdateAt<UZSPolicySystem>(SystemUpdatePhase.PrefabUpdate);
            updateSystem.UpdateAt<BuildingZoneModifierSystem>(SystemUpdatePhase.PrefabUpdate);
            updateSystem.UpdateAt<UniversalBuildingDataSystem>(SystemUpdatePhase.PrefabUpdate);
            updateSystem.UpdateAt<DistrictSpawnEnforcementSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAt<DebugSystem>(SystemUpdatePhase.PrefabUpdate);
            updateSystem.UpdateAt<PolicyExplorerSystem>(SystemUpdatePhase.PrefabUpdate);

            Log.Info("Universal Zoning System loaded successfully.");
        }

        public void OnDispose()
        {
            Log.Info("Universal Zoning System disposed.");

            if (Settings != null)
            {
                Settings.UnregisterInOptionsUI();
                Settings = null;
            }

            Instance = null;
        }
    }
}
