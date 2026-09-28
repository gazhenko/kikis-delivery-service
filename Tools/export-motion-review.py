#!/usr/bin/env python3
"""Encode the native --koriko-motion-check drawings without altering their timing.
Requires ffmpeg on PATH. The raw captures stay in Unity's persistentDataPath;
only videos, telemetry and selected actual frames go into the verification folder.
"""
import argparse
import csv
from pathlib import Path
import shutil
import subprocess

parser=argparse.ArgumentParser(description=__doc__)
parser.add_argument('source',type=Path)
parser.add_argument('destination',type=Path)
args=parser.parse_args()
source=args.source.expanduser().resolve();destination=args.destination.resolve()
destination.mkdir(parents=True,exist_ok=True)
movies=[]
for table in sorted(source.glob('*.csv')):
    name=table.stem
    rows=list(csv.DictReader(table.open()))
    frames=source/name
    images=sorted(frames.glob('frame_*.png'))
    if len(images)!=len(rows):raise SystemExit(f'{name}: {len(images)} images, {len(rows)} telemetry rows; incomplete capture')
    movie=destination/(name+'.mp4')
    subprocess.run(['ffmpeg','-hide_banner','-loglevel','error','-y','-framerate','24','-i',str(frames/'frame_%05d.png'),'-c:v','libx264','-threads','4','-preset','medium','-crf','19','-pix_fmt','yuv420p','-movflags','+faststart',str(movie)],check=True)
    movies.append(movie)
    shutil.copy2(table,destination/table.name)
    # Keep real adjacent frames around the first smear, plus one pose per action.
    chosen={0,len(rows)-1}
    for i,row in enumerate(rows):
        if i and row['action']!=rows[i-1]['action']:chosen.add(min(i+4,len(rows)-1))
    first_smear=next((i for i,row in enumerate(rows) if float(row['smear'])>0),None)
    if first_smear is not None:chosen.update(range(max(0,first_smear-1),min(len(rows),first_smear+4)))
    stills=destination/name;stills.mkdir(exist_ok=True)
    for i in sorted(chosen):shutil.copy2(frames/f'frame_{i:05}.png',stills/f'frame_{i:05}.png')
    print(name,len(rows),'drawings',f'{len(rows)/24:.2f}s')
# All four clips have identical dimensions, codec and frame rate.
manifest=destination/'clips.ffconcat'
manifest.write_text('ffconcat version 1.0\n'+''.join(f"file '{movie.name}'\n" for movie in movies))
subprocess.run(['ffmpeg','-hide_banner','-loglevel','error','-y','-safe','1','-f','concat','-i',str(manifest),'-c','copy','-movflags','+faststart',str(destination/'Kiki-motion-reel.mp4')],check=True)
manifest.unlink()
shutil.copy2(source/'result.txt',destination/'result.txt')
