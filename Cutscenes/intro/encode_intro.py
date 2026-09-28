#!/usr/bin/env python3
"""Intro cutscene -> two MP4s, one per Paul VO take.

Both are built from the same rendered frames; only the voice track differs.
-shortest lets the VO govern length so the final line is never clipped.
"""
import os, sys, glob, subprocess

def find_ffmpeg():
    import shutil, os
    for c in [shutil.which("ffmpeg"),
              r"C:\ffmpeg\bin\ffmpeg.exe",
              r"C:\Program Files\ffmpeg\bin\ffmpeg.exe",
              r"C:\ProgramData\chocolatey\bin\ffmpeg.exe"]:
        if c and os.path.exists(c):
            return c
    return None

HERE   = os.path.dirname(os.path.abspath(__file__))
FRAMES = os.path.join(HERE, "frames")
DEST   = os.path.abspath(os.path.join(HERE, "..", "..", "Assets", "Cutscene Videos"))
FPS = 30
TAKES = [("paul_intro_take1.wav", "Intro_Take1.mp4"),
         ("paul_intro_take2.wav", "Intro_Take2.mp4")]

frames = sorted(glob.glob(os.path.join(FRAMES, "f_*.png")))
if not frames:
    sys.exit("No frames in %s - render the intro first." % FRAMES)
print("%d frames (%.2fs of picture)" % (len(frames), len(frames) / float(FPS)))

ff = find_ffmpeg()
if not ff:
    sys.exit("FFmpeg not found (expected C:\\ffmpeg\\bin\\ffmpeg.exe)")
os.makedirs(DEST, exist_ok=True)

made = []
for wav, out_name in TAKES:
    src = os.path.join(HERE, wav)
    if not os.path.exists(src):
        print("skipping %s - not found" % wav)
        continue
    out = os.path.join(DEST, out_name)
    cmd = [ff, "-y", "-framerate", str(FPS), "-start_number", "1",
           "-i", os.path.join(FRAMES, "f_%04d.png"),
           "-i", src,
           "-c:v", "libx264", "-preset", "slow", "-crf", "17",
           "-pix_fmt", "yuv420p",
           "-c:a", "aac", "-b:a", "192k",
           "-shortest", "-movflags", "+faststart", out]
    print("\nEncoding %s ..." % out_name)
    subprocess.run(cmd, check=True)
    made.append((out, os.path.getsize(out) / (1024.0 * 1024)))
    print("  %.1f MB" % made[-1][1])

print("\nBoth takes written to Assets/Cutscene Videos/:")
for p, mb in made:
    print("  %s  (%.1f MB)" % (os.path.basename(p), mb))
print("\nIn Unity, drag the one you prefer onto the VideoPlayer in "
      "FirstGameCutscene (the intro player, not the backstory ones).")
