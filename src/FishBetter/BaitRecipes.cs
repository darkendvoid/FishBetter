using System.Collections.Generic;
using System.Linq;

namespace FishBetter
{
    // Biome bait recipes (regular bait + a trophy at the prep table). Recipes are shared
    // ScriptableObjects, so they're edited in place and the vanilla values kept for restoring.
    internal static class BaitRecipes
    {
        private const string RegularBait = "FishingBait";

        private sealed class Original
        {
            public int Amount;
            public Piece.Requirement[] Resources;
        }

        private static readonly Dictionary<Recipe, Original> s_originals = new Dictionary<Recipe, Original>();

        public static void Apply(ObjectDB db)
        {
            if (db.m_recipes.Count == 0)
                return;
            Dictionary<string, string> trophies = ParseTrophies(FishBetterConfig.BaitTrophies.Value);
            bool enabled = FishBetterConfig.Enabled.Value && FishBetterConfig.RecipesEnabled.Value;
            int changed = 0;
            foreach (Recipe recipe in db.m_recipes)
            {
                if (!recipe || !recipe.m_item || !recipe.m_item.name.StartsWith(RegularBait)
                    || recipe.m_item.name == RegularBait)
                    continue;
                if (!s_originals.TryGetValue(recipe, out Original original))
                {
                    original = new Original { Amount = recipe.m_amount, Resources = recipe.m_resources };
                    s_originals[recipe] = original;
                }
                recipe.m_amount = original.Amount;
                recipe.m_resources = original.Resources;
                if (!enabled)
                    continue;

                recipe.m_amount = FishBetterConfig.BaitPerCraft.Value;
                recipe.m_resources = original.Resources.Select(r => Rework(db, recipe, r, trophies)).ToArray();
                changed++;
            }
            if (enabled)
                LogRecipes(db, changed);
        }

        private static Piece.Requirement Rework(ObjectDB db, Recipe recipe, Piece.Requirement req,
            Dictionary<string, string> trophies)
        {
            var copy = new Piece.Requirement
            {
                m_resItem = req.m_resItem,
                m_amount = req.m_amount,
                m_amountPerLevel = req.m_amountPerLevel,
                m_extraAmountOnlyOneIngredient = req.m_extraAmountOnlyOneIngredient,
                m_recover = req.m_recover,
            };
            if (!req.m_resItem)
                return copy;
            if (req.m_resItem.name == RegularBait)
            {
                copy.m_amount = FishBetterConfig.RegularBaitCost.Value;
            }
            else if (req.m_resItem.name.StartsWith("Trophy")
                && trophies.TryGetValue(recipe.m_item.name, out string trophyName))
            {
                ItemDrop trophy = db.GetItemPrefab(trophyName)?.GetComponent<ItemDrop>();
                if (trophy)
                    copy.m_resItem = trophy;
                else
                    FishBetterPlugin.Log.LogWarning($"Unknown trophy '{trophyName}' for {recipe.m_item.name}; keeping {req.m_resItem.name}");
                copy.m_amount = 1;
            }
            return copy;
        }

        private static Dictionary<string, string> ParseTrophies(string text)
        {
            var result = new Dictionary<string, string>();
            foreach (string entry in text.Split(','))
            {
                string[] parts = entry.Split(':');
                if (parts.Length == 2 && parts[0].Trim().Length > 0 && parts[1].Trim().Length > 0)
                    result[parts[0].Trim()] = parts[1].Trim();
                else if (entry.Trim().Length > 0)
                    FishBetterPlugin.Log.LogWarning($"Ignoring malformed bait trophy entry '{entry.Trim()}'");
            }
            return result;
        }

        private static void LogRecipes(ObjectDB db, int changed)
        {
            var lines = new List<string>();
            foreach (Recipe recipe in db.m_recipes)
            {
                if (!recipe || !recipe.m_item || !s_originals.ContainsKey(recipe))
                    continue;
                string resources = string.Join(" + ", recipe.m_resources.Where(r => r.m_resItem)
                    .Select(r => $"{r.m_amount}x {r.m_resItem.name}"));
                lines.Add($"{recipe.name}: {resources} -> {recipe.m_amount}x {recipe.m_item.name}"
                    + $" (stack {recipe.m_item.m_itemData.m_shared.m_maxStackSize})");
            }
            FishBetterPlugin.Log.LogInfo($"Bait recipes reworked ({changed}):\n  " + string.Join("\n  ", lines));
        }
    }
}
