#!/usr/bin/env bash
set -euo pipefail
# Requires the licensed VM and Linux standalone support. Close its project editor first.
project_dir=$(cd -- "$(dirname -- "$0")/.." && pwd)
bash "$project_dir/Tools/sync-to-vm.sh"
ssh -o BatchMode=yes -o StrictHostKeyChecking=yes kiki-unity \
  'mkdir -p /home/jim/Projects/kikis-delivery-desktop/Logs && /home/jim/Unity/6000.3.20f1/Editor/Unity -batchmode -nographics -buildTarget StandaloneLinux64 -projectPath /home/jim/Projects/kikis-delivery-desktop -executeMethod Koriko.Editor.DesktopBuild.Linux -quit -logFile /home/jim/Projects/kikis-delivery-desktop/Logs/linux-build.log'
# A successful export can still contain a shader error fallback.
ssh -o BatchMode=yes -o StrictHostKeyChecking=yes kiki-unity \
  'if grep -q "Shader error in" /home/jim/Projects/kikis-delivery-desktop/Logs/linux-build.log; then grep "Shader error in" /home/jim/Projects/kikis-delivery-desktop/Logs/linux-build.log; exit 1; fi'
mkdir -p "$project_dir/Builds/linux"
rsync -az kiki-unity:/home/jim/Projects/kikis-delivery-desktop/Builds/linux/ "$project_dir/Builds/linux/"
# Platform-generated settings stay on the VM; preserve the local Mac build and project.
cp "$project_dir/Assets/Koriko/Art/GameIcon.png" "$project_dir/Builds/linux/GameIcon.png"
