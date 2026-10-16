using BepInEx.Configuration;

namespace FishBetter
{
    internal static class FishBetterConfig
    {
        private const string General = "1 - General";
        private const string Bait = "2 - Bait tiers";
        private const string Bite = "3 - Bite and hook";
        private const string Fight = "4 - Fighting the fish";
        private const string Reel = "5 - Reeling";
        private const string Rewards = "6 - Catch rewards";

        public static ConfigEntry<bool> Enabled;

        public static ConfigEntry<bool> BaitTiersEnabled;
        public static ConfigEntry<string> BaitTierList;
        public static ConfigEntry<string> FishTierOverrides;
        public static ConfigEntry<float> HookBonusPerTier;
        public static ConfigEntry<float> StarChancePerTier;

        public static ConfigEntry<float> HookChanceBonusAtMax;
        public static ConfigEntry<float> BiteChanceBonusAtMax;
        public static ConfigEntry<float> MaxBiteChance;

        public static ConfigEntry<float> EscapeDurationAtMax;
        public static ConfigEntry<float> EscapeWaitAtMax;
        public static ConfigEntry<float> LineBreakDistanceAtMax;

        public static ConfigEntry<float> ReelSpeedAtMax;
        public static ConfigEntry<float> ReelStaminaAtMax;
        public static ConfigEntry<float> HookedStaminaAtMax;
        public static ConfigEntry<float> AutoCatchDistance;

        public static ConfigEntry<float> ExtraFishChanceAtMax;
        public static ConfigEntry<float> StarUpChanceAtMax;
        public static ConfigEntry<float> KeepBaitChanceAtMax;
        public static ConfigEntry<float> CatchXp;

        public static void Bind(ConfigFile config)
        {
            Enabled = config.Bind(General, "Enabled", true,
                new ConfigDescription("Turn FishBetter on or off.",
                    null, new ConfigurationManagerAttributes { Order = 1 }));

            BaitTiersEnabled = config.Bind(Bait, "Enabled", true,
                new ConfigDescription("Fish only bite bait from their own biome tier or a later one. Later-tier bait bites more often.",
                    null, new ConfigurationManagerAttributes { Order = 4 }));
            BaitTierList = config.Bind(Bait, "BaitTiers",
                "FishingBait:1, FishingBaitForest:2, FishingBaitSwamp:3, FishingBaitCave:4, FishingBaitOcean:4, FishingBaitPlains:5, FishingBaitMistlands:6, FishingBaitAshlands:7, FishingBaitDeepNorth:8",
                new ConfigDescription("Bait prefab:tier pairs, comma separated. Bait not listed keeps vanilla behaviour.",
                    null, new ConfigurationManagerAttributes { Order = 3 }));
            FishTierOverrides = config.Bind(Bait, "FishTierOverrides", "",
                new ConfigDescription("Optional fish prefab:tier pairs (e.g. Fish1:1). By default a fish's tier is the highest tier among the baits it accepts in vanilla.",
                    null, new ConfigurationManagerAttributes { Order = 2 }));
            HookBonusPerTier = config.Bind(Bait, "HookBonusPerTier", 0.1f,
                new ConfigDescription("Added chance per tier the bait is above the fish that the fish swims to your float. (Vanilla bait always bites, so tiers boost this and stars instead.)",
                    new AcceptableValueRange<float>(0f, 1f), new ConfigurationManagerAttributes { Order = 1 }));
            StarChancePerTier = config.Bind(Bait, "StarChancePerTier", 0.05f,
                new ConfigDescription("Added chance per tier the bait is above the fish that the catch gains a star.",
                    new AcceptableValueRange<float>(0f, 1f), new ConfigurationManagerAttributes { Order = 0 }));

            HookChanceBonusAtMax = config.Bind(Bite, "HookChanceBonusAtMax", 0.6f,
                new ConfigDescription("At skill 100, this share of the remaining chance is added to how often a fish swims to your float (vanilla 50%, so 0.6 gives 80%).",
                    new AcceptableValueRange<float>(0f, 1f), new ConfigurationManagerAttributes { Order = 3 }));
            BiteChanceBonusAtMax = config.Bind(Bite, "BiteChanceBonusAtMax", 0.3f,
                new ConfigDescription("Bite chance added at skill 100. Vanilla fish already bite their bait 100% of the time, so this only matters for modded fish with lower bait chances.",
                    new AcceptableValueRange<float>(0f, 1f), new ConfigurationManagerAttributes { Order = 2 }));
            MaxBiteChance = config.Bind(Bite, "MaxBiteChance", 0.95f,
                new ConfigDescription("Upper limit on bite chance.",
                    new AcceptableValueRange<float>(0f, 1f), new ConfigurationManagerAttributes { Order = 1 }));

            EscapeDurationAtMax = config.Bind(Fight, "EscapeDurationAtMax", 0.4f,
                new ConfigDescription("Multiplier on how long a hooked fish thrashes (escaping halves reel speed and doubles stamina use) at skill 100.",
                    new AcceptableValueRange<float>(0.05f, 1f), new ConfigurationManagerAttributes { Order = 3 }));
            EscapeWaitAtMax = config.Bind(Fight, "EscapeWaitAtMax", 2f,
                new ConfigDescription("Multiplier on the calm gap between escape attempts at skill 100.",
                    new AcceptableValueRange<float>(1f, 10f), new ConfigurationManagerAttributes { Order = 2 }));
            LineBreakDistanceAtMax = config.Bind(Fight, "LineBreakDistanceAtMax", 1.75f,
                new ConfigDescription("Multiplier on how far the line can stretch before it snaps at skill 100.",
                    new AcceptableValueRange<float>(1f, 5f), new ConfigurationManagerAttributes { Order = 1 }));

            AutoCatchDistance = config.Bind(Reel, "AutoCatchDistance", 2.5f,
                new ConfigDescription("A hooked fish this close to you (metres) is caught and goes straight into your inventory. 0 turns it off.",
                    new AcceptableValueRange<float>(0f, 10f), new ConfigurationManagerAttributes { Order = 4 }));
            ReelSpeedAtMax = config.Bind(Reel, "ReelSpeedAtMax", 3f,
                new ConfigDescription("Reel speed at skill 100 in m/s (vanilla 2, skill 0 is 1).",
                    new AcceptableValueRange<float>(1f, 10f), new ConfigurationManagerAttributes { Order = 3 }));
            ReelStaminaAtMax = config.Bind(Reel, "ReelStaminaAtMax", 0.1f,
                new ConfigDescription("Reeling stamina cost multiplier at skill 100 (vanilla 0.2).",
                    new AcceptableValueRange<float>(0f, 1f), new ConfigurationManagerAttributes { Order = 2 }));
            HookedStaminaAtMax = config.Bind(Reel, "HookedStaminaAtMax", 0.05f,
                new ConfigDescription("Stamina per second drained while a fish is on the line at skill 100 (vanilla 0.2, skill 0 is 1).",
                    new AcceptableValueRange<float>(0f, 1f), new ConfigurationManagerAttributes { Order = 1 }));

            ExtraFishChanceAtMax = config.Bind(Rewards, "ExtraFishChanceAtMax", 0.3f,
                new ConfigDescription("Chance at skill 100 to land a second fish of the same kind and stars.",
                    new AcceptableValueRange<float>(0f, 1f), new ConfigurationManagerAttributes { Order = 4 }));
            StarUpChanceAtMax = config.Bind(Rewards, "StarUpChanceAtMax", 0.25f,
                new ConfigDescription("Chance at skill 100 for the caught fish to gain one star (up to its max).",
                    new AcceptableValueRange<float>(0f, 1f), new ConfigurationManagerAttributes { Order = 3 }));
            KeepBaitChanceAtMax = config.Bind(Rewards, "KeepBaitChanceAtMax", 0.4f,
                new ConfigDescription("Chance at skill 100 to get the bait back after a catch.",
                    new AcceptableValueRange<float>(0f, 1f), new ConfigurationManagerAttributes { Order = 2 }));
            CatchXp = config.Bind(Rewards, "CatchXp", 10f,
                new ConfigDescription("Fishing XP for landing a fish, times its quality (1 = no stars). Vanilla only gives about 1 per second of reeling. 0 turns it off.",
                    new AcceptableValueRange<float>(0f, 100f), new ConfigurationManagerAttributes { Order = 1 }));
        }

        // Read by name via reflection by Configuration Manager; only the fields used here are declared.
#pragma warning disable 0649
        private sealed class ConfigurationManagerAttributes
        {
            public int? Order;
        }
#pragma warning restore 0649
    }
}
