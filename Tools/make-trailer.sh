#!/usr/bin/env bash
# Cut the trailer from the frames written by the player's --koriko-trailer mode.
#   Tools/make-trailer.sh <trailer frame dir> <tour-check audio dir> [out dir]
# The frames are 24 fps drawings of the real game. Sound is the game's own synthesized clips
# (the title waltz, the sea and the wind), mixed here with ffmpeg. The logo is overlaid on the
# final shot. Also writes the README teaser GIF from the street flight.
set -euo pipefail
root="$(cd "$(dirname "$0")/.." && pwd)"
source=${1:?trailer frame directory}; audio=${2:?tour-check audio directory}; out=${3:-$root/Docs/media}
mkdir -p "$out"
logo="$out/logo.png"
# TRAILER_SKIP names shots (comma separated) to leave out of the cut. The remaining frames are
# relinked in order so ffmpeg reads one continuous sequence.
frames="${TMPDIR:-/tmp}/kiki-trailer-cut"; rm -rf "$frames"; mkdir -p "$frames"
python3 - "$source" "$frames" "${TRAILER_SKIP:-}" <<'PY'
import csv,os,sys
source,target,skip=sys.argv[1],sys.argv[2],set(filter(None,sys.argv[3].split(',')))
rows=list(csv.DictReader(open(os.path.join(source,'shots.csv'))))
n=0
with open(os.path.join(target,'shots.csv'),'w') as log:
    log.write('shot,firstFrame,frames\n')
    for row in rows:
        if row['shot'] in skip: continue
        first,count=int(row['firstFrame']),int(row['frames'])
        log.write(f"{row['shot']},{n},{count}\n")
        for i in range(first,first+count):
            os.symlink(os.path.join(source,f'frame_{i:05d}.png'),os.path.join(target,f'frame_{n:05d}.png'));n+=1
print('TRAILER_CUT',n,'frames,',len(rows)-len(skip),'shots')
PY
count=$(ls "$frames"/frame_*.png | wc -l | tr -d ' ')
seconds=$(python3 -c "print($count/24)")
# Shot boundaries, for the title overlay on the last shot and the teaser cut.
last_first=$(tail -1 "$frames/shots.csv" | cut -d, -f2)
street_first=$(grep '^06-street' "$frames/shots.csv" | cut -d, -f2)
street_count=$(grep '^06-street' "$frames/shots.csv" | cut -d, -f3)
# The title card: the game fades to the paper colour and the logo sits on it for a moment.
card=2.6
total=$(python3 -c "print($seconds+$card)")
fade_out=$(python3 -c "print(max(0,$seconds-1.0))")
audio_fade=$(python3 -c "print(max(0,$total-2.0))")
# Music bed: the waltz looped under the whole cut, the sea and wind beds low beneath it.
ffmpeg -loglevel error -y \
  -stream_loop -1 -i "$audio/waltz.wav" -stream_loop -1 -i "$audio/sea.wav" -stream_loop -1 -i "$audio/wind.wav" \
  -filter_complex "[0:a]volume=0.55[m];[1:a]volume=0.22[s];[2:a]volume=0.10[w];[m][s][w]amix=inputs=3:duration=longest:normalize=0,atrim=0:$total,afade=t=in:st=0:d=1.2,afade=t=out:st=$audio_fade:d=2.0,aresample=48000[a]" \
  -map "[a]" -c:a aac -b:a 160k "$out/trailer-audio.m4a"
# Picture: fade in from black, the game, a fade to paper, then the logo on the paper card.
ffmpeg -loglevel error -y -framerate 24 -i "$frames/frame_%05d.png" -i "$logo" -f lavfi -i "color=c=0xf3eedc:s=1920x1080:r=24:d=$card" -i "$out/trailer-audio.m4a" \
  -filter_complex "[0:v]fade=t=in:st=0:d=1,fade=t=out:st=$fade_out:d=1.0:color=0xf3eedc,format=yuv420p[g];[1:v]scale=1200:-1[lg];[2:v][lg]overlay=(W-w)/2:(H-h)/2,fade=t=in:st=0:d=0.5:color=0xf3eedc,format=yuv420p[c];[g][c]concat=n=2:v=1:a=0[v]" \
  -map "[v]" -map 3:a -c:v libx264 -preset slow -crf 18 -pix_fmt yuv420p -movflags +faststart -shortest "$out/trailer.mp4"
# A short teaser for the README: five seconds of the street flight at 12 drawings a second.
teaser_count=$(( street_count < 120 ? street_count : 120 ))
ffmpeg -loglevel error -y -start_number "$street_first" -framerate 24 -i "$frames/frame_%05d.png" -frames:v "$teaser_count" \
  -vf "fps=12,scale=720:-1:flags=lanczos,split[a][b];[a]palettegen=max_colors=128:stats_mode=diff[p];[b][p]paletteuse=dither=bayer:bayer_scale=5" "$out/teaser.gif"
rm -f "$out/trailer-audio.m4a"
ls -lh "$out/trailer.mp4" "$out/teaser.gif"
