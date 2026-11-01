"""Cut idle gaps from actual sampled camera footage; retain timing inside every action.

Usage: python Encode-P49-ActionVideo.py FRAME_FOLDER FFMPEG OUTPUT.mp4
No interpolation, new frames, camera staging or speed changes. The JSON sidecar
records every source interval and cut so the highlight is not mistaken for a continuous take.
"""
import csv
import json
import pathlib
import statistics
import subprocess
import sys

folder, ffmpeg, output = map(pathlib.Path, sys.argv[1:])
rows = list(csv.reader((folder / "frames.csv").read_text().splitlines()))
if len(rows) < 8 or any(len(row) != 6 for row in rows):
    raise SystemExit("Missing actual action frames or invalid metadata")
groups = []
for row in rows:
    if groups and float(row[1]) <= float(groups[-1][-1][1]):
        raise SystemExit("Source timestamps are not increasing")
    if not groups or row[2] != groups[-1][-1][2] or float(row[1]) - float(groups[-1][-1][1]) > .5:
        groups.append([])
    groups[-1].append(row)
lines = ["ffconcat version 1.0"]
segments = []
for group in groups:
    gaps = [float(b[1]) - float(a[1]) for a, b in zip(group, group[1:])]
    last_duration = statistics.median(gaps) if gaps else .05
    for index, row in enumerate(group):
        frame = (folder / f"{int(row[0]):05}.png").resolve()
        if not frame.is_file():
            raise SystemExit(f"Missing source frame: {frame}")
        duration = gaps[index] if index < len(gaps) else last_duration
        lines += ["file '" + frame.as_posix().replace("'", "'\\''") + "'", f"duration {duration:.6f}"]
    segments.append(dict(kind=int(group[0][2]), first=int(group[0][0]), last=int(group[-1][0]),
                         source_start=float(group[0][1]), source_end=float(group[-1][1]),
                         output_duration=sum(gaps) + last_duration))
lines.append(lines[-2])
concat = folder / "highlights.ffconcat"
concat.write_text("\n".join(lines) + "\n", encoding="utf-8")
output.parent.mkdir(parents=True, exist_ok=True)
subprocess.run([str(ffmpeg), "-hide_banner", "-loglevel", "error", "-y", "-safe", "0", "-f", "concat", "-i", str(concat),
                "-vf", "scale=960:540", "-fps_mode", "vfr", "-c:v", "libx264", "-crf", "20", "-pix_fmt", "yuv420p",
                "-movflags", "+faststart", str(output)], check=True)
output.with_suffix(".segments.json").write_text(json.dumps(dict(actual_frames=len(rows), idle_gaps_cut=True,
    interpolated_frames=0, segments=segments), indent=2) + "\n", encoding="utf-8")
print(f"{len(rows)} actual frames, {len(segments)} action cuts, {sum(s['output_duration'] for s in segments):.2f} s: {output}")
