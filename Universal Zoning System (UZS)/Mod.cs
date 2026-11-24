using Colossal.IO.AssetDatabase;
using Colossal.Logging;
using Colossal.UI;
using Game;
using Game.Modding;
using Game.SceneFlow;
using HarmonyLib;
using System;
using System.IO;
using Unity.Entities;
using UniversalZoningSystem.Diagnostics;
using UniversalZoningSystem.Localization;
using UniversalZoningSystem.Settings;

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

        private Harmony _harmony;

        public void OnLoad(UpdateSystem updateSystem)
        {
            using var profilerScope = ModProfiler.ModOnLoad.Auto();

            Instance = this;
            Log.Info("Universal Zoning System loading...");

            if (GameManager.instance.modManager.TryGetExecutableAsset(this, out var asset))
            {
                Log.Info($"Mod location: {asset.path}");
                string modPath = Path.GetDirectoryName(asset.path);
                UIManager.defaultUISystem.AddHostLocation("universalzoningsystem", modPath, false);
            }

            // Initialize settings
            using (ModProfiler.ModLoadSettings.Auto())
            {
                Settings = new ModSettings(this);
                Settings.RegisterInOptionsUI();
                AssetDatabase.global.LoadSettings(nameof(UniversalZoningSystem), Settings, new ModSettings(this));
            }

            // Initialize localization (lang/*.json, managed through Crowdin)
            LocaleLoader.Register(Settings);

            using (ModProfiler.ModRegisterSystems.Auto())
            {
                // Create systems directly like StarQ's AssetUIManager does
                // This ensures they're created during mod loading, not later
                World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<UniversalZoneUISystem>();
                World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<UniversalZoneBindingSystem>();

                // Register systems with update phases
                // UI System - use GameSimulation phase which runs earlier
                updateSystem.UpdateAt<UniversalZoneUISystem>(SystemUpdatePhase.GameSimulation);

                // UI Binding system - needs to run during UIUpdate phase
                updateSystem.UpdateAt<UniversalZoneBindingSystem>(SystemUpdatePhase.UIUpdate);
                Log.Info("UniversalZoneBindingSystem registered with UIUpdate phase.");

                CustomZones.CustomZoneRegistry.EnsureLoaded();
                updateSystem.UpdateAt<CustomZones.CustomZoneBindingSystem>(SystemUpdatePhase.UIUpdate);

                // Core systems run during PrefabUpdate
                // UniversalZonePrefabSystem - caches zone templates and classifies zones
                updateSystem.UpdateAt<UniversalZonePrefabSystem>(SystemUpdatePhase.PrefabUpdate);

                // Building modifier must run after zone prefabs are created
                // This is the main system that clones buildings for universal zones
                updateSystem.UpdateAt<BuildingZoneModifierSystem>(SystemUpdatePhase.PrefabUpdate);

                // Debug/diagnostic systems (can be disabled in production)
                updateSystem.UpdateAt<DebugSystem>(SystemUpdatePhase.PrefabUpdate);
            }

            using (ModProfiler.ModApplyHarmony.Auto())
            {
                try
                {
                    _harmony = new Harmony("redlabracer.universalzoningsystem");
                    _harmony.PatchAll(typeof(Mod).Assembly);
                    BuildingZoneModifierSystem.UseHarmonyTrigger = true;
                    Log.Info("Harmony patches applied.");
                }
                catch (Exception ex)
                {
                    BuildingZoneModifierSystem.UseHarmonyTrigger = false;
                    Log.Error($"Failed to apply Harmony patches: {ex.Message}");
                }
            }

            Log.Info("Universal Zoning System loaded successfully.");
        }

        public void OnDispose()
        {
            using var profilerScope = ModProfiler.ModOnDispose.Auto();

            Log.Info("Universal Zoning System disposed.");

            if (Settings != null)
            {
                Settings.UnregisterInOptionsUI();
                Settings = null;
            }

            if (_harmony != null)
            {
                _harmony.UnpatchAll(_harmony.Id);
                _harmony = null;
            }

            BuildingZoneModifierSystem.UseHarmonyTrigger = false;

            Instance = null;
        }
    }
}
