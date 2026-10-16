using HarmonyLib;
using UnityEngine;

namespace FishBetter
{
    [HarmonyPatch]
    internal static class FishingPatches
    {
        // Skill factor (0..1) of the float's owner, or 0 when another player cast it: remote
        // players' skill levels aren't synced, so they get vanilla skill effects.
        private static float OwnerSkill(FishingFloat ff)
        {
            Character owner = ff ? ff.GetOwner() : null;
            return owner && owner == Player.m_localPlayer ? owner.GetSkillFactor(Skills.SkillType.Fishing) : 0f;
        }

        private static bool IsLocal(FishingFloat ff)
        {
            return ff && ff.GetOwner() == Player.m_localPlayer && Player.m_localPlayer;
        }

        private static bool s_loggedTiers;

        // The main menu fills its ObjectDB through CopyOtherDB, so this fires before any world loads.
        [HarmonyPostfix]
        [HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.CopyOtherDB))]
        private static void LogFishTiers(ObjectDB __instance)
        {
            if (s_loggedTiers)
                return;
            s_loggedTiers = true;
            BaitTiers.LogTable(__instance.m_items);
        }

        // The menu copy (CopyOtherDB) and the in-world ObjectDB (Awake) share the same recipe
        // assets; Apply is idempotent and skips the still-empty menu DB at Awake.
        [HarmonyPostfix]
        [HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.CopyOtherDB))]
        private static void ReworkMenuRecipes(ObjectDB __instance) => BaitRecipes.Apply(__instance);

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.Awake))]
        private static void ReworkWorldRecipes(ObjectDB __instance) => BaitRecipes.Apply(__instance);

        // --- Bite and hook -------------------------------------------------------------------

        // Vanilla rolls m_baseHookChance (50%) per float to decide whether a fish swims to it.
        [HarmonyPrefix]
        [HarmonyPatch(typeof(Fish), nameof(Fish.FindFloat))]
        private static bool FindFloat(Fish __instance, ref FishingFloat __result)
        {
            if (!FishBetterConfig.Enabled.Value)
                return true;
            __result = null;
            foreach (FishingFloat ff in FishingFloat.GetAllInstances())
            {
                if (!ff.IsInWater() || ff.GetCatch() != null
                    || Vector3.Distance(__instance.transform.position, ff.transform.position) > ff.m_range)
                    continue;
                float chance = __instance.m_baseHookChance;
                chance += (1f - chance) * FishBetterConfig.HookChanceBonusAtMax.Value * OwnerSkill(ff);
                chance += FishBetterConfig.HookBonusPerTier.Value * BaitTiers.TiersAbove(__instance, ff.GetBait());
                if (Random.value < chance)
                {
                    __result = ff;
                    break;
                }
            }
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Fish), nameof(Fish.TestBate))]
        private static bool TestBait(Fish __instance, FishingFloat ff, ref bool __result)
        {
            if (!FishBetterConfig.Enabled.Value)
                return true;
            float skill = OwnerSkill(ff);
            float? chance = FishBetterConfig.BaitTiersEnabled.Value
                ? BaitTiers.BiteChance(__instance, ff.GetBait(), skill)
                : null;
            if (chance == null)
            {
                // Unknown bait or fish: vanilla bait list, plus the skill bonus on matching bait.
                string bait = ff.GetBait();
                float best = -1f;
                foreach (Fish.BaitSetting setting in __instance.m_baits)
                {
                    if (setting.m_bait && setting.m_bait.name == bait)
                        best = Mathf.Max(best, setting.m_chance);
                }
                chance = best < 0f ? 0f
                    : Mathf.Min(best + FishBetterConfig.BiteChanceBonusAtMax.Value * skill,
                        Mathf.Max(FishBetterConfig.MaxBiteChance.Value, best));
            }
            __result = Random.value < chance.Value;
            return false;
        }

        // --- Fighting the fish ---------------------------------------------------------------

        // Shorter thrashing. Escape runs on the fish's owner, which is the fisher once hooked.
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Fish), nameof(Fish.Escape))]
        private static void ShorterEscape(Fish __instance)
        {
            FishingFloat ff = __instance.m_fishingFloat;
            if (!FishBetterConfig.Enabled.Value || !IsLocal(ff))
                return;
            __instance.m_escapeTime *= Mathf.Lerp(1f, FishBetterConfig.EscapeDurationAtMax.Value, OwnerSkill(ff));
            __instance.m_nview.GetZDO().Set(ZDOVars.s_escape, __instance.m_escapeTime);
        }

        // Longer calm between escapes: vanilla sets m_nextEscape inline in CustomFixedUpdate
        // when an escape ends, so stretch it whenever it changes.
        [HarmonyPrefix]
        [HarmonyPatch(typeof(Fish), nameof(Fish.CustomFixedUpdate))]
        private static void RememberNextEscape(Fish __instance, out float __state)
        {
            __state = __instance.m_nextEscape;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Fish), nameof(Fish.CustomFixedUpdate))]
        private static void LongerEscapeWait(Fish __instance, float __state)
        {
            if (__instance.m_nextEscape == __state || !FishBetterConfig.Enabled.Value)
                return;
            FishingFloat ff = __instance.m_fishingFloat;
            if (!IsLocal(ff))
                return;
            float wait = __instance.m_nextEscape - Fish.s_time;
            __instance.m_nextEscape = Fish.s_time + wait * Mathf.Lerp(1f, FishBetterConfig.EscapeWaitAtMax.Value, OwnerSkill(ff));
        }

        // --- Reeling -------------------------------------------------------------------------

        // Setup runs on the caster's client on a fresh float, so the per-instance tunables can
        // be changed without touching the prefab.
        [HarmonyPostfix]
        [HarmonyPatch(typeof(FishingFloat), nameof(FishingFloat.Setup))]
        private static void TuneFloat(FishingFloat __instance, Character owner)
        {
            if (!FishBetterConfig.Enabled.Value || owner != Player.m_localPlayer)
                return;
            float skill = owner.GetSkillFactor(Skills.SkillType.Fishing);
            __instance.m_pullLineSpeedMaxSkill = FishBetterConfig.ReelSpeedAtMax.Value;
            __instance.m_pullStaminaUseMaxSkillMultiplier = FishBetterConfig.ReelStaminaAtMax.Value;
            __instance.m_hookedStaminaPerSecMaxSkill = FishBetterConfig.HookedStaminaAtMax.Value;
            __instance.m_breakDistance *= Mathf.Lerp(1f, FishBetterConfig.LineBreakDistanceAtMax.Value, skill);
        }

        // A hooked fish reeled within grabbing distance counts as caught, the same way vanilla
        // lands it when the line is fully wound in.
        [HarmonyPrefix]
        [HarmonyPatch(typeof(FishingFloat), nameof(FishingFloat.FixedUpdate))]
        private static bool AutoCatch(FishingFloat __instance)
        {
            if (!FishBetterConfig.Enabled.Value || FishBetterConfig.AutoCatchDistance.Value <= 0f
                || !__instance.m_nview.IsOwner())
                return true;
            Player player = Player.m_localPlayer;
            Fish fish = __instance.GetCatch();
            if (!fish || !player || __instance.GetOwner() != player || player.IsDead())
                return true;
            if (Vector3.Distance(fish.transform.position, player.transform.position) > FishBetterConfig.AutoCatchDistance.Value)
                return true;

            __instance.Message(FishingFloat.Catch(fish, player), prioritized: true);
            __instance.SetCatch(null);
            fish.OnHooked(null);
            __instance.m_nview.Destroy();
            return false;
        }

        // --- Catch rewards -------------------------------------------------------------------

        // Prefix: the fish object may be gone after Catch picks it up.
        [HarmonyPrefix]
        [HarmonyPatch(typeof(FishingFloat), nameof(FishingFloat.Catch))]
        private static void BeforeCatch(Fish fish, Character owner, out CatchInfo __state)
        {
            __state = null;
            if (!FishBetterConfig.Enabled.Value || !fish || owner != Player.m_localPlayer)
                return;
            float skill = owner.GetSkillFactor(Skills.SkillType.Fishing);
            ItemDrop drop = fish.GetComponent<ItemDrop>();
            FishingFloat ff = FishingFloat.FindFloat(fish);
            string bait = ff ? ff.GetBait() : null;
            float starChance = FishBetterConfig.StarUpChanceAtMax.Value * skill
                + FishBetterConfig.StarChancePerTier.Value * BaitTiers.TiersAbove(fish, bait);
            if (drop && Random.value < starChance
                && drop.m_itemData.m_quality < drop.m_itemData.m_shared.m_maxQuality)
            {
                drop.SetQuality(drop.m_itemData.m_quality + 1);
                drop.Save();
                owner.Message(MessageHud.MessageType.TopLeft, "FishBetter: the fish grew a star!");
            }
            __state = new CatchInfo
            {
                Skill = skill,
                Prefab = Utils.GetPrefabName(fish.gameObject),
                Quality = drop ? drop.m_itemData.m_quality : 1,
                Variant = drop ? drop.m_itemData.m_variant : 0,
                Bait = bait,
            };
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(FishingFloat), nameof(FishingFloat.Catch))]
        private static void AfterCatch(Character owner, CatchInfo __state)
        {
            if (__state == null || !(owner is Player player))
                return;
            Inventory inventory = player.GetInventory();

            if (Random.value < FishBetterConfig.ExtraFishChanceAtMax.Value * __state.Skill)
            {
                ItemDrop.ItemData extra = inventory.AddItem(__state.Prefab, 1, __state.Quality, __state.Variant,
                    0L, "", cheated: false, pickedUp: true);
                if (extra != null)
                    player.Message(MessageHud.MessageType.TopLeft, $"FishBetter: bonus {extra.m_shared.m_name}");
            }

            if (__state.Bait != null && Random.value < FishBetterConfig.KeepBaitChanceAtMax.Value * __state.Skill)
            {
                GameObject bait = ZNetScene.instance.GetPrefab(__state.Bait);
                if (bait && inventory.AddItem(bait, 1))
                    player.Message(MessageHud.MessageType.TopLeft, "FishBetter: bait saved");
            }

            if (FishBetterConfig.CatchXp.Value > 0f)
                player.RaiseSkill(Skills.SkillType.Fishing, FishBetterConfig.CatchXp.Value * __state.Quality);
        }

        private sealed class CatchInfo
        {
            public float Skill;
            public string Prefab;
            public int Quality;
            public int Variant;
            public string Bait;
        }
    }
}
