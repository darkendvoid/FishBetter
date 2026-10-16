#!/usr/bin/env bash
# Set the mod version everywhere it lives and turn CHANGELOG.md's "## Unreleased" into "## <version>".
# Usage: bump-version.sh <major.minor.patch>
set -euo pipefail
source "$(dirname "$0")/env.sh"

new="${1:-}"
[[ "$new" =~ ^[0-9]+\.[0-9]+\.[0-9]+$ ]] || { echo "usage: $0 <major.minor.patch>" >&2; exit 2; }
old=$(sed -n 's:.*<Version>\(.*\)</Version>.*:\1:p' "$CSPROJ")

sed -i "s:<Version>.*</Version>:<Version>$new</Version>:" "$CSPROJ"
sed -i "s/ModVersion = \".*\"/ModVersion = \"$new\"/" "$PLUGIN_CS"

changelog="$PROJECT_ROOT/CHANGELOG.md"
if grep -qx '## Unreleased' "$changelog"; then
  sed -i "s/^## Unreleased\$/## $new/" "$changelog"
  echo "CHANGELOG.md: '## Unreleased' -> '## $new'"
elif ! grep -qx "## $new" "$changelog"; then
  echo "WARNING: CHANGELOG.md has no '## Unreleased' or '## $new' section; nexus-upload.sh will refuse to upload." >&2
fi
echo "$MOD_NAME: $old -> $new"
