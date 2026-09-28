#!/usr/bin/env python3
"""Day 1 cutscene (the puppet show) -> Day1Play.mp4, applause under shot 10.

Writes to the existing Day1Play.mp4 filename so Unity's asset GUID and any
VideoPlayer reference stay intact. The old file is backed up outside Assets/
first, so Unity does not try to import the backup.
"""
import os, sys, glob, shutil, subprocess

def find_ffmpeg():
    import shutil, os
    for c in [shutil.which("ffmpeg"),
              r"C:\ffmpeg\bin\ffmpeg.exe",
              r"C:\Program Files\ffmpeg\bin\ffmpeg.exe",
              r"C:\ProgramData\chocolatey\bin\ffmpeg.exe"]:
        if c and os.path.exists(c):
            return c
    return None

HERE     = os.path.dirname(os.path.abspath(__file__))
FRAMES   = os.path.join(HERE, "frames")
APPLAUSE = os.path.join(HERE, "applause.wav")
DEST     = os.path.abspath(os.path.join(HERE, "..", "..", "Assets", "Cutscene Videos"))
OUT      = os.path.join(DEST, "Day1Play.mp4")
BACKUP   = os.path.abspath(os.path.join(HERE, "..", "Day1Play_previous.mp4"))

EXPECTED, FPS = 2730, 30
SHOT10_FRAME  = 2371
DELAY_MS = int(round((SHOT10_FRAME - 1) / float(FPS) * 1000))   # 79000

frames = sorted(glob.glob(os.path.join(FRAMES, "f_*.png")))
if not frames:
    sys.exit("No frames in %s" % FRAMES)
nums = set(int(os.path.basename(f)[2:-4]) for f in frames)
missing = [n for n in range(1, EXPECTED + 1) if n not in nums]
print("%d/%d frames" % (len(frames), EXPECTED))
if missing:
    print("WARNING: %d frames missing, first: %s" % (len(missing), missing[:10]))
    if input("Encode anyway? (y/n): ").strip().lower() != "y":
        sys.exit(0)

ff = find_ffmpeg()
if not ff:
    sys.exit("FFmpeg not found (expected C:\\ffmpeg\\bin\\ffmpeg.exe)")
os.makedirs(DEST, exist_ok=True)

if os.path.exists(OUT) and not os.path.exists(BACKUP):
    shutil.copy2(OUT, BACKUP)
    print("backed up the previous Day1Play.mp4 -> %s" % BACKUP)

cmd = [ff, "-y", "-framerate", str(FPS), "-start_number", "1",
       "-i", os.path.join(FRAMES, "f_%04d.png")]
# --- look: crushed blacks, cooler desaturated shadows, deep vignette, fine grain
GRADE = ("colorlevels=rimin=0.065:gimin=0.060:bimin=0.040:romax=0.96:gomax=0.97:bomax=1.00,eq=saturation=0.80:contrast=1.08:gamma=0.96,colorbalance=rs=-0.05:gs=-0.01:bs=0.09:rm=-0.02:bm=0.03,vignette=angle=PI/3.2,noise=alls=6:allf=t+u")

if os.path.exists(APPLAUSE):
    cmd += ["-i", APPLAUSE,
            "-filter_complex",
            "[0:v]" + GRADE + "[v];[1:a]adelay=%d|%d,apad[a]" % (DELAY_MS, DELAY_MS),
            "-map", "[v]", "-map", "[a]",
            "-c:a", "aac", "-b:a", "192k", "-shortest"]
    print("applause enters at %.2fs (shot 10)" % (DELAY_MS / 1000.0))
else:
    cmd += ["-filter_complex", "[0:v]" + GRADE + "[v]", "-map", "[v]"]
    print("applause.wav missing - encoding silent")
cmd += ["-c:v", "libx264", "-preset", "slow", "-crf", "17",
        "-pix_fmt", "yuv420p", "-movflags", "+faststart", OUT]
subprocess.run(cmd, check=True)

print("\nDone: %s (%.1f MB)" % (OUT, os.path.getsize(OUT) / (1024.0 * 1024)))
print("Unity keeps its existing reference - no re-assignment needed.")
