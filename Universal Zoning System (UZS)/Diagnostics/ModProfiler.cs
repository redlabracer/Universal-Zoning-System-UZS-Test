using Unity.Profiling;

namespace UniversalZoningSystem.Diagnostics
{
    /// <summary>
    /// Unity profiler markers for the Universal Zoning System.
    /// All markers are prefixed with "UZS." so they can be found via the search field
    /// of the Unity Profiler (CPU Usage module, Hierarchy/Timeline view).
    /// Usage: <c>using (ModProfiler.SomeMarker.Auto()) { ... }</c>
    /// </summary>
    internal static class ModProfiler
    {
        private const string Prefix = "UZS.";

        // Mod lifecycle
        public static readonly ProfilerMarker ModOnLoad = Create("Mod.OnLoad");
        public static readonly ProfilerMarker ModLoadSettings = Create("Mod.LoadSettings");
        public static readonly ProfilerMarker ModRegisterSystems = Create("Mod.RegisterSystems");
        public static readonly ProfilerMarker ModApplyHarmony = Create("Mod.ApplyHarmony");
        public static readonly ProfilerMarker ModOnDispose = Create("Mod.OnDispose");

        // Harmony
        public static readonly ProfilerMarker HarmonyPrefabInitializePrefix = Create("Harmony.PrefabInitializeSystem.Prefix");

        // UniversalZonePrefabSystem
        public static readonly ProfilerMarker PrefabSystemOnUpdate = Create("ZonePrefabSystem.OnUpdate");
        public static readonly ProfilerMarker PrefabSystemCacheZoneTemplates = Create("ZonePrefabSystem.CacheZoneTemplates");
        public static readonly ProfilerMarker PrefabSystemCollectBuildings = Create("ZonePrefabSystem.CollectAndClassifyBuildings");
        public static readonly ProfilerMarker PrefabSystemModifyZones = Create("ZonePrefabSystem.ModifyZonePrefabs");
        public static readonly ProfilerMarker PrefabSystemAddBuildingsToZone = Create("ZonePrefabSystem.AddBuildingsToZone");
        public static readonly ProfilerMarker PrefabSystemLogResults = Create("ZonePrefabSystem.LogResults");
        public static readonly ProfilerMarker GetZoneClassification = Create("ZonePrefabSystem.GetZoneClassification");

        // UniversalZoneUISystem
        public static readonly ProfilerMarker UISystemOnGamePreload = Create("ZoneUISystem.OnGamePreload");
        public static readonly ProfilerMarker UISystemOnUpdate = Create("ZoneUISystem.OnUpdate");
        public static readonly ProfilerMarker UISystemInitialize = Create("ZoneUISystem.InitializeUniversalZones");
        public static readonly ProfilerMarker UISystemFindTemplates = Create("ZoneUISystem.FindTemplateZones");
        public static readonly ProfilerMarker UISystemCreateCategory = Create("ZoneUISystem.CreateCategory");
        public static readonly ProfilerMarker UISystemCreatePack = Create("ZoneUISystem.CreateAssetPack");
        public static readonly ProfilerMarker UISystemCreateUniversalZones = Create("ZoneUISystem.CreateUniversalZonePrefabs");
        public static readonly ProfilerMarker UISystemCreateCustomZones = Create("ZoneUISystem.CreateCustomZonePrefabs");
        public static readonly ProfilerMarker UISystemEnsureCustomZone = Create("ZoneUISystem.EnsureCustomZonePrefab");
        public static readonly ProfilerMarker UISystemCreateZonePrefab = Create("ZoneUISystem.CreateZonePrefab");
        public static readonly ProfilerMarker UISystemCloneZonePrefab = Create("ZoneUISystem.CreateZonePrefab.Clone");
        public static readonly ProfilerMarker UISystemAddZonePrefab = Create("ZoneUISystem.CreateZonePrefab.AddPrefab");
        public static readonly ProfilerMarker UISystemCopyZoneData = Create("ZoneUISystem.CreateZonePrefab.CopyZoneData");

        // BuildingZoneModifierSystem
        public static readonly ProfilerMarker BuildingSystemOnUpdate = Create("BuildingModifier.OnUpdate");
        public static readonly ProfilerMarker BuildingSystemTryModify = Create("BuildingModifier.TryModifyBuildings");
        public static readonly ProfilerMarker BuildingSystemUniversalClones = Create("BuildingModifier.CloneForUniversalZones");
        public static readonly ProfilerMarker BuildingSystemAllCustomClones = Create("BuildingModifier.CloneForAllCustomZones");
        public static readonly ProfilerMarker BuildingSystemCustomClones = Create("BuildingModifier.CloneForCustomZone");
        public static readonly ProfilerMarker BuildingSystemClassifyZone = Create("BuildingModifier.ClassifyZone");
        public static readonly ProfilerMarker BuildingSystemClonePrefab = Create("BuildingModifier.ClonePrefab");
        public static readonly ProfilerMarker BuildingSystemAddPrefab = Create("BuildingModifier.AddPrefab");
        public static readonly ProfilerMarker BuildingSystemOnDestroy = Create("BuildingModifier.OnDestroy");

        // UniversalZoneBindingSystem
        public static readonly ProfilerMarker BindingSystemOnUpdate = Create("ZoneBinding.OnUpdate");
        public static readonly ProfilerMarker BindingSystemInitCache = Create("ZoneBinding.InitializeZonePrefabCache");
        public static readonly ProfilerMarker BindingSystemSelectZone = Create("ZoneBinding.SelectZone");

        // CustomZoneBindingSystem
        public static readonly ProfilerMarker CustomBindingLoadingComplete = Create("CustomZoneBinding.OnGameLoadingComplete");
        public static readonly ProfilerMarker CustomBindingBuildSourceCatalog = Create("CustomZoneBinding.BuildSourceCatalog");
        public static readonly ProfilerMarker CustomBindingSerializeZones = Create("CustomZoneBinding.SerializeZones");
        public static readonly ProfilerMarker CustomBindingRegistryChanged = Create("CustomZoneBinding.OnRegistryChanged");
        public static readonly ProfilerMarker CustomBindingReloadLocale = Create("CustomZoneBinding.ReloadActiveLocale");
        public static readonly ProfilerMarker CustomBindingSetBuilderOpen = Create("CustomZoneBinding.SetBuilderOpen");
        public static readonly ProfilerMarker CustomBindingSaveZone = Create("CustomZoneBinding.SaveCustomZone");
        public static readonly ProfilerMarker CustomBindingDeleteZone = Create("CustomZoneBinding.DeleteCustomZone");

        // CustomZoneRegistry
        public static readonly ProfilerMarker RegistryLoad = Create("CustomZoneRegistry.Load");
        public static readonly ProfilerMarker RegistrySave = Create("CustomZoneRegistry.Save");

        // Localization
        public static readonly ProfilerMarker LocaleRegister = Create("Locale.Register");
        public static readonly ProfilerMarker LocaleLoad = Create("Locale.LoadTranslation");
        public static readonly ProfilerMarker LocaleExpandEntries = Create("Locale.ExpandEntries");
        public static readonly ProfilerMarker LocaleDescribeCustomZone = Create("Locale.DescribeCustomZone");

        // Helpers
        public static readonly ProfilerMarker RegionFromPrefabName = Create("Region.GetRegionFromPrefabName");

        // DebugSystem
        public static readonly ProfilerMarker DebugVerify = Create("Debug.VerifyUniversalZoneBuildings");

        private static ProfilerMarker Create(string name) => new ProfilerMarker(Prefix + name);
    }
}
