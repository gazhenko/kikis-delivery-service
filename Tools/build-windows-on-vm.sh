#!/usr/bin/env bash
set -euo pipefail
# Requires the licensed VM and Windows Mono standalone support. Close its project editor first.
# The export is not run on Windows here; it is packaged for the release and noted as untested.
project_dir=$(cd -- "$(dirname -- "$0")/.." && pwd)
bash "$project_dir/Tools/sync-to-vm.sh"
dev=${KORIKO_DEV_BUILD:-0}
ssh -o BatchMode=yes -o StrictHostKeyChecking=yes kiki-unity \
  "mkdir -p /home/jim/Projects/kikis-delivery-desktop/Logs && KORIKO_DEV_BUILD=$dev /home/jim/Unity/6000.3.20f1/Editor/Unity -batchmode -nographics -buildTarget StandaloneWindows64 -projectPath /home/jim/Projects/kikis-delivery-desktop -executeMethod Koriko.Editor.DesktopBuild.Windows -quit -logFile /home/jim/Projects/kikis-delivery-desktop/Logs/windows-build.log"
# A successful export can still contain a shader error fallback.
ssh -o BatchMode=yes -o StrictHostKeyChecking=yes kiki-unity \
  'if grep -q "Shader error in" /home/jim/Projects/kikis-delivery-desktop/Logs/windows-build.log; then grep "Shader error in" /home/jim/Projects/kikis-delivery-desktop/Logs/windows-build.log; exit 1; fi'
mkdir -p "$project_dir/Builds/windows"
rsync -az --delete kiki-unity:/home/jim/Projects/kikis-delivery-desktop/Builds/windows/ "$project_dir/Builds/windows/"
