using System;
using Colossal.IO.AssetDatabase;
using Colossal.Logging;
using Game;
using Game.Modding;
using Game.SceneFlow;
using Unity.Entities;
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

        private LocalizationManager _localizationManager;

        public void OnLoad(UpdateSystem updateSystem)
        {
            Instance = this;
            Log.Info("Universal Zoning System loading...");

            try
            {
                if (GameManager.instance.modManager.TryGetExecutableAsset(this, out var asset))
                {
                    Log.Info($"Mod location: {asset.path}");
                }

                // Initialize settings
                Settings = new ModSettings(this);
                Settings.RegisterInOptionsUI();
                AssetDatabase.global.LoadSettings(nameof(UniversalZoningSystem), Settings, new ModSettings(this));

                // Initialize localization
                _localizationManager = new LocalizationManager(Settings);
                GameManager.instance.localizationManager.AddSource("en-US", _localizationManager);

                // Register systems - use only updateSystem.UpdateAt, don't create systems directly
                // This is safer and won't interfere with other mods
                
                // UI System - creates zone prefabs and category
                updateSystem.UpdateAt<UniversalZoneUISystem>(SystemUpdatePhase.PrefabUpdate);
                
                // Building modifier - creates building duplicates for universal zones
                updateSystem.UpdateAt<BuildingZoneModifierSystem>(SystemUpdatePhase.PrefabUpdate);
                
                // Other helper systems
                updateSystem.UpdateAt<UniversalZonePrefabSystem>(SystemUpdatePhase.PrefabUpdate);

                Log.Info("Universal Zoning System loaded successfully.");
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to load Universal Zoning System: {ex.Message}\n{ex.StackTrace}");
            }
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
