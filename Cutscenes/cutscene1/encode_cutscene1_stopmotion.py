#!/usr/bin/env python3
"""Day 1 cutscene -> Day1Play.mp4, as a stop-motion cut.

Each of the 10 shots is shown as a burst of 30 poses (2-5 seconds),
then cuts to black, then the next shot. Only those 300 frames need
rendering - run render_cutscene1.ps1 first (it skips frames that
already exist), then this script.

    python encode_cutscene1_stopmotion.py            final encode
    python encode_cutscene1_stopmotion.py --preview  small + quick
    python encode_cutscene1_stopmotion.py --write-render  rewrite the .ps1
"""
import os, sys
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, ".."))
import stopmotion_cut as sm

LAST_FRAME = 2730
SHOT_STARTS = [1, 301, 511, 781, 1111, 1381, 1771, 1891, 2161, 2371]

GRADE = (
    "colorlevels=rimin=0.065:gimin=0.060:bimin=0.040"
    ":romax=0.96:gomax=0.97:bomax=1.00,"
    "eq=saturation=0.80:contrast=1.08:gamma=0.96,"
    "colorbalance=rs=-0.05:gs=-0.01:bs=0.09:rm=-0.02:bm=0.03,"
    "vignette=angle=PI/3.2,"
    "noise=alls=6:allf=t+u"
)

FRAMES = os.path.join(HERE, "frames")
DEST = os.path.abspath(os.path.join(HERE, "..", "..",
                                    "Assets", "Cutscene Videos"))
OUT = os.path.join(DEST, "Day1Play.mp4")
BACKUP = os.path.abspath(os.path.join(HERE, "..", "Day1Play_previous.mp4"))
APPLAUSE = os.path.join(HERE, "applause.wav")


def main():
    segs = sm.plan(SHOT_STARTS, LAST_FRAME)
    total, _ = sm.timeline(segs)

    ps1 = os.path.join(HERE, "render_cutscene1.ps1")
    if "--write-render" in sys.argv or not os.path.exists(ps1):
        with open(ps1, "w", newline="") as fh:
            fh.write(sm.blender_script("cutscene1.blend", segs,
                                       "cutscene 1"))
        print("wrote %s" % ps1)
        if "--write-render" in sys.argv:
            return

    only = None
    if "--only" in sys.argv:
        only = int(sys.argv[sys.argv.index("--only") + 1])
        segs = segs[:only]
        sm.timeline(segs)

    gone = sm.missing(segs, FRAMES)
    if gone:
        print("%d of the needed frames are not rendered yet, first: %s"
              % (len(gone), gone[:8]))
        print("Run render_cutscene1.ps1 first.")
        sys.exit(1)

    # Applause comes in as shot 10 starts.
    audio = None
    if len(segs) == len(SHOT_STARTS):
        audio = {"path": APPLAUSE, "in_t": segs[-1]["t0"],
                 "out_t": total - sm.END_BLACK - 0.5}

    preview = "--preview" in sys.argv
    out = os.path.join(sm_work(), "preview.mp4") if preview else OUT
    os.makedirs(os.path.dirname(out), exist_ok=True)
    sm.encode(segs, FRAMES, sm_work(), GRADE, out, BACKUP,
              audio=audio, preview=preview)


def sm_work():
    return os.path.join(HERE, "_encode_work")


if __name__ == "__main__":
    main()
