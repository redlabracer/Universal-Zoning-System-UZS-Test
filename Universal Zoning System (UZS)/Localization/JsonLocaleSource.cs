using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Colossal;
using Game.Modding;
using Game.SceneFlow;
using UniversalZoningSystem.CustomZones;
using UniversalZoningSystem.Diagnostics;
using UniversalZoningSystem.Settings;

namespace UniversalZoningSystem.Localization
{
    /// <summary>
    /// Dictionary source for one game locale, backed by a translation file from the <c>lang</c> folder.
    /// Also yields the names and descriptions of the player's custom zones, which are computed at read time
    /// so every locale stays current after <c>ReloadActiveLocale</c>.
    /// </summary>
    public class JsonLocaleSource : IDictionarySource
    {
        private const string kSectionKey = "Options.SECTION";
        private const string kTabPrefix = "Options.TAB:";
        private const string kGroupPrefix = "Options.GROUP:";
        private const string kOptionPrefix = "Options.OPTION:";
        private const string kOptionDescPrefix = "Options.OPTION_DESCRIPTION:";
        private const string kEnumPrefix = "Options.ENUM:";
        private const string kPackedPrefix = "Options.";

        private const string kCustomZoneHeaderKey = "UniversalZoning.CustomZones.DESCRIPTION_HEADER";
        private const string kCustomZoneHeaderDefault = "Custom zone combining:";

        private static readonly MethodInfo s_GetEnumValueLocaleID =
            typeof(ModSetting).GetMethod(nameof(ModSetting.GetEnumValueLocaleID));

        private readonly string m_LocaleId;
        private readonly Dictionary<string, string> m_Entries;
        private readonly Dictionary<string, string> m_FallbackEntries;

        public JsonLocaleSource(string localeId, Dictionary<string, string> entries, Dictionary<string, string> fallbackEntries)
        {
            m_LocaleId = localeId;
            m_Entries = entries ?? new Dictionary<string, string>();
            m_FallbackEntries = fallbackEntries ?? new Dictionary<string, string>();
        }

        public int Count => m_Entries.Count;

        public IEnumerable<KeyValuePair<string, string>> ReadEntries(IList<IDictionaryEntryError> errors, Dictionary<string, int> indexCounts)
        {
            foreach (var entry in m_Entries)
            {
                yield return entry;
            }

            var header = Lookup(kCustomZoneHeaderKey) ?? kCustomZoneHeaderDefault;
            foreach (var custom in CustomZoneRegistry.Zones)
            {
                if (string.IsNullOrWhiteSpace(custom?.Id))
                {
                    continue;
                }

                yield return new KeyValuePair<string, string>($"Assets.NAME[{custom.Id}]", string.IsNullOrEmpty(custom.Name) ? custom.Id : custom.Name);
                yield return new KeyValuePair<string, string>($"Assets.DESCRIPTION[{custom.Id}]", DescribeCustomZone(custom, header));
            }
        }

        public void Unload() { }

        public override string ToString() => $"UniversalZoningSystem.Locale.{m_LocaleId}";

        /// <summary>
        /// Converts the translator-friendly keys of a <c>lang</c> file (e.g. <c>Options.OPTION:EnableJapan</c>)
        /// into the keys the game looks up, using the settings' own key builders.
        /// Invalid keys are logged and skipped so one typo cannot drop a whole language.
        /// </summary>
        public static Dictionary<string, string> ExpandEntries(string localeId, IDictionary<string, string> rawEntries, ModSettings settings)
        {
            using var profilerScope = ModProfiler.LocaleExpandEntries.Auto();

            var result = new Dictionary<string, string>(rawEntries.Count);
            foreach (var entry in rawEntries)
            {
                if (string.IsNullOrWhiteSpace(entry.Key) || string.IsNullOrEmpty(entry.Value))
                {
                    continue;
                }

                try
                {
                    result[ExpandKey(entry.Key, settings)] = entry.Value;
                }
                catch (InvalidDataException ex)
                {
                    Mod.Log.Warn($"[{localeId}] Skipping translation entry: {ex.Message}");
                }
            }

            return result;
        }

        private string Lookup(string key)
        {
            if (m_Entries.TryGetValue(key, out var value) || m_FallbackEntries.TryGetValue(key, out value))
            {
                return value;
            }

            return null;
        }

        private static string DescribeCustomZone(CustomZoneDefinition custom, string header)
        {
            using var profilerScope = ModProfiler.LocaleDescribeCustomZone.Auto();

            var dictionary = GameManager.instance?.localizationManager?.activeDictionary;
            var names = (custom.SourceZones ?? new List<string>()).Select(zone =>
                dictionary != null && dictionary.TryGetValue($"Assets.NAME[{zone}]", out var name) && !string.IsNullOrEmpty(name) ? name : zone);
            return header + "\n" + string.Join("\n", names.Select(n => "• " + n));
        }

        private static string ExpandKey(string key, ModSettings settings)
        {
            if (key == kSectionKey)
            {
                return settings.GetSettingsLocaleID();
            }

            if (key.StartsWith(kTabPrefix, StringComparison.Ordinal))
            {
                return settings.GetOptionTabLocaleID(key.Substring(kTabPrefix.Length));
            }

            if (key.StartsWith(kGroupPrefix, StringComparison.Ordinal))
            {
                return settings.GetOptionGroupLocaleID(key.Substring(kGroupPrefix.Length));
            }

            if (key.StartsWith(kOptionDescPrefix, StringComparison.Ordinal))
            {
                return settings.GetOptionDescLocaleID(RequireProperty(key, kOptionDescPrefix));
            }

            if (key.StartsWith(kOptionPrefix, StringComparison.Ordinal))
            {
                return settings.GetOptionLabelLocaleID(RequireProperty(key, kOptionPrefix));
            }

            if (key.StartsWith(kEnumPrefix, StringComparison.Ordinal))
            {
                return ExpandEnumKey(key, settings);
            }

            // A packed "Options.*:" key that matched none of the prefixes above is a typo;
            // passing it through would silently create a dead entry.
            if (key.StartsWith(kPackedPrefix, StringComparison.Ordinal) && key.IndexOf(':') >= 0 && key.IndexOf('[') < 0)
            {
                throw new InvalidDataException($"Unknown packed locale key prefix in '{key}'.");
            }

            return key;
        }

        private static string RequireProperty(string key, string prefix)
        {
            var propertyName = key.Substring(prefix.Length);
            if (typeof(ModSettings).GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance) == null)
            {
                throw new InvalidDataException($"Locale key '{key}' references unknown setting '{propertyName}'.");
            }

            return propertyName;
        }

        private static string ExpandEnumKey(string key, ModSettings settings)
        {
            var body = key.Substring(kEnumPrefix.Length);
            var dot = body.LastIndexOf('.');
            if (dot <= 0 || dot == body.Length - 1)
            {
                throw new InvalidDataException($"Locale key '{key}' must have the form '{kEnumPrefix}<EnumType>.<Value>'.");
            }

            var typeName = body.Substring(0, dot);
            var enumType = typeof(ModSettings).GetNestedType(typeName, BindingFlags.Public)
                ?? typeof(ModSettings).Assembly.GetType(typeof(ModSettings).Namespace + "." + typeName);
            if (enumType == null || !enumType.IsEnum)
            {
                throw new InvalidDataException($"Locale key '{key}' references unknown enum '{typeName}'.");
            }

            var valueName = body.Substring(dot + 1);
            if (!Enum.IsDefined(enumType, valueName))
            {
                throw new InvalidDataException($"Locale key '{key}' references unknown value '{valueName}' of '{enumType.Name}'.");
            }

            var value = Enum.Parse(enumType, valueName);
            return (string)s_GetEnumValueLocaleID.MakeGenericMethod(enumType).Invoke(settings, new[] { value });
        }
    }
}
