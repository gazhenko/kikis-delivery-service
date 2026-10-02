#!/usr/bin/env bash
# Cut the trailer from the frames written by the player's --koriko-trailer mode.
#   Tools/make-trailer.sh <trailer frame dir> <tour-check audio dir> [out dir]
# The frames are 24 fps drawings of the real game. Sound is the game's own synthesized clips
# (the title waltz, the sea and the wind), mixed here with ffmpeg. The logo is overlaid on the
# final shot. Also writes the README teaser GIF from the street flight.
set -euo pipefail
root="$(cd "$(dirname "$0")/.." && pwd)"
frames=${1:?trailer frame directory}; audio=${2:?tour-check audio directory}; out=${3:-$root/Docs/media}
mkdir -p "$out"
logo="$out/logo.png"
count=$(ls "$frames"/frame_*.png | wc -l | tr -d ' ')
seconds=$(python3 -c "print($count/24)")
# Shot boundaries, for the title overlay on the last shot and the teaser cut.
last_first=$(tail -1 "$frames/shots.csv" | cut -d, -f2)
street_first=$(grep '^06-street' "$frames/shots.csv" | cut -d, -f2)
street_count=$(grep '^06-street' "$frames/shots.csv" | cut -d, -f3)
title_at=$(python3 -c "print($last_first/24+0.6)")
fade_out=$(python3 -c "print(max(0,$seconds-1.5))")
# Music bed: the waltz looped under the whole cut, the sea and wind beds low beneath it.
ffmpeg -loglevel error -y \
  -stream_loop -1 -i "$audio/waltz.wav" -stream_loop -1 -i "$audio/sea.wav" -stream_loop -1 -i "$audio/wind.wav" \
  -filter_complex "[0:a]volume=0.55[m];[1:a]volume=0.22[s];[2:a]volume=0.10[w];[m][s][w]amix=inputs=3:duration=longest:normalize=0,atrim=0:$seconds,afade=t=in:st=0:d=1.2,afade=t=out:st=$fade_out:d=1.5,aresample=48000[a]" \
  -map "[a]" -c:a aac -b:a 160k "$out/trailer-audio.m4a"
# Picture: fade in, the logo on the final shot, fade out.
ffmpeg -loglevel error -y -framerate 24 -i "$frames/frame_%05d.png" -i "$logo" -i "$out/trailer-audio.m4a" \
  -filter_complex "[1:v]scale=1100:-1[lg];[0:v][lg]overlay=(W-w)/2:H*0.62-h/2:enable='gte(t,$title_at)':eval=frame,fade=t=in:st=0:d=1,fade=t=out:st=$fade_out:d=1.5,format=yuv420p[v]" \
  -map "[v]" -map 2:a -c:v libx264 -preset slow -crf 18 -pix_fmt yuv420p -movflags +faststart -shortest "$out/trailer.mp4"
# A short teaser for the README: six seconds of the street flight at 12 drawings a second.
ffmpeg -loglevel error -y -start_number "$street_first" -framerate 24 -i "$frames/frame_%05d.png" -frames:v "$street_count" \
  -vf "fps=12,scale=900:-1:flags=lanczos,split[a][b];[a]palettegen=max_colors=192:stats_mode=diff[p];[b][p]paletteuse=dither=bayer:bayer_scale=4" "$out/teaser.gif"
rm -f "$out/trailer-audio.m4a"
ls -lh "$out/trailer.mp4" "$out/teaser.gif"
