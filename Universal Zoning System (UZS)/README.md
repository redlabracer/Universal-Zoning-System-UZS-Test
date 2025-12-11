# Universal Zoning System (UZS)

This is a mod enabling **universal zoning** in Cities: Skylines II, allowing buildings from all regional content packs to spawn in unified zone types.

**Now in ALPHA TESTING stage: Volunteers needed!**

The goal is to create diverse, multi-cultural cities by mixing architectural styles from North America, Europe, Japan, China, and more - all within a single zone.
The mod is inspired by the desire to build realistic cosmopolitan cities without being locked into regional building styles.

## General Info

- Adds **10 Universal Zone Types** to the game, available in the Zones menu under a "Universal" category
- Universal zones combine buildings from ALL installed regional content packs
- Buildings still follow vanilla spawning rules (demand, land value, pollution, desirability)
- Original EU/NA zones remain fully functional and unchanged
- Enable/disable specific regions through mod settings
- PDX Mods compatible - no Harmony patches required

### Universal Zone Types

| Zone | Description |
|------|-------------|
| **Universal Low Residential** | Single-family homes from all regions - American suburbs, European villas, Japanese houses |
| **Universal Row Residential** | Townhouses and row homes worldwide - Boston brownstones, London terraces, Dutch canal houses |
| **Universal Medium Residential** | Mid-rise apartments globally - Parisian Haussmann, Berlin blocks, Tokyo apartments |
| **Universal High Residential** | Skyscrapers and towers from every culture - Manhattan high-rises, Hong Kong towers |
| **Universal Low Rent** | Affordable housing from all regions - Social housing, public apartments |
| **Universal Mixed Use** | Ground-floor retail with residential above - Global mixed-use developments |
| **Universal Low Commercial** | Small shops and boutiques - Corner stores, cafes, local businesses |
| **Universal High Commercial** | Major retail and department stores - Shopping centers, flagship stores |
| **Universal Low Office** | Small professional buildings - Local offices, clinics |
| **Universal High Office** | Corporate towers and business centers - Global headquarters |

## UZS Alpha Test

### Disclaimer
This is an experimental, likely unstable release. Expect issues, bugs, and potential game crashes. Always backup your saves. Use at your own risk.

Please head to PDXMods to install the private alpha of Universal Zoning System and join testing.

### Reporting Issues
Please report any issues or feedback you can share.

**Include the following logs:**
C:\Users<user>\AppData\LocalLow\Colossal Order\Cities Skylines II\Logs\UniversalZoningSystem.log 
C:\Users<user>\AppData\LocalLow\Colossal Order\Cities Skylines II\Logs\Player.log

## Usage Info

1. **Find Universal Zones** in the Zones menu - look for the "Universal" category tab
2. **Select a Universal Zone** type (Low Residential, High Commercial, etc.)
3. **Paint zones** near roads just like vanilla zones
4. **Wait for buildings** to spawn - they'll come from multiple regions based on demand
5. **Configure regions** in Options → Mods → Universal Zoning System

### Mod Settings
Access via **Options → Mods → Universal Zoning System**

- **Region Toggles**: Enable/Disable specific regions (North America, Europe, Japan, etc.)
- **Verbose Logging**: Enable detailed logging for troubleshooting

### Supported Regions

| Flag | Region | Building Prefixes |
|------|--------|-------------------|
| 🇺🇸 | North America | NA_, USNE_, USSW_ |
| 🇪🇺 | European | EU_ |
| 🇬🇧 | United Kingdom | UK_ |
| 🇩🇪 | Germany | GER_, DE_ |
| 🇫🇷 | France | FR_ |
| 🇳🇱 | Netherlands | NL_ |
| 🇵🇱 | Eastern Europe | EE_ |
| 🇯🇵 | Japan | JP_ |
| 🇨🇳 | China | CN_ |
| 🌐 | Generic | (non-regional buildings) |

## How It Works

The mod creates clones of existing building prefabs and reassigns them to new universal zone prefabs:

1. On game load, UZS scans all installed building prefabs
2. Buildings are categorized by zone type and region
3. Clones are created that reference the universal zone instead of regional zones
4. When you paint a universal zone, the game's spawn system finds ALL matching buildings
5. Buildings spawn naturally following vanilla demand/desirability rules

**Important**: Original regional zones (EU/NA Low Residential, etc.) continue to work normally with their original buildings only.

## Known Issues

- First load after enabling mod may take slightly longer due to building cloning (~7000+ clones created)
- Some signature/unique buildings may not clone (by design - they remain exclusive)
- Zone colors match the template zone used (visual only, doesn't affect function)
- Buildings follow their original level/size requirements

## Additional Thoughts

Long-term, I have hopes to expand this with:
- Custom zone colors and icons for universal zones
- Per-zone region weighting (control spawn probability by region)
- Integration with other zoning mods
- Support for custom/modded building packs

## Credits

- Built using the Cities: Skylines II modding framework
- Thanks to the CS2 modding community for documentation and support
- Inspired by the desire for diverse, cosmopolitan cityscapes