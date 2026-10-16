#!/usr/bin/env bash
# Publish the current version as a release on the lab Forgejo server: tag v<version>, notes from
# that version's CHANGELOG.md section, Release zip attached. Then copy it to the GitHub mirror with
# `lab-git release-sync` (push mirrors only carry the tag). Nexus is separate: see nexus-upload.sh.
# Usage: lab-release.sh [--dry-run] [--no-build]
#   --dry-run   check everything and build the zip, but tag and publish nothing
#   --no-build  zip the existing Release build instead of rebuilding
set -euo pipefail
source "$(dirname "$0")/env.sh"

DRY_RUN=0 BUILD=1
for arg in "$@"; do
  case "$arg" in
    --dry-run) DRY_RUN=1 ;;
    --no-build) BUILD=0 ;;
    *) echo "Unknown option: $arg" >&2; exit 2 ;;
  esac
done

die() { echo "ERROR: $*" >&2; exit 1; }

LAB_API="https://git.lab.coylebox.net/api/v1"
TOKEN_FILE="$HOME/.config/lab-git/token"
[[ -r "$TOKEN_FILE" ]] || die "No lab-git token at $TOKEN_FILE"
TOKEN=$(<"$TOKEN_FILE")

# owner/name from the lab origin, e.g. ssh://git@git-ssh.lab.coylebox.net:2222/acoyle/QuickFill.git
origin=$(git -C "$PROJECT_ROOT" remote get-url origin 2>/dev/null) || die "No origin remote (run lab-git init)"
[[ "$origin" == *lab.coylebox.net* ]] || die "origin is not the lab server: $origin"
repo=$(sed -E 's#.*[:/]([^/]+/[^/]+)$#\1#; s#\.git$##' <<<"$origin")

# api METHOD PATH [curl args...] -> response body on stdout; fails on non-2xx.
api() {
  local method=$1 path=$2 out status; shift 2
  out=$(mktemp)
  status=$(curl -sS -o "$out" -w '%{http_code}' -X "$method" "$LAB_API$path" \
    -H "Authorization: token $TOKEN" -H 'Accept: application/json' "$@")
  if [[ "$status" != 2* ]]; then
    echo "ERROR: $method $path -> HTTP $status" >&2
    cat "$out" >&2; echo >&2
    rm -f "$out"; return 1
  fi
  cat "$out"; rm -f "$out"
}

# --- Version, changelog and git checks ---
version=$(sed -n 's:.*<Version>\(.*\)</Version>.*:\1:p' "$CSPROJ")
[[ -n "$version" ]] || die "No <Version> in $CSPROJ"
plugin_version=$(sed -n 's/.*ModVersion = "\(.*\)".*/\1/p' "$PLUGIN_CS")
[[ "$plugin_version" == "$version" ]] || die "csproj version $version != ModVersion $plugin_version"
tag="v$version"

changelog=$(awk -v v="$version" '
  $0 == "## " v { found = 1; next }
  found && /^## / { exit }
  found' "$PROJECT_ROOT/CHANGELOG.md" | sed '/^[[:space:]]*$/d')
[[ -n "$changelog" ]] || die "CHANGELOG.md has no entries under '## $version' (run bump-version.sh)"

[[ -z "$(git -C "$PROJECT_ROOT" status --porcelain)" ]] || die "Uncommitted changes; commit the release first"
git -C "$PROJECT_ROOT" fetch -q origin
[[ -z "$(git -C "$PROJECT_ROOT" rev-list '@{u}..HEAD')" ]] || die "HEAD is not pushed to origin; push first"

status=$(curl -sS -o /dev/null -w '%{http_code}' -H "Authorization: token $TOKEN" \
  "$LAB_API/repos/$repo/releases/tags/$tag")
[[ "$status" == 404 ]] || die "Release $tag already exists on $repo (HTTP $status). Bump the version first."
if git -C "$PROJECT_ROOT" rev-parse -q --verify "refs/tags/$tag" >/dev/null; then
  [[ "$(git -C "$PROJECT_ROOT" rev-list -n1 "$tag")" == "$(git -C "$PROJECT_ROOT" rev-parse HEAD)" ]] \
    || die "Tag $tag exists but isn't on HEAD"
fi

# --- Build and package (same zip as nexus-upload.sh) ---
if (( BUILD )); then
  dotnet build "$CSPROJ" -c Release -nologo -v minimal
fi
dll="$(dirname "$CSPROJ")/bin/Release/$MOD_NAME.dll"
[[ -f "$dll" ]] || die "$dll not found (build with Release first)"
zip_name="$MOD_NAME-$version.zip"
zip_path="$PROJECT_ROOT/$zip_name"
rm -f "$zip_path"
zip -j -q "$zip_path" "$dll"

notes="## Changes in $version

$changelog

## Install

Unzip and put \`$MOD_NAME.dll\` in \`BepInEx/plugins\`. Requires BepInEx and Jötunn."

echo "Repo:      $repo"
echo "Release:   $tag ($MOD_NAME $version) on $(git -C "$PROJECT_ROOT" rev-parse --short HEAD)"
echo "Upload:    $zip_name ($(stat -c %s "$zip_path") bytes)"
echo "Notes:"
sed 's/^/  /' <<<"$notes"
if (( DRY_RUN )); then
  echo "Dry run: nothing tagged or published. Zip left at $zip_path"
  exit 0
fi

# --- Tag, release, attach, mirror ---
if ! git -C "$PROJECT_ROOT" rev-parse -q --verify "refs/tags/$tag" >/dev/null; then
  git -C "$PROJECT_ROOT" tag -a "$tag" -m "$MOD_NAME $version"
fi
git -C "$PROJECT_ROOT" push -q origin "$tag"

body=$(jq -n --arg t "$tag" --arg n "$MOD_NAME $version" --arg b "$notes" \
  '{tag_name: $t, name: $n, body: $b, draft: false, prerelease: false}')
release_id=$(api POST "/repos/$repo/releases" -H 'Content-Type: application/json' --data "$body" | jq -r .id)
api POST "/repos/$repo/releases/$release_id/assets?name=$zip_name" -F "attachment=@$zip_path" >/dev/null
rm -f "$zip_path"
echo "Published https://git.lab.coylebox.net/$repo/releases/tag/$tag"

(cd "$PROJECT_ROOT" && lab-git release-sync "$tag") \
  || echo "WARNING: lab-git release-sync failed; rerun it in $PROJECT_ROOT" >&2
