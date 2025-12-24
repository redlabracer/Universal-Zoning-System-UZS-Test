# Universal Zoning System (UZS)

I started working on **Universal Zoning System (UZS)** because I honestly missed the variety in my districts. This mod basically lets you paint zones that pull buildings from *every* installed content pack—NA, EU, Japan, whatever you've got—randomly within the same block.

Instead of locking a district to just one style, this lets you mix North American suburbs, European row houses, and Asian assets side-by-side.

**ALPHA STATUS: Needs Testing!**

Just a heads up: This is still in early Alpha. I'm looking for volunteers to help me break it (and hopefully fix it). It’s stable enough to run, but please **backup your saves** before using it on a city you care about.

## What does it actually do?

It adds a new **"Universal" tab** to your Zones menu with 10 new zoning options. When you paint these, the game grabs assets from all your active DLCs/Regions based on standard demand and land value.

The new zones include:

- **Residential:** Low (mixes suburbs/villas), Row (mixes brownstones/terraces), Medium, and High density.
- **Commercial:** Low & High density (mixes boutiques, big box stores, etc).
- **Office:** Low & High density.
- **Others:** Mixed Use and Low Rent housing.

*Note: Your standard NA/EU zones are untouched and will still work exactly as before if you want specific control.*

## Installation & Usage

Grab the alpha release from PDX Mods. Once installed:

1. Open the Zoning menu and look for the new **Universal** tab.
2. Pick a zone (e.g., Universal High Res) and paint it like normal.
3. Watch the game spawn a mix of buildings.
4. **Optional:** Go to `Options → Mods → Universal Zoning System` if you want to toggle specific regions off (e.g., if you want a mix of everything *except* UK assets).

### Supported Regions

Currently, the mod pulls from pretty much everything: North America, Europe, UK, Germany, France, Netherlands, East Europe, Japan, China, and generic assets.

## Helping with the Alpha

If (or when) you run into bugs, I'd appreciate a report.

Please grab these two log files and send them my way (Discord or PDX Mods):

`%LocalAppData%\Colossal Order\Cities Skylines II\Logs\UniversalZoningSystem.log`
`%LocalAppData%\Colossal Order\Cities Skylines II\Logs\Player.log`

## How it works under the hood

For those curious: On game load, the mod scans all your building prefabs. It creates "clones" of them and assigns these clones to the new Universal Zone definitions. 

It ends up creating about **7,000+ clones** on the first load, so your **game startup might take a few seconds longer than usual**. This is normal.

**Known Limitations:**
- Signature/Unique buildings aren't cloned (kept them exclusive on purpose).
- Zone colors on the map are just visual indicators; they don't change the underlying logic.
- Buildings still respect their original level and size constraints.

## Future Plans

I'm hoping to polish this up with custom icons and maybe a way to weigh the probabilities (e.g., 80% EU, 20% US). Also looking into support for custom asset packs once the community releases more of them.

## Credits

Big thanks to the CS2 modding discord for the documentation. Special shoutout to **@luca** and **StarQ** for pointing me in the right direction when I got stuck!
