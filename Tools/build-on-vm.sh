#!/usr/bin/env bash
set -euo pipefail
# Requires Unity license activation in the guest. Do not run while its editor has this project open.
project_dir=$(cd -- "$(dirname -- "$0")/.." && pwd)
bash "$project_dir/Tools/sync-to-vm.sh"
ssh -o BatchMode=yes -o StrictHostKeyChecking=yes kiki-unity \
  'mkdir -p /home/jim/Projects/kikis-delivery-desktop/Logs && /home/jim/Unity/6000.3.20f1/Editor/Unity -batchmode -nographics -buildTarget StandaloneOSX -projectPath /home/jim/Projects/kikis-delivery-desktop -executeMethod Koriko.Editor.DesktopBuild.Mac -quit -logFile /home/jim/Projects/kikis-delivery-desktop/Logs/mac-build.log'
# Unity can report a successful player export while a platform shader compiled to
# its pink error fallback. Reject that export before replacing the local player.
ssh -o BatchMode=yes -o StrictHostKeyChecking=yes kiki-unity \
  'if grep -q "Shader error in" /home/jim/Projects/kikis-delivery-desktop/Logs/mac-build.log; then grep "Shader error in" /home/jim/Projects/kikis-delivery-desktop/Logs/mac-build.log; exit 1; fi'
mkdir -p "$project_dir/Builds/mac"
rsync -az kiki-unity:/home/jim/Projects/kikis-delivery-desktop/Builds/mac/ "$project_dir/Builds/mac/"
# Preserve generated GUIDs and scene references without overwriting authored source.
rsync -az --exclude='/Resources/***' --exclude='/Resources.meta' \
  --include='*/' --include='*.meta' --include='*.asset' \
  --include='/Koriko/Generated/***' --include='/Koriko/Scenes/***' --exclude='*' \
  kiki-unity:/home/jim/Projects/kikis-delivery-desktop/Assets/ "$project_dir/Assets/"
rsync -az kiki-unity:/home/jim/Projects/kikis-delivery-desktop/ProjectSettings/ "$project_dir/ProjectSettings/"
rsync -az kiki-unity:/home/jim/Projects/kikis-delivery-desktop/Packages/packages-lock.json "$project_dir/Packages/packages-lock.json"
