#!/usr/bin/env bash
# Package Builds/{mac,linux,windows} and the trailer, then publish a GitHub release.
# Usage: Tools/release.sh v1.0.0 [--draft]
# Expects verified players in Builds/mac, Builds/linux and Builds/windows, release notes in
# Docs/RELEASE_NOTES.md and the trailer at Docs/media/trailer.mp4.
set -euo pipefail
root="$(cd "$(dirname "$0")/.." && pwd)"
tag=${1:?tag like v1.0.0}; shift || true
draft=""; [[ "${1:-}" == "--draft" ]] && draft="--draft"
out="$root/Builds/release/$tag"; rm -rf "$out"; mkdir -p "$out"
app="$root/Builds/mac/Kiki’s Delivery Service.app"
if [ -d "$app" ]; then
  # The published bundle carries a plain apostrophe so shells and agents can quote it easily.
  stage="$out/stage"; mkdir -p "$stage"
  ditto "$app" "$stage/Kiki's Delivery Service.app"
  codesign --verify --deep --strict "$stage/Kiki's Delivery Service.app"
  (cd "$stage" && ditto -c -k --sequesterRsrc --keepParent "Kiki's Delivery Service.app" "$out/KikisDeliveryService-$tag-macOS-universal.zip")
  rm -rf "$stage"
fi
if [ -d "$root/Builds/windows" ]; then
  (cd "$root/Builds/windows" && zip -qr9 "$out/KikisDeliveryService-$tag-Windows-x64.zip" . -x "*_BurstDebugInformation_DoNotShip/*" -x "*_BackUpThisFolder_ButDontShipItWithYourGame/*")
fi
if [ -d "$root/Builds/linux" ]; then
  (cd "$root/Builds/linux" && tar --exclude="*_BurstDebugInformation_DoNotShip" --exclude="*_BackUpThisFolder_ButDontShipItWithYourGame" --exclude="release.json" --exclude="file-hashes.json" -czf "$out/KikisDeliveryService-$tag-Linux-x64.tar.gz" .)
fi
[ -f "$root/Docs/media/trailer.mp4" ] && cp "$root/Docs/media/trailer.mp4" "$out/Kikis-Delivery-Service-trailer.mp4"
(cd "$out" && shasum -a 256 * > SHA256SUMS.txt)
ls -lh "$out"
gh release create "$tag" $draft --title "Kiki's Delivery Service $tag" --notes-file "$root/Docs/RELEASE_NOTES.md" "$out"/*
