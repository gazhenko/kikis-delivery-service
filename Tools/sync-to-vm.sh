#!/usr/bin/env bash
set -euo pipefail
project_dir=$(cd -- "$(dirname -- "$0")/.." && pwd)
ssh -o BatchMode=yes -o StrictHostKeyChecking=yes kiki-unity 'mkdir -p /home/jim/Projects/kikis-delivery-desktop'
rsync -az --exclude Library --exclude Temp --exclude Logs --exclude Builds \
  --exclude obj --exclude bin --exclude .git --exclude __pycache__ --exclude '*.blend1' \
  "$project_dir/" kiki-unity:/home/jim/Projects/kikis-delivery-desktop/
