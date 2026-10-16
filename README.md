# FishBetter

A private, client-side Valheim mod built on BepInEx and Jötunn. **Not for publishing.**

## Description

Reworks the Fishing skill so it matters: higher skill improves bite and hook chance, tames escaping fish, reels faster for less stamina, and rewards catches with bonus fish, extra stars and saved bait. Bait is tiered by biome — fish never take bait from an earlier biome than their own, and later-biome bait works better on earlier fish.

## Installation

Place `FishBetter.dll` in `BepInEx/plugins`. Requires BepInEx and Jotunn.

## Features

Everything below scales linearly with your Fishing skill (0 at level 0, the full value at level 100) and can be changed in config.

- **Bait tiers**: every bait belongs to a biome tier (Meadows 1, Black Forest 2, Swamp 3, Mountain/Ocean 4, Plains 5, Mistlands 6, Ashlands 7, Deep North 8). A fish's tier is the highest tier among the baits it takes in vanilla. Fish never bite bait from a lower tier than their own. They bite any bait of their tier or higher. Vanilla fish always bite their own bait, so each tier above theirs instead adds +10% to how often fish swim to your float and +5% to the chance of a bonus star.
- **Bite and hook**: fish swim to your float more often (50% → 80%) and bite more often (+30%, capped at 95%). Vanilla fish already bite their own bait every time, so the bite bonus only matters for modded fish.
- **Fighting the fish**: escape thrashing lasts 60% less, calm gaps between escapes last twice as long, and the line stretches 75% further before it snaps.
- **Reeling**: max-skill reel speed is 3 m/s (vanilla 2). Reeling stamina drops to 0.15× (vanilla 0.2×), and the drain while a fish is hooked to 0.1/s (vanilla 0.2).
- **Auto-catch**: a hooked fish reeled within 2.5 m of you is caught straight into your inventory.
- **Catch rewards**: 30% chance of a bonus fish of the same kind and stars, 25% chance of +1 star, and 40% chance to keep your bait.
- **XP on catch**: 10 Fishing XP × fish quality for every catch, on top of vanilla's reeling XP.
- **Bait recipes**: each biome bait at the prep table costs 100 regular bait + 1 trophy and makes 100 bait (vanilla 20 + 1 → 20). The trophies now come from common mobs of each bait's own biome: Greydwarf (Mossy), Leech (Sticky), Wolf (Cold), Serpent (Heavy), Deathsquito (Stingy), Seeker (Misty), Asksvin (Hot) and Drake hatchling (Frosty; Deep North has no mobs yet).

The mod logs a table of every fish, its tier and its baits at startup (`Fish tiers (...)` in `BepInEx/LogOutput.log`).

Skill effects only apply to your own casts. Another player's float near fish you control gets vanilla skill behaviour, but the bait-tier rule still applies.

## Settings

- **Enabled**: on by default. Turns the mod on or off without restarting the game (Configuration Manager, F1).
- **Bait tiers**: `Enabled`, `BaitTiers` (bait:tier list), `FishTierOverrides` (fish:tier list), `HookBonusPerTier`, `StarChancePerTier`.
- **Bite and hook**: `HookChanceBonusAtMax`, `BiteChanceBonusAtMax`, `MaxBiteChance`.
- **Fighting the fish**: `EscapeDurationAtMax`, `EscapeWaitAtMax`, `LineBreakDistanceAtMax`.
- **Reeling**: `AutoCatchDistance`, `ReelSpeedAtMax`, `ReelStaminaAtMax`, `HookedStaminaAtMax`.
- **Catch rewards**: `ExtraFishChanceAtMax`, `StarUpChanceAtMax`, `KeepBaitChanceAtMax`, `CatchXp`.
- **Bait recipes**: `Enabled`, `BaitTrophies` (bait:trophy list), `RegularBaitCost`, `BaitPerCraft`. Applied when a world loads.

## Requirements

- [BepInExPack Valheim](https://thunderstore.io/c/valheim/p/denikson/BepInExPack_Valheim/)
- [Jotunn, the Valheim Library](https://www.nexusmods.com/valheim/mods/1138)
- Configuration Manager (optional). You can also edit `BepInEx/config/com.darkendvoid.fishbetter.cfg` by hand.

## Development

### Setup (Linux)

1. Install the .NET SDK (`sudo apt install dotnet-sdk-10.0`). The plugin targets `net462`.
2. Copy `Environment.props.example` to `Environment.props` and adjust the paths.
3. Register the mod with Amethyst Mod Manager once: `scripts/register-mod.sh`.

### Dev loop

The Amethyst GUI must be closed while scripts deploy, because it locks the mod library.

| Script | Purpose |
|---|---|
| `scripts/deploy.sh [--no-build] [Debug\|Release]` | Build, rescan the Amethyst library, deploy |
| `scripts/launch.sh` / `scripts/stop.sh` | Start or stop Valheim through Steam (Proton) |
| `scripts/smoketest.sh [timeout] [--keep-running]` | Launch the game, wait for `SMOKE_MARKERS` (in `scripts/env.sh`) in the log, stop the game |
| `scripts/log.sh [all\|mod\|errors\|plugins]` | Follow or filter `BepInEx/LogOutput.log` |
| `scripts/bump-version.sh <x.y.z>` | Set the version in the csproj and plugin, and date the CHANGELOG `## Unreleased` section |

This mod is private, so it has no Nexus upload script.
