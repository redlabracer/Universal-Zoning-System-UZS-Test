using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Game.SceneFlow;
using Newtonsoft.Json;
using UniversalZoningSystem.Diagnostics;
using UniversalZoningSystem.Settings;

namespace UniversalZoningSystem.Localization
{
    /// <summary>
    /// Registers the translations embedded from the <c>lang</c> folder
    /// (resource name <c>UniversalZoningSystem.l10n.&lt;locale&gt;.json</c>) with the game.
    /// <c>lang/en-US.json</c> is the Crowdin source file; every other file is written by Crowdin.
    /// </summary>
    public static class LocaleLoader
    {
        private const string kResourcePrefix = "UniversalZoningSystem.l10n.";
        private const string kResourceSuffix = ".json";
        private const string kSourceLocale = "en-US";

        public static void Register(ModSettings settings)
        {
            using var profilerScope = ModProfiler.LocaleRegister.Auto();

            var localizationManager = GameManager.instance.localizationManager;
            var assembly = typeof(LocaleLoader).Assembly;

            var resources = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var resourceName in assembly.GetManifestResourceNames())
            {
                if (resourceName.StartsWith(kResourcePrefix, StringComparison.Ordinal) &&
                    resourceName.EndsWith(kResourceSuffix, StringComparison.OrdinalIgnoreCase))
                {
                    var localeId = resourceName.Substring(kResourcePrefix.Length, resourceName.Length - kResourcePrefix.Length - kResourceSuffix.Length);
                    resources[localeId] = resourceName;
                }
            }

            foreach (var localeId in resources.Keys.Where(id => !localizationManager.SupportsLocale(id)))
            {
                Mod.Log.Warn($"Skipping translation '{localeId}': the game does not ship this locale.");
            }

            var sourceEntries = Load(assembly, resources, kSourceLocale, settings);
            if (sourceEntries == null)
            {
                Mod.Log.Error($"Source translation '{kSourceLocale}' is missing or invalid; mod texts will show as keys.");
                sourceEntries = new Dictionary<string, string>();
            }

            // A source is registered for every game locale, even without a translation file, so the
            // player's custom zone names are available in every language. Missing strings fall back to English.
            var locales = localizationManager.GetSupportedLocales()
                .OrderBy(id => id == kSourceLocale ? 0 : 1)
                .ThenBy(id => id, StringComparer.Ordinal);

            foreach (var localeId in locales)
            {
                var entries = localeId == kSourceLocale
                    ? sourceEntries
                    : Load(assembly, resources, localeId, settings) ?? new Dictionary<string, string>();

                var source = new JsonLocaleSource(localeId, entries, sourceEntries);
                localizationManager.AddSource(localeId, source);

                if (source.Count > 0)
                {
                    Mod.Log.Info($"Loaded translation '{localeId}' ({source.Count} entries).");
                }
            }
        }

        private static Dictionary<string, string> Load(Assembly assembly, Dictionary<string, string> resources, string localeId, ModSettings settings)
        {
            if (!resources.TryGetValue(localeId, out var resourceName))
            {
                return null;
            }

            using var profilerScope = ModProfiler.LocaleLoad.Auto();

            try
            {
                using (var stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream == null)
                    {
                        throw new FileNotFoundException($"Embedded resource '{resourceName}' not found.");
                    }

                    using (var reader = new StreamReader(stream, Encoding.UTF8))
                    {
                        var raw = JsonConvert.DeserializeObject<Dictionary<string, string>>(reader.ReadToEnd())
                            ?? new Dictionary<string, string>();
                        return JsonLocaleSource.ExpandEntries(localeId, raw, settings);
                    }
                }
            }
            catch (Exception ex)
            {
                Mod.Log.Error(ex, $"Failed to load translation '{localeId}'.");
                return null;
            }
        }
    }
}
