"""Encode actual camera frames with their sampled game-time durations (no interpolation).

Usage: python tools/Encode-P187-RepairVideo.py FRAME_FOLDER FFMPEG OUTPUT.mp4
The development player writes frames.csv and numbered PNGs. ffmpeg is supplied by
the caller; no download, global install, or gameplay dependency is introduced.
"""
import csv
import pathlib
import subprocess
import sys

folder, ffmpeg, output = map(pathlib.Path, sys.argv[1:])
rows = list(csv.reader((folder / "frames.csv").read_text().splitlines()))
# Existing development players use the Windows locale for F4 timestamps. Turkish
# decimal commas split that one value into two fields; recover the exact timestamp.
rows = [([r[0], r[1] + "." + r[2], *r[3:]] if len(r) == 7 and r[4] in ("True", "False") else r)
        for r in rows]
if any(len(r) != 6 for r in rows):
    raise SystemExit("Unexpected frame metadata shape")
if len(rows) < 8:
    raise SystemExit("Not enough actual repair frames")
concat = folder / "frames.ffconcat"
lines = ["ffconcat version 1.0"]
for index, row in enumerate(rows):
    frame = (folder / f"{int(row[0]):05}.png").resolve()
    if not frame.is_file():
        raise SystemExit(f"Missing frame: {frame}")
    duration = float(rows[index + 1][1]) - float(row[1]) if index + 1 < len(rows) else 0.125
    if duration <= 0:
        raise SystemExit("Frame timestamps are not increasing")
    lines += ["file '" + frame.as_posix().replace("'", "'\\''") + "'", f"duration {duration:.6f}"]
lines.append(lines[-2])  # Keep the final frame for its sampled duration.
concat.write_text("\n".join(lines) + "\n", encoding="utf-8")
output.parent.mkdir(parents=True, exist_ok=True)
subprocess.run([str(ffmpeg), "-hide_banner", "-loglevel", "error", "-y", "-safe", "0",
                "-f", "concat", "-i", str(concat), "-vf", "scale=960:540",
                "-fps_mode", "vfr", "-c:v", "libx264", "-crf", "23", "-pix_fmt", "yuv420p",
                "-movflags", "+faststart", str(output)], check=True)
print(f"{len(rows)} actual frames, {float(rows[-1][1]) - float(rows[0][1]):.2f} s: {output}")
