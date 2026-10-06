#!/usr/bin/env python3
"""Day 1 cutscene, re-cut as stop-motion -> Day1Play.mp4

Nothing is re-rendered. The existing 2730 frames are decimated to a
stepped frame rate so the motion judders like a puppet being moved by
hand between exposures, and each shot change becomes a fade through
black instead of a hard cut.

Duration is unchanged, so the applause still lands on shot 10.

Writes to the existing Day1Play.mp4 filename so Unity's asset GUID and
any VideoPlayer reference stay intact.
"""
import os, sys, glob, shutil, subprocess

# =====================================================================
# THE LOOK
# =====================================================================

# Exposures per second. Real stop-motion is usually shot "on twos" or
# "on threes" off 24fps, i.e. 8-12 distinct poses a second. Lower is
# more puppet-like and more unsettling; higher is smoother.
STEP_FPS = 8

# Seconds of fade either side of a shot change.
FADE = 0.35

# Hold on black between shots, on top of the two fades.
HOLD_BLACK = 0.12

FPS = 30
EXPECTED = 2730

# First frame of each shot. Shot 1 starts at 1 and gets no fade in
# front of it - the curtain is already the opening image.
SHOT_STARTS = [1, 301, 511, 781, 1111, 1381, 1771, 1891, 2161, 2371]

SHOT10_FRAME = 2371

# Shot changes that keep their hard cut. Frame 1891 is the spotlight
# snapping on after the blackout - the one hard hit in the whole
# piece, and the thing the silence before it is setting up. Fading
# into it throws that away.
NO_FADE_AT = [1891]

# Carried over from the continuous cut: crushed blacks, cooler
# desaturated shadows, deep vignette, fine grain.
GRADE = (
    "colorlevels=rimin=0.065:gimin=0.060:bimin=0.040"
    ":romax=0.96:gomax=0.97:bomax=1.00,"
    "eq=saturation=0.80:contrast=1.08:gamma=0.96,"
    "colorbalance=rs=-0.05:gs=-0.01:bs=0.09:rm=-0.02:bm=0.03,"
    "vignette=angle=PI/3.2,"
    "noise=alls=6:allf=t+u"
)


def find_ffmpeg():
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
DEST     = os.path.abspath(os.path.join(HERE, "..", "..",
                                        "Assets", "Cutscene Videos"))
OUT      = os.path.join(DEST, "Day1Play.mp4")
BACKUP   = os.path.abspath(os.path.join(HERE, "..",
                                        "Day1Play_continuous.mp4"))

DELAY_MS = int(round((SHOT10_FRAME - 1) / float(FPS) * 1000))


def build_fade_chain():
    """A fade out into each shot change and a fade in out of it.

    The windows never overlap, so the fades compose correctly when
    chained. Shot 1 is skipped - there is nothing before it to fade
    away from.
    """
    parts = []

    for start in SHOT_STARTS[1:]:
        if start in NO_FADE_AT:
            continue

        cut = (start - 1) / float(FPS)

        out_at = cut - FADE - (HOLD_BLACK / 2.0)
        in_at  = cut + (HOLD_BLACK / 2.0)

        if out_at < 0:
            continue

        parts.append("fade=t=out:st=%.3f:d=%.3f:color=black"
                     % (out_at, FADE))
        parts.append("fade=t=in:st=%.3f:d=%.3f:color=black"
                     % (in_at, FADE))

    return ",".join(parts)


def main():
    frames = sorted(glob.glob(os.path.join(FRAMES, "f_*.png")))

    if not frames:
        sys.exit("No frames in %s\n"
                 "Are you on the Lobby-Day2-Decay branch?" % FRAMES)

    nums = set(int(os.path.basename(f)[2:-4]) for f in frames)
    missing = [n for n in range(1, EXPECTED + 1) if n not in nums]

    print("%d/%d frames" % (len(frames), EXPECTED))

    if missing:
        print("WARNING: %d missing, first: %s"
              % (len(missing), missing[:10]))
        if input("Encode anyway? (y/n): ").strip().lower() != "y":
            sys.exit(0)

    ff = find_ffmpeg()
    if not ff:
        sys.exit("FFmpeg not found (expected C:\\ffmpeg\\bin\\ffmpeg.exe)")

    os.makedirs(DEST, exist_ok=True)

    if os.path.exists(OUT) and not os.path.exists(BACKUP):
        shutil.copy2(OUT, BACKUP)
        print("kept the continuous cut -> %s" % BACKUP)

    # Order matters here.
    #
    #   fps=STEP_FPS   throw away the in-between frames. This is the
    #                  stop-motion step itself.
    #   GRADE          colour and grain, applied once per surviving
    #                  exposure...
    #   fps=FPS        ...and only then stretched back out, so the
    #                  grain freezes with the pose it belongs to
    #                  rather than crawling underneath it. Grain that
    #                  moves while the puppet does not is the thing
    #                  that gives a fake stop-motion away.
    #   fades          last, so black is actually black.
    video = "fps=%d,%s,fps=%d" % (STEP_FPS, GRADE, FPS)

    fades = build_fade_chain()
    if fades:
        video += "," + fades

    cmd = [ff, "-y", "-framerate", str(FPS), "-start_number", "1",
           "-i", os.path.join(FRAMES, "f_%04d.png")]

    if os.path.exists(APPLAUSE):
        cmd += ["-i", APPLAUSE,
                "-filter_complex",
                "[0:v]" + video + "[v];"
                "[1:a]adelay=%d|%d,apad[a]" % (DELAY_MS, DELAY_MS),
                "-map", "[v]", "-map", "[a]",
                "-c:a", "aac", "-b:a", "192k", "-shortest"]
        print("applause enters at %.2fs" % (DELAY_MS / 1000.0))
    else:
        cmd += ["-filter_complex", "[0:v]" + video + "[v]",
                "-map", "[v]"]
        print("applause.wav missing - encoding silent")

    cmd += ["-c:v", "libx264", "-preset", "slow", "-crf", "17",
            "-pix_fmt", "yuv420p", "-movflags", "+faststart", OUT]

    print("stepping %d fps -> %d fps, %d fades (%d hard cut kept)"
          % (FPS, STEP_FPS,
             len(SHOT_STARTS) - 1 - len(NO_FADE_AT),
             len(NO_FADE_AT)))

    subprocess.run(cmd, check=True)

    print("\nDone: %s (%.1f MB)"
          % (OUT, os.path.getsize(OUT) / (1024.0 * 1024)))
    print("Unity keeps its existing reference.")


if __name__ == "__main__":
    main()
