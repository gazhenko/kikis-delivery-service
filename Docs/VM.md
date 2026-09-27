# Unity development VM

Created 2026-09-27 on the existing Proxmox host through its configured SSH connection.

| Setting | Value |
| --- | --- |
| Proxmox VM | 130, `kiki-unity` |
| Guest | Ubuntu 24.04 x86-64 |
| CPU | 8 vCPU, host model, CPU cap 6 |
| Memory | 16 GiB maximum, 8 GiB balloon minimum |
| Disk | 100 GiB thin provisioned on `local-lvm` |
| Network | DHCP on existing `vmbr0`; initially `192.168.1.186` |
| Login | `jim`, existing SSH public key; password login locked |
| Editor | `~/Unity/6000.3.20f1/Editor/Unity` |
| Project | `~/Projects/kikis-delivery-desktop` |
| Remote desktop | Xvfb/XFCE, 1440 × 900, VNC on VM loopback only |
| Rendering | Mesa llvmpipe software OpenGL 4.5, no GPU acceleration |

The local SSH alias `kiki-unity` uses a pinned guest host key obtained through the trusted Proxmox guest agent. Existing VMs and host GPU settings were left unchanged. The new VM is not configured to start automatically with the host.

## Connect

```sh
ssh kiki-unity
```

Start its desktop if necessary:

```sh
ssh kiki-unity 'bash ~/start-vm-desktop.sh'
ssh -fN -o ExitOnForwardFailure=yes \
  -L 127.0.0.1:5905:127.0.0.1:5900 kiki-unity
open vnc://127.0.0.1:5905
```

Do not start a second tunnel if port 5905 is already forwarded. VNC is bound to VM localhost and relies on SSH authentication. No public desktop endpoint was created. Sign into Unity Hub inside the VM and activate a suitable license. The terms and account sign-in are left for the account holder.

## Reproducible setup

`Tools/setup-unity-vm.sh` installs the editor and Mac/Windows Mono support using Unity's release metadata and verifies download integrity. `Tools/install-vm-browser.sh` installs Mozilla's official Firefox package after checking its repository key fingerprint. `Tools/start-vm-desktop.sh` starts the private desktop and Unity Hub.

Official references:
- https://unity.com/releases/editor/whats-new/6000.3.20f1
- https://services.api.unity.com/unity/editor/release/v1/releases?version=6000.3.20f1&architecture=X86_64&platform=LINUX
- https://support.mozilla.org/en-US/kb/install-firefox-linux

The first editor invocation reached the licensing service and exited with `No valid Unity Editor license found. Please activate your license.` No project import or build succeeded before activation.

Use this machine for imports, compilation, tests and build storage. Software-rendered views can help inspect the scene, but are not performance evidence for a GPU-equipped desktop. The intended performance test is an exported Mac app on the user's Mac; that export path is not yet verified.
