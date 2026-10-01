#!/usr/bin/env bash
# Smoke test template: pack semua paket SyncNetPro.* ke feed lokal, pasang SyncNetPro.Templates, buat proyek dari
# setiap template, lalu build + test proyek hasil template terhadap paket lokal tersebut.
# Pemakaian: scripts/test-templates.sh [folder-kerja]
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
WORK="${1:-$(mktemp -d)}"
VERSION="1.0.0-templatetest"
FEED="$WORK/feed"
OUT="$WORK/generated"
export DOTNET_CLI_HOME="$WORK/cli-home"   # cache template terpisah dari user
export NUGET_PACKAGES="$WORK/nuget-packages"  # paket lokal dengan versi sama tidak tercampur cache global

rm -rf "$FEED" "$OUT" "$DOTNET_CLI_HOME" "$NUGET_PACKAGES"
mkdir -p "$FEED" "$OUT"

echo "== pack $VERSION -> $FEED"
dotnet pack "$ROOT/SyncNetSdkPro.slnx" -c Release -o "$FEED" -p:MinVerVersionOverride=$VERSION -p:SdkReleaseVersion=$VERSION > "$WORK/pack.log"

echo "== install SyncNetPro.Templates"
dotnet new install "$FEED/SyncNetPro.Templates.$VERSION.nupkg" > /dev/null

cat > "$WORK/nuget.local.config" <<CONFIG
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="local" value="$FEED" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
  <packageSourceMapping>
    <packageSource key="local"><package pattern="SyncNetPro.*" /></packageSource>
    <packageSource key="nuget.org"><package pattern="*" /></packageSource>
  </packageSourceMapping>
</configuration>
CONFIG

# Entri "template:opsi" membuat varian dengan opsi tambahan (mis. syncnet-inbound-http:--with-routing).
TEMPLATES="${TEMPLATES:-syncnet-outbound-iso syncnet-outbound-http syncnet-inbound-http syncnet-inbound-http:--with-routing syncnet-inbound-iso syncnet-blank}"
for entry in $TEMPLATES; do
  t="${entry%%:*}"
  opts=""; suffix=""
  if [[ "$entry" == *:* ]]; then opts="${entry#*:}"; suffix="$(echo "$opts" | sed -E 's/^--(with-)?//; s/(^|-)([a-z])/\U\2/g')"; fi
  name="Api.Smoke$(echo "$t" | sed -E 's/syncnet-//; s/(^|-)([a-z])/\U\2/g')$suffix"
  echo "== $t $opts -> $name"
  # shellcheck disable=SC2086
  dotnet new "$t" -n "$name" -o "$OUT/$name" --app-name "API Smoke" --node-name SMOKE_NODE $opts > /dev/null
  cp "$WORK/nuget.local.config" "$OUT/$name/nuget.config"
  (cd "$OUT/$name" && dotnet build -c Release > "$WORK/$name.build.log" 2>&1) || { tail -40 "$WORK/$name.build.log"; exit 1; }
  (cd "$OUT/$name" && dotnet test -c Release --no-build > "$WORK/$name.test.log" 2>&1) || { tail -60 "$WORK/$name.test.log"; exit 1; }
  grep -E '^\s+(total|failed|succeeded):' "$WORK/$name.test.log" | tr -s ' ' | tr '\n' ' '; echo
done

echo "Semua template OK ($WORK)"
