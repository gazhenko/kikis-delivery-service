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

Do not start a second tunnel if port 5905 is already forwarded. VNC is bound to VM localhost, reached through authenticated SSH, and uses a dedicated VNC password for compatibility with macOS Screen Sharing. The server's password file is `~/.vnc/kiki-passwd`; the client secret is in the protected local file `~/.local/share/kiki-tools/kiki-vnc-password`, outside the project and version control. No public desktop endpoint was created. Sign into Unity Hub inside the VM and activate a suitable license. The terms and account sign-in are left for the account holder.

The Mac initially stopped at its VNC password prompt when the server used no password. Dedicated authentication resolved this; a full framebuffer connection was confirmed on 2026-09-27. The Screen Sharing window is titled `ubuntu:99`.

## Reproducible setup

`Tools/setup-unity-vm.sh` installs the editor and Mac/Windows Mono support using Unity's release metadata and verifies download integrity. `Tools/install-vm-browser.sh` installs Mozilla's official Firefox package after checking its repository key fingerprint. `Tools/start-vm-desktop.sh` starts the private desktop and Unity Hub.

Official references:
- https://unity.com/releases/editor/whats-new/6000.3.20f1
- https://services.api.unity.com/unity/editor/release/v1/releases?version=6000.3.20f1&architecture=X86_64&platform=LINUX
- https://support.mozilla.org/en-US/kb/install-firefox-linux

The account holder accepted Hub terms and signed in on 2026-09-27. The editor subsequently resolved the Unity Personal entitlement, imported the project and exported a universal Mac application. The first import needed a restart after enabling the new Input System; that setting is now retained in version-controlled ProjectSettings.

Use this machine for imports, compilation, tests and build storage. Software-rendered views are not performance evidence for a GPU-equipped desktop. The exported Intel/ARM64 Mac app passed local code-signature verification and launched using Metal on the Mac's Apple M1 Pro. See `VERIFICATION.md` for traversal results and remaining checks.
