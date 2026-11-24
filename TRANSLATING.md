# Translating Universal Zoning System (UZS)

Translations are managed on **Crowdin**: <!-- TODO: replace with the public project link -->
`https://crowdin.com/project/<your-project>`

Anyone can join the project and translate in the browser. No GitHub account or coding is needed.

## Supported languages

The mod can only show languages that Cities: Skylines II itself ships:

| Crowdin language        | Game locale |
| ----------------------- | ----------- |
| English (source)        | `en-US`     |
| German                  | `de-DE`     |
| Spanish                 | `es-ES`     |
| French                  | `fr-FR`     |
| Italian                 | `it-IT`     |
| Japanese                | `ja-JP`     |
| Korean                  | `ko-KR`     |
| Polish                  | `pl-PL`     |
| Portuguese (Brazil)     | `pt-BR`     |
| Russian                 | `ru-RU`     |
| Chinese (Simplified)    | `zh-HANS`   |
| Chinese (Traditional)   | `zh-HANT`   |

Strings that are not translated yet fall back to English in-game.

## Notes for translators

- Translate only the text. Keep **Universal Mod Menu** as it is; it is the name of another mod.
- Keep the texts short; most of them are labels in the options screen, the zone toolbar or a small in-game panel.
- `Options.OPTION:<Name>` is the label of a setting, `Options.OPTION_DESCRIPTION:<Name>` is its tooltip.
- `Assets.NAME[...]` / `Assets.DESCRIPTION[...]` are the names and tooltips of the zones in the zoning toolbar.

## Notes for developers

- `Universal Zoning System (UZS)/lang/en-US.json` is the **only** file you edit by hand. Every other file in `lang/` is written by Crowdin and arrives through the automated *New translations from Crowdin* pull request.
- All files in `lang/` are embedded into the DLL at build time and registered by `Localization/LocaleLoader.cs`. A source is registered for every game locale, so custom zone names created by the player show up in every language.
- Settings keys are written in a short form and expanded to the game's real option keys when loading:

  | Key in JSON                                      | Meaning                          |
  | ------------------------------------------------ | -------------------------------- |
  | `Options.SECTION`                                | Mod name in the options menu     |
  | `Options.TAB:<tab>`                              | Options tab                      |
  | `Options.GROUP:<group>`                          | Options group header             |
  | `Options.OPTION:<Property>`                      | Setting label                    |
  | `Options.OPTION_DESCRIPTION:<Property>`          | Setting tooltip                  |
  | `Options.ENUM:<EnumType>.<Value>`                | Dropdown value                   |
  | anything else (e.g. `UniversalZoning.*`, `Assets.*`) | Used as-is                   |

  A typo in an `Options.*:` key or a reference to a setting that does not exist is reported in `UniversalZoningSystem.log` and that single entry is skipped.
- Adding a new setting: add the property in `Settings/ModSettings.cs`, add its `Options.OPTION:` / `Options.OPTION_DESCRIPTION:` entries to `en-US.json`, push to `master`. The Crowdin workflow uploads the new strings automatically.
- Adding a new UI string: add the key to `en-US.json` and use it in the UI with `translate('UniversalZoning.…', 'English fallback')`.
