#!/usr/bin/env bash
set -euo pipefail
# Official Mozilla APT instructions: https://support.mozilla.org/en-US/kb/install-firefox-linux
sudo install -d -m 0755 /etc/apt/keyrings
key_file=$(mktemp)
trap 'rm -f "$key_file"' EXIT
curl -fsSL https://packages.mozilla.org/apt/repo-signing-key.gpg > "$key_file"
fingerprint=$(gpg --batch --show-keys --with-colons "$key_file" | awk -F: '$1 == "fpr" { print $10; exit }')
[[ "$fingerprint" == 35BAA0B33E9EB396F59CA838C0BA5CE6DC6315A3 ]]
sudo install -m 0644 "$key_file" /etc/apt/keyrings/packages.mozilla.org.asc
printf '%s\n' 'deb [signed-by=/etc/apt/keyrings/packages.mozilla.org.asc] https://packages.mozilla.org/apt mozilla main' | sudo tee /etc/apt/sources.list.d/mozilla.list > /dev/null
printf '%s\n' 'Package: *' 'Pin: origin packages.mozilla.org' 'Pin-Priority: 1000' '' 'Package: firefox' 'Pin: release o=Ubuntu' 'Pin-Priority: -1' | sudo tee /etc/apt/preferences.d/mozilla > /dev/null
sudo apt-get -o DPkg::Lock::Timeout=180 update
sudo env DEBIAN_FRONTEND=noninteractive apt-get -o DPkg::Lock::Timeout=180 install -y firefox scrot
xdg-settings set default-web-browser firefox.desktop
