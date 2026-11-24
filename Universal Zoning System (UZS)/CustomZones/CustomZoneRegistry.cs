using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Colossal.Logging;
using Colossal.PSI.Environment;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace UniversalZoningSystem.CustomZones
{
    /// <summary>
    /// Global (not savegame-specific) storage of user-defined custom zones.
    /// Stored as JSON in ModsData so the zone prefabs can be created before a save is loaded.
    /// </summary>
    public static class CustomZoneRegistry
    {
        private static readonly ILog Log = Mod.Log;

        private static readonly List<CustomZoneDefinition> _zones = new List<CustomZoneDefinition>();
        private static bool _loaded;

        private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            Converters = { new StringEnumConverter() }
        };

        public static event Action Changed;

        public static string FilePath =>
            Path.Combine(EnvPath.kUserDataPath, "ModsData", nameof(UniversalZoningSystem), "CustomZones.json");

        public static IReadOnlyList<CustomZoneDefinition> Zones
        {
            get
            {
                EnsureLoaded();
                return _zones;
            }
        }

        public static CustomZoneDefinition Get(string id)
        {
            EnsureLoaded();
            return _zones.FirstOrDefault(z => z.Id == id);
        }

        public static void EnsureLoaded()
        {
            if (_loaded)
                return;

            using var profilerScope = Diagnostics.ModProfiler.RegistryLoad.Auto();

            _loaded = true;
            try
            {
                if (!File.Exists(FilePath))
                    return;

                var loaded = JsonConvert.DeserializeObject<List<CustomZoneDefinition>>(File.ReadAllText(FilePath), JsonSettings);
                if (loaded == null)
                    return;

                foreach (var zone in loaded)
                {
                    if (zone == null || string.IsNullOrEmpty(zone.Id) || !zone.Id.StartsWith(CustomZoneDefinition.IdPrefix))
                        continue;
                    zone.SourceZones = zone.SourceZones ?? new List<string>();
                    _zones.Add(zone);
                }

                Log.Info($"Loaded {_zones.Count} custom zones from {FilePath}");
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to load custom zones: {ex.Message}");
            }
        }

        public static void Upsert(CustomZoneDefinition definition)
        {
            EnsureLoaded();
            var index = _zones.FindIndex(z => z.Id == definition.Id);
            if (index >= 0)
                _zones[index] = definition;
            else
                _zones.Add(definition);

            Save();
        }

        public static bool Remove(string id)
        {
            EnsureLoaded();
            if (_zones.RemoveAll(z => z.Id == id) == 0)
                return false;

            Save();
            return true;
        }

        public static string NewId()
        {
            return CustomZoneDefinition.IdPrefix + Guid.NewGuid().ToString("N").Substring(0, 8);
        }

        private static void Save()
        {
            using (Diagnostics.ModProfiler.RegistrySave.Auto())
            {
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
                    File.WriteAllText(FilePath, JsonConvert.SerializeObject(_zones, JsonSettings));
                }
                catch (Exception ex)
                {
                    Log.Error($"Failed to save custom zones: {ex.Message}");
                }
            }

            Changed?.Invoke();
        }
    }
}
