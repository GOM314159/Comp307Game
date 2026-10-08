#!/usr/bin/env python3
"""Cutscene 2 "The Actor" -> Day2Play.mp4, as a stop-motion cut.

Each of the 5 beats is shown as a burst of 30 poses (2-5 seconds),
then cuts to black, then the next beat. Only those 150 frames need
rendering - run render_cutscene2.ps1 first (it skips frames that
already exist), then this script.

    python encode_cutscene2.py            final encode
    python encode_cutscene2.py --preview  small + quick
    python encode_cutscene2.py --write-render  rewrite the .ps1
"""
import os, sys
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, ".."))
import stopmotion_cut as sm

LAST_FRAME = 1830
B1, B2, B3, B4, B5 = 1, 421, 721, 1081, 1351
BEAT_STARTS = [B1, B2, B3, B4, B5]

GRADE = (
    "colorlevels=rimin=0.060:gimin=0.058:bimin=0.038"
    ":romax=0.96:gomax=0.97:bomax=1.00,"
    "eq=saturation=0.74:contrast=1.10:gamma=0.94,"
    "colorbalance=rs=-0.06:gs=-0.01:bs=0.10:rm=-0.02:bm=0.04,"
    "vignette=angle=PI/3.0,"
    "noise=alls=7:allf=t+u"
)

FRAMES = os.path.join(HERE, "frames")
DEST = os.path.abspath(os.path.join(HERE, "..", "..",
                                    "Assets", "Cutscene Videos"))
OUT = os.path.join(DEST, "Day2Play.mp4")
BACKUP = os.path.abspath(os.path.join(HERE, "..", "Day2Play_previous.mp4"))
APPLAUSE = os.path.abspath(os.path.join(HERE, "..", "cutscene1",
                                        "applause.wav"))
WORK = os.path.join(HERE, "_encode_work")


def main():
    segs = sm.plan(BEAT_STARTS, LAST_FRAME)
    sm.timeline(segs)

    ps1 = os.path.join(HERE, "render_cutscene2.ps1")
    if "--write-render" in sys.argv or not os.path.exists(ps1):
        with open(ps1, "w", newline="") as fh:
            fh.write(sm.blender_script("cutscene2.blend", segs,
                                       "cutscene 2"))
        print("wrote %s" % ps1)
        if "--write-render" in sys.argv:
            return

    only = None
    if "--only" in sys.argv:
        only = int(sys.argv[sys.argv.index("--only") + 1])
        segs = segs[:only]

    gone = sm.missing(segs, FRAMES)
    if gone:
        print("%d of the needed frames are not rendered yet, first: %s"
              % (len(gone), gone[:8]))
        print("Run render_cutscene2.ps1 first.")
        sys.exit(1)

    # Applause comes in with beat 2 and stops dead on the pose where
    # the chandelier has landed (4th pose of beat 5).
    audio = None
    if len(segs) == len(BEAT_STARTS):
        b5 = segs[4]
        audio = {"path": APPLAUSE, "in_t": segs[1]["t0"],
                 "out_t": b5["t0"] + 3 * b5["per"]}

    preview = "--preview" in sys.argv
    out = os.path.join(WORK, "preview.mp4") if preview else OUT
    os.makedirs(os.path.dirname(out), exist_ok=True)
    sm.encode(segs, FRAMES, WORK, GRADE, out, BACKUP,
              audio=audio, preview=preview)


if __name__ == "__main__":
    main()
