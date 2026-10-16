using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Jotunn.Utils;

namespace FishBetter
{
    [BepInPlugin(ModGuid, ModName, ModVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    // Client-side only: servers and other players don't need it.
    [NetworkCompatibility(CompatibilityLevel.NotEnforced, VersionStrictness.None)]
    public class FishBetterPlugin : BaseUnityPlugin
    {
        public const string ModGuid = "com.darkendvoid.fishbetter";
        public const string ModName = "FishBetter";
        public const string ModVersion = "0.2.1";

        internal static ManualLogSource Log;

        private void Awake()
        {
            Log = Logger;
            FishBetterConfig.Bind(Config);
            BaitTiers.Init();
            Harmony harmony = new Harmony(ModGuid);
            harmony.PatchAll(typeof(FishingPatches));
            Log.LogInfo("Fishing patches applied");
            // smoketest.sh waits for this line; keep the format.
            Log.LogInfo($"{ModName} {ModVersion} loaded");
        }
    }
}
