#!/usr/bin/env bash
set -euo pipefail
# Private desktop, accessible only through an authenticated SSH tunnel.
export DISPLAY=:99
export XAUTHORITY="$HOME/.Xauthority-kiki"
export LIBGL_ALWAYS_SOFTWARE=1
export GALLIUM_DRIVER=llvmpipe
export XDG_SESSION_TYPE=x11
export XDG_CURRENT_DESKTOP=XFCE
export XDG_RUNTIME_DIR="/run/user/$(id -u)"
desktop_logs="$HOME/.local/state/kiki-desktop"
mkdir -p "$desktop_logs"
if ! pgrep -u "$(id -u)" -f '^Xvfb :99' > /dev/null; then
  touch "$XAUTHORITY"
  chmod 600 "$XAUTHORITY"
  xauth -f "$XAUTHORITY" add :99 . "$(openssl rand -hex 16)"
  nohup Xvfb :99 -screen 0 1440x900x24 -auth "$XAUTHORITY" -nolisten tcp > "$desktop_logs/display.log" 2>&1 < /dev/null &
  sleep 2
fi
if ! pgrep -u "$(id -u)" -x xfce4-session > /dev/null; then
  nohup dbus-run-session -- xfce4-session > "$desktop_logs/session.log" 2>&1 < /dev/null &
fi
if ! pgrep -u "$(id -u)" -x x11vnc > /dev/null; then
  nohup x11vnc -display :99 -auth "$XAUTHORITY" -localhost -nopw -forever -shared -rfbport 5900 -noxdamage > "$desktop_logs/vnc.log" 2>&1 < /dev/null &
fi
if ! pgrep -u "$(id -u)" -f '^/usr/lib/unityhub/unityhub-bin$' > /dev/null; then
  nohup unityhub > "$desktop_logs/unityhub.log" 2>&1 < /dev/null &
fi
printf '%s\n' 'Private desktop ready on VM loopback port 5900. Connect through SSH forwarding.'
