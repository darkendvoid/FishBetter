# Shared paths for FishBetter dev scripts. Source, don't execute.
PROJECT_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
VALHEIM_DIR="$HOME/.local/share/Steam/steamapps/common/Valheim"
VALHEIM_APPID=892970
AMETHYST_APPIMAGE="$HOME/Applications/AmethystModManager-x86_64.AppImage"
AMETHYST_GAME_DIR="$HOME/Games/Amethyst/Valheim"
AMETHYST_PROFILE="default"
MOD_NAME="FishBetter"
MOD_DIR="$AMETHYST_GAME_DIR/mods/$MOD_NAME"
MODLIST="$AMETHYST_GAME_DIR/profiles/$AMETHYST_PROFILE/modlist.txt"
BEPINEX_LOG="$VALHEIM_DIR/BepInEx/LogOutput.log"
CSPROJ="$PROJECT_ROOT/src/$MOD_NAME/$MOD_NAME.csproj"
PLUGIN_CS="$PROJECT_ROOT/src/$MOD_NAME/${MOD_NAME}Plugin.cs"
# Extended regexes smoketest.sh waits for in the BepInEx log. Add mod-specific markers here.
SMOKE_MARKERS=(
  "$MOD_NAME [0-9.]+ loaded"
  "Fishing patches applied"
  "Fish tiers \\([1-9][0-9]* fish\\)"
)
