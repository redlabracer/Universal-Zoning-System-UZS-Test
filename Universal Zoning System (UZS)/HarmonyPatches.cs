using HarmonyLib;
using Game.Prefabs;

namespace UniversalZoningSystem
{
    [HarmonyPatch(typeof(PrefabInitializeSystem), "OnUpdate")]
    internal static class PrefabInitializeSystemPatch
    {
        [HarmonyPrefix]
        private static void Prefix(PrefabInitializeSystem __instance)
        {
            if (__instance?.World == null)
                return;

            var buildingZoneModifierSystem = __instance.World.GetExistingSystemManaged<BuildingZoneModifierSystem>();
            buildingZoneModifierSystem?.TryModifyBuildingsFromHarmony();
        }
    }
}
