#!/usr/bin/env bash
set -euo pipefail
# Requires Unity license activation in the guest. Do not run while its editor has this project open.
project_dir=$(cd -- "$(dirname -- "$0")/.." && pwd)
bash "$project_dir/Tools/sync-to-vm.sh"
ssh -o BatchMode=yes -o StrictHostKeyChecking=yes kiki-unity \
  'mkdir -p /home/jim/Projects/kikis-delivery-desktop/Logs && /home/jim/Unity/6000.3.20f1/Editor/Unity -batchmode -nographics -projectPath /home/jim/Projects/kikis-delivery-desktop -executeMethod Koriko.Editor.DesktopBuild.Mac -quit -logFile /home/jim/Projects/kikis-delivery-desktop/Logs/mac-build.log'
mkdir -p "$project_dir/Builds/mac"
rsync -az kiki-unity:/home/jim/Projects/kikis-delivery-desktop/Builds/mac/ "$project_dir/Builds/mac/"
