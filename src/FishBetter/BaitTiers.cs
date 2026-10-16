using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FishBetter
{
    // Biome tiers for bait and fish. A fish's tier is the highest-tier bait it accepts in vanilla
    // (its own biome's bait), unless overridden in config.
    internal static class BaitTiers
    {
        private static Dictionary<string, int> s_baitTiers;
        private static Dictionary<string, int> s_fishOverrides;
        private static readonly Dictionary<string, int> s_fishTierCache = new Dictionary<string, int>();

        public static void Init()
        {
            Reload();
            FishBetterConfig.BaitTierList.SettingChanged += (_, __) => Reload();
            FishBetterConfig.FishTierOverrides.SettingChanged += (_, __) => Reload();
        }

        private static void Reload()
        {
            s_baitTiers = ParsePairs(FishBetterConfig.BaitTierList.Value);
            s_fishOverrides = ParsePairs(FishBetterConfig.FishTierOverrides.Value);
            s_fishTierCache.Clear();
        }

        private static Dictionary<string, int> ParsePairs(string text)
        {
            var result = new Dictionary<string, int>();
            foreach (string entry in text.Split(','))
            {
                string[] parts = entry.Split(':');
                if (parts.Length == 2 && int.TryParse(parts[1].Trim(), out int tier) && parts[0].Trim().Length > 0)
                    result[parts[0].Trim()] = tier;
                else if (entry.Trim().Length > 0)
                    FishBetterPlugin.Log.LogWarning($"Ignoring malformed tier entry '{entry.Trim()}'");
            }
            return result;
        }

        // 0 means unknown: fall back to vanilla rules.
        public static int BaitTier(string bait)
        {
            return bait != null && s_baitTiers.TryGetValue(bait, out int tier) ? tier : 0;
        }

        public static int FishTier(Fish fish)
        {
            string name = Utils.GetPrefabName(fish.gameObject);
            if (s_fishTierCache.TryGetValue(name, out int cached))
                return cached;
            if (!s_fishOverrides.TryGetValue(name, out int tier))
            {
                tier = 0;
                foreach (Fish.BaitSetting setting in fish.m_baits)
                {
                    if (setting.m_bait)
                        tier = Mathf.Max(tier, BaitTier(setting.m_bait.name));
                }
            }
            s_fishTierCache[name] = tier;
            return tier;
        }

        // Null means vanilla decides (bait or fish has no known tier).
        public static float? BiteChance(Fish fish, string bait, float skill)
        {
            int fishTier = FishTier(fish);
            int baitTier = BaitTier(bait);
            if (fishTier == 0 || baitTier == 0)
                return null;
            if (baitTier < fishTier)
                return 0f;

            float native = -1f, best = 0f;
            foreach (Fish.BaitSetting setting in fish.m_baits)
            {
                if (!setting.m_bait)
                    continue;
                best = Mathf.Max(best, setting.m_chance);
                if (setting.m_bait.name == bait)
                    native = Mathf.Max(native, setting.m_chance);
            }
            float chance = native >= 0f ? native : best;
            float cap = Mathf.Max(FishBetterConfig.MaxBiteChance.Value, chance);
            return Mathf.Min(chance + FishBetterConfig.BiteChanceBonusAtMax.Value * skill, cap);
        }

        // How many tiers the bait is above the fish; 0 when below, equal or unknown.
        public static int TiersAbove(Fish fish, string bait)
        {
            if (!FishBetterConfig.BaitTiersEnabled.Value)
                return 0;
            int fishTier = FishTier(fish);
            int baitTier = BaitTier(bait);
            return fishTier == 0 || baitTier == 0 ? 0 : Mathf.Max(0, baitTier - fishTier);
        }

        // Logged once the prefabs exist so the derived tiers can be checked against the game data.
        public static void LogTable(IEnumerable<GameObject> prefabs)
        {
            var lines = new List<string>();
            foreach (GameObject prefab in prefabs)
            {
                Fish fish = prefab ? prefab.GetComponent<Fish>() : null;
                if (!fish)
                    continue;
                string baits = string.Join(" ", fish.m_baits.Where(b => b.m_bait)
                    .Select(b => $"{b.m_bait.name}({BaitTier(b.m_bait.name)})={b.m_chance:0.##}"));
                lines.Add($"{prefab.name} tier {FishTier(fish)}: {baits}");
            }
            FishBetterPlugin.Log.LogInfo($"Fish tiers ({lines.Count} fish):\n  " + string.Join("\n  ", lines));
        }
    }
}
