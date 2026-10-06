#!/usr/bin/env python3
"""Cutscene 2 "The Actor" -> Day2Play.mp4, stop-motion cut.

The whole piece is stepped to 8 exposures a second so every gesture
lands as a series of poses - a puppet moves in increments because
something is moving it, and Vashin, stepped at the same rate, is
being moved too.

The one exception is the chandelier. SMOOTH_WINDOW below is left at
full frame rate, so the only fluid motion in the cutscene is the
thing that kills him. The puppet world steps; gravity does not.
Set SMOOTH_WINDOW to None to step it like everything else.
"""
import os, sys, glob, shutil, subprocess

FPS       = 30
STEP_FPS  = 8
FADE      = 0.35
HOLD_BLACK= 0.12
EXPECTED  = 1830

# Beat boundaries, matching build_cutscene2.py.
B1, B2, B3, B4, B5 = 1, 421, 721, 1081, 1351
BEAT_STARTS = [B1, B2, B3, B4, B5]

# Beat 5 opens on the chandelier already falling. The silence and the
# held stare in beat 4 exist to set that up; dissolving into it
# throws the only hard hit in the piece away.
NO_FADE_AT = [B5]

FALL        = B5 + 36          # frame the chandelier lets go
SMOOTH_WINDOW = (FALL - 2, FALL + 18)

APPLAUSE_IN  = 14.0            # beat 2
APPLAUSE_OUT = 45.6            # it stops when the chandelier lands

GRADE = (
    "colorlevels=rimin=0.060:gimin=0.058:bimin=0.038"
    ":romax=0.96:gomax=0.97:bomax=1.00,"
    "eq=saturation=0.74:contrast=1.10:gamma=0.94,"
    "colorbalance=rs=-0.06:gs=-0.01:bs=0.10:rm=-0.02:bm=0.04,"
    "vignette=angle=PI/3.0,"
    "noise=alls=7:allf=t+u"
)

HERE   = os.path.dirname(os.path.abspath(__file__))
FRAMES = os.path.join(HERE, "frames")
DEST   = os.path.abspath(os.path.join(HERE, "..", "..",
                                      "Assets", "Cutscene Videos"))
OUT    = os.path.join(DEST, "Day2Play.mp4")
BACKUP = os.path.abspath(os.path.join(HERE, "..", "Day2Play_previous.mp4"))
APPLAUSE = os.path.abspath(os.path.join(HERE, "..", "cutscene1", "applause.wav"))


def find_ffmpeg():
    for c in [shutil.which("ffmpeg"),
              r"C:\ffmpeg\bin\ffmpeg.exe",
              r"C:\Program Files\ffmpeg\bin\ffmpeg.exe",
              r"C:\ProgramData\chocolatey\bin\ffmpeg.exe"]:
        if c and os.path.exists(c):
            return c
    return None


def t(frame):
    return (frame - 1) / float(FPS)


def video_chain():
    """Stepped everywhere except the fall, which stays fluid.

    The grade sits BETWEEN the decimation and the re-expansion on the
    stepped segments, so grain is generated once per surviving
    exposure and freezes with the pose it belongs to. Grain that
    crawls while the puppet holds still is what gives fake
    stop-motion away.
    """
    stepped = "fps=%d,%s,fps=%d" % (STEP_FPS, GRADE, FPS)

    if SMOOTH_WINDOW is None:
        return "[0:v]" + stepped + "[g]", "[g]"

    a, b = t(SMOOTH_WINDOW[0]), t(SMOOTH_WINDOW[1])
    parts = [
        "[0:v]trim=start=0:end=%.3f,setpts=PTS-STARTPTS,%s[s0]" % (a, stepped),
        "[0:v]trim=start=%.3f:end=%.3f,setpts=PTS-STARTPTS,%s[s1]" % (a, b, GRADE),
        "[0:v]trim=start=%.3f,setpts=PTS-STARTPTS,%s[s2]" % (b, stepped),
        "[s0][s1][s2]concat=n=3:v=1[g]",
    ]
    return ";".join(parts), "[g]"


def fade_chain():
    out = []
    for start in BEAT_STARTS[1:]:
        if start in NO_FADE_AT:
            continue
        cut = t(start)
        o = cut - FADE - HOLD_BLACK / 2.0
        i = cut + HOLD_BLACK / 2.0
        if o < 0:
            continue
        out.append("fade=t=out:st=%.3f:d=%.3f:color=black" % (o, FADE))
        out.append("fade=t=in:st=%.3f:d=%.3f:color=black" % (i, FADE))
    # Out on the last frame.
    out.append("fade=t=out:st=%.3f:d=0.6:color=black" % (t(EXPECTED) - 0.6))
    return ",".join(out)


def main():
    frames = sorted(glob.glob(os.path.join(FRAMES, "f_*.png")))
    if not frames:
        sys.exit("No frames in %s - render first." % FRAMES)

    nums = set(int(os.path.basename(f)[2:-4]) for f in frames)
    missing = [n for n in range(1, EXPECTED + 1) if n not in nums]
    print("%d/%d frames" % (len(frames), EXPECTED))
    if missing:
        print("WARNING: %d missing, first: %s" % (len(missing), missing[:10]))
        if input("Encode anyway? (y/n): ").strip().lower() != "y":
            sys.exit(0)

    ff = find_ffmpeg()
    if not ff:
        sys.exit("FFmpeg not found (expected C:\\ffmpeg\\bin\\ffmpeg.exe)")

    os.makedirs(DEST, exist_ok=True)
    if os.path.exists(OUT) and not os.path.exists(BACKUP):
        shutil.copy2(OUT, BACKUP)
        print("kept the old Day2Play -> %s" % BACKUP)

    vchain, vlabel = video_chain()
    fades = fade_chain()
    graph = vchain + ";" + vlabel + fades + "[v]"

    cmd = [ff, "-y", "-framerate", str(FPS), "-start_number", "1",
           "-i", os.path.join(FRAMES, "f_%04d.png")]

    if os.path.exists(APPLAUSE):
        # Comes in on beat 2 and stops dead when the chandelier lands.
        cmd += ["-i", APPLAUSE]
        graph += (";[1:a]adelay=%d|%d,apad,"
                  "afade=t=out:st=%.2f:d=0.25[a]"
                  % (int(APPLAUSE_IN*1000), int(APPLAUSE_IN*1000),
                     APPLAUSE_OUT))
        cmd += ["-filter_complex", graph,
                "-map", "[v]", "-map", "[a]",
                "-c:a", "aac", "-b:a", "192k", "-shortest"]
        print("applause %.1fs -> %.1fs" % (APPLAUSE_IN, APPLAUSE_OUT))
    else:
        cmd += ["-filter_complex", graph, "-map", "[v]"]
        print("no applause.wav - encoding silent")

    cmd += ["-c:v", "libx264", "-preset", "slow", "-crf", "17",
            "-pix_fmt", "yuv420p", "-movflags", "+faststart", OUT]

    print("stepping to %d fps; fall kept fluid at %s"
          % (STEP_FPS, "off" if SMOOTH_WINDOW is None
             else "%.2f-%.2fs" % (t(SMOOTH_WINDOW[0]), t(SMOOTH_WINDOW[1]))))
    subprocess.run(cmd, check=True)
    print("\nDone: %s (%.1f MB)"
          % (OUT, os.path.getsize(OUT) / (1024.0*1024)))


if __name__ == "__main__":
    main()
