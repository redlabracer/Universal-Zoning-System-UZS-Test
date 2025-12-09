# Universal Zoning System (UZS)

A Cities: Skylines II mod that creates universal zone types drawing buildings from all regional content packs for truly diverse cityscapes.

## Features

- **10 Universal Zone Types**: Available in a custom UI panel within the zone tool
- **Multi-Region Building Pool**: Draws from NA, EU, UK, Germany, France, Netherlands, Eastern Europe, Japan, China, and more
- **Configurable Regions**: Enable/disable specific regions in settings
- **Custom UI Panel**: Universal zones appear in a dedicated panel when using the zone tool
- **Performance Optimized**: Near-instant initialization with minimal memory footprint
- **DLC-Aware**: Automatically incorporates buildings from installed DLCs
- **PDX Mods Compatible**: Uses the official modding API

## How It Works

The Universal Zoning System works within Cities: Skylines II's modding framework:

1. **Zone Creation**: The mod creates new ZonePrefab instances for each universal zone type
2. **Building Collection**: Scans all spawnable building prefabs and categorizes them by zone type and region
3. **Building Linking**: Creates duplicate buildings that reference universal zones
4. **Custom UI**: A React-based UI panel displays universal zones in the zone tool
5. **Settings Integration**: Players can enable/disable specific regions through the options menu

### How Building Spawning Works

When you paint a Universal zone:
1. The game looks for buildings with `SpawnableBuildingData.m_ZonePrefab` matching the zone
2. Our `BuildingZoneModifierSystem` has created duplicate buildings that reference the universal zone
3. The game's spawn system randomly selects from ALL available duplicate buildings
4. Result: A diverse mix of architectural styles from around the world!

## Project Structure

```
Universal Zoning System (UZS)/
??? Mod.cs                         # Main entry point, system registration
??? UniversalZoningSystem.cs       # Core zone management system
??? UniversalZonePrefabSystem.cs   # Zone prefab caching and classification
??? UniversalZoneUISystem.cs       # Creates zone prefabs
??? UniversalZoneBindingSystem.cs  # UI bindings for React UI
??? BuildingZoneModifierSystem.cs  # Creates building duplicates for universal zones
??? ZoneBuildingLinkerSystem.cs    # Links buildings to zones across regions
??? UniversalZoneCreatorSystem.cs  # Creates universal zone configurations
??? ZoneDefinitions.cs             # Universal zone type definitions
??? RegionPrefixManager.cs         # Regional building prefix mappings
??? BuildingMatcher.cs             # Building-to-zone matching logic
??? BuildingCollector.cs           # Building collection utilities
??? DebugSystem.cs                 # Diagnostic logging
??? Settings/
?   ??? ModSettings.cs             # User-configurable settings
??? Localization/
?   ??? LocalizationManager.cs     # UI text translations
??? UI/
    ??? src/
    ?   ??? index.tsx              # React UI component
    ??? dist/
    ?   ??? index.js               # Built UI (auto-generated)
    ??? package.json               # NPM configuration
    ??? build.mjs                  # UI build script
```

## System Execution Order

The mod registers systems in a specific order to ensure proper initialization:

```
1. UniversalZoningSystem         - Collects and categorizes buildings
2. UniversalZonePrefabSystem     - Caches zone templates, classifies zones
3. ZoneBuildingLinkerSystem      - Analyzes building-zone relationships
4. UniversalZoneCreatorSystem    - Prepares zone configurations
5. UniversalZoneUISystem         - Creates ZonePrefabs, registers with PrefabSystem
6. BuildingZoneModifierSystem    - Modifies SpawnableBuildingData on buildings
7. DebugSystem                   - Logs diagnostics (if enabled)
```

## Universal Zone Types

| Zone Type | ID | Description |
|-----------|-----|-------------|
| Universal Low Residential | UZS_LowResidential | Single-family homes from all regions |
| Universal Row Residential | UZS_RowResidential | Townhouses and row homes worldwide |
| Universal Medium Residential | UZS_MediumResidential | Mid-rise apartments globally |
| Universal High Residential | UZS_HighResidential | Skyscrapers and towers from every culture |
| Universal Low Rent | UZS_LowRent | Affordable housing from all regions |
| Universal Mixed Use | UZS_MixedUse | Ground-floor retail with residential above |
| Universal Low Commercial | UZS_LowCommercial | Small shops and boutiques |
| Universal High Commercial | UZS_HighCommercial | Major retail and department stores |
| Universal Low Office | UZS_LowOffice | Small professional buildings |
| Universal High Office | UZS_HighOffice | Corporate towers and business centers |

## Supported Regions

| Region | Prefix(es) | Description |
|--------|------------|-------------|
| North America | NA_, USNE_, USSW_ | US standard, Northeast, Southwest |
| European | EU_ | Core European buildings |
| United Kingdom | UK_ | Victorian, Georgian, British styles |
| Germany | GER_, DE_ | Bauhaus, German apartment blocks |
| France | FR_ | Parisian Haussmann, provincial styles |
| Netherlands | NL_ | Dutch canal houses, Amsterdam modern |
| Eastern Europe | EE_ | Post-war blocks, traditional styles |
| Japan | JP_ | Dense Tokyo apartments, modern towers |
| China | CN_ | High-density residential, commercial |

The mod automatically detects and incorporates buildings from any future DLC or mod-added regions.

## Settings

Access mod settings through **Options ? Universal Zoning System**:

### Region Settings
Toggle individual regions on/off to customize which building styles appear:
- North America (includes USNE, USSW variants)
- European
- United Kingdom
- Germany
- France
- Netherlands
- Eastern Europe
- Japan
- China

### Weighting Settings
- **Equal Region Weighting**: When enabled, each region has an equal chance of being selected regardless of how many buildings it contains.

### Advanced Settings
- **Verbose Logging**: Enable detailed logging for debugging purposes
- **Reset to Defaults**: Reset all settings to their default values

## Technical Details

- **Target Framework**: .NET Framework 4.8
- **Game Version**: Cities: Skylines II
- **Architecture**: Unity ECS (Entity Component System)
- **Mod Framework**: Official CS2 Modding API
- **PDX Mods**: Fully compatible

### Key Technical Approach

The mod works by:
1. Creating new `ZonePrefab` instances via `ScriptableObject.CreateInstance<ZonePrefab>()`
2. Registering them with `PrefabSystem.AddPrefab()` so they appear in the UI
3. Modifying `SpawnableBuildingData.m_ZonePrefab` on building entities to point to our universal zones
4. The game's native spawn system then naturally includes all linked buildings

This approach:
- ? Uses only official modding APIs
- ? Doesn't require Harmony or code injection
- ? Is fully compatible with PDX Mods publishing
- ? Works with future game updates (as long as APIs remain stable)

## Building the Mod

1. Install Cities: Skylines II modding tools
2. Set environment variable: `CSII_TOOLPATH` ? path to modding tools
3. Open `Universal Zoning System (UZS).sln` in Visual Studio
4. Build (F6 or Ctrl+Shift+B)
5. The mod will be output to the game's mod folder

## Publishing to PDX Mods

The project includes publish profiles in `Properties/PublishProfiles/`:
- `PublishNewMod.pubxml` - First-time publishing
- `PublishNewVersion.pubxml` - Update existing mod

## Troubleshooting

### Zones don't appear in vanilla toolbar
The vanilla zoning toolbar is built during game initialization and doesn't support dynamically added zones. 

**Current workaround options:**
1. Use the **Find It** mod to search for and place universal zones by name (e.g., "UZS_LowResidential")
2. Use the **Asset Menu Helper** mod if available
3. The zones ARE registered in the game - they just need a UI to access them

**Why this happens:**
- The game's toolbar UI is built from prefabs during initialization
- Prefabs added via `PrefabSystem.AddPrefab()` after this point don't automatically appear
- This is a limitation of the vanilla modding API, not a bug in this mod

**Future solutions being explored:**
- Custom UI panel for universal zone selection
- Integration with the game's asset menu system
- Hooking into the zone tool selection

### Buildings not spawning in universal zones
- Enable Verbose Logging in settings and check the log file
- Look for "Created X building duplicates for..." messages
- Verify the region is enabled in settings
- Check that buildings exist for that zone type in your installed DLCs

### Original zones broken
If original regional zones stop working, this indicates the mod is modifying buildings incorrectly. The current implementation creates **duplicate** building prefabs rather than modifying originals, which should preserve original zone functionality.

### Log File Location
Logs are written to: `%LOCALAPPDATA%Low\Colossal Order\Cities Skylines II\Logs\UniversalZoningSystem.log`

## Contributing

Contributions are welcome! Key areas for development:
- Testing with various DLC combinations
- UI improvements for zone selection
- Additional region support
- Performance optimization

## License

[Add your license here]
