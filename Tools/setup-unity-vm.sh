#!/usr/bin/env bash
set -euo pipefail
# Run as the development user inside the dedicated Ubuntu 24.04 VM.
unity_version=6000.3.20f1
unity_revision=c9ba695d4f07
unity_root="$HOME/Unity/$unity_version"
unity_cache="$HOME/.cache/kiki-unity-install"
mkdir -p "$unity_root" "$unity_cache"
# Recent Hub packages register their own deb822 source. Keep that source on reruns.
if [[ -f /etc/apt/sources.list.d/unityhub.sources && -f /etc/apt/sources.list.d/unityhub.list ]]; then
  sudo mv /etc/apt/sources.list.d/unityhub.list /etc/apt/sources.list.d/unityhub.list.kiki-disabled
fi
sudo cloud-init status --wait
sudo apt-get update
sudo env DEBIAN_FRONTEND=noninteractive apt-get install -y --no-install-recommends \
  ubuntu-desktop-minimal xfce4 xfce4-terminal xauth xvfb x11vnc mesa-utils libarchive-tools curl ca-certificates \
  gnupg libgtk-3-0 libnss3 libasound2t64 libgbm1 libsecret-1-0 libxss1 \
  libglu1-mesa dbus-x11 unzip rsync python3-pip
if [[ ! -f /etc/apt/sources.list.d/unityhub.sources ]]; then
  curl -fsSL https://hub.unity3d.com/linux/keys/public | gpg --dearmor > "$unity_cache/unityhub.gpg"
  sudo install -m 0644 "$unity_cache/unityhub.gpg" /usr/share/keyrings/unityhub.gpg
  printf '%s\n' 'deb [arch=amd64 signed-by=/usr/share/keyrings/unityhub.gpg] https://hub.unity3d.com/linux/repos/deb stable main' | sudo tee /etc/apt/sources.list.d/unityhub.list > /dev/null
fi
sudo apt-get update
sudo env DEBIAN_FRONTEND=noninteractive apt-get install -y unityhub
python3 -u - "$unity_root" "$unity_cache" <<'PY'
import pathlib,urllib.request,json,hashlib,base64,subprocess,sys,shutil
target=pathlib.Path(sys.argv[1]);cache=pathlib.Path(sys.argv[2])
data=json.load(urllib.request.urlopen('https://services.api.unity.com/unity/editor/release/v1/releases?version=6000.3.20f1&architecture=X86_64&platform=LINUX'))
download=data['results'][0]['downloads'][0]
def fetch(item,name):
    file=cache/name
    if not file.exists():
        print('Downloading',name,flush=True);urllib.request.urlretrieve(item['url'],file)
    algorithm,expected=item['integrity'].split('-',1)
    h=hashlib.new(algorithm)
    with file.open('rb') as f:
        for chunk in iter(lambda:f.read(1024*1024),b''):h.update(chunk)
    if base64.b64encode(h.digest()).decode()!=expected:raise RuntimeError('Integrity mismatch: '+name)
    return file
if not (target/'Editor/Unity').exists():
    archive=fetch(download,'Unity.tar.xz')
    print('Extracting Unity editor',flush=True)
    subprocess.run(['tar','-xf',str(archive),'-C',str(target)],check=True)
    archive.unlink()
for module in download['modules']:
    if module['id'] not in ['mac-mono','windows-mono']:continue
    destination=target/'Editor/Data/PlaybackEngines'/('MacStandaloneSupport' if module['id']=='mac-mono' else 'WindowsStandaloneSupport')
    if destination.exists():continue
    file=fetch(module,module['id']+'.pkg')
    extracted=cache/(module['id']+'-extracted');extracted.mkdir(exist_ok=True)
    subprocess.run(['bsdtar','-xf',str(file),'-C',str(extracted)],check=True)
    payloads=list(extracted.rglob('Payload'))
    if not payloads:raise RuntimeError('No payload in '+str(file))
    for payload in payloads:
        folder=payload.parent/'unpacked';folder.mkdir(exist_ok=True)
        subprocess.run(['bsdtar','-xf',str(payload),'-C',str(folder)],check=True)
        matches=list(folder.rglob(destination.name))
        if matches:
            destination.parent.mkdir(parents=True,exist_ok=True)
            shutil.copytree(matches[0],destination,dirs_exist_ok=True)
        elif (folder/'modules.asset').exists() and (folder/'Variations').is_dir():
            # Component PKGs contain files relative to their PackageInfo install-location.
            destination.parent.mkdir(parents=True,exist_ok=True)
            shutil.copytree(folder,destination,dirs_exist_ok=True)
    if not destination.exists():raise RuntimeError('Could not locate '+str(destination))
    shutil.rmtree(extracted);file.unlink()
print('UNITY_EDITOR_READY',target/'Editor/Unity',flush=True)
PY
printf '%s\n' 'Unity installed. User sign-in/license activation is still required.'
