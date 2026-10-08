#!/usr/bin/env python3
"""Shared stop-motion cutter for the cutscene encoders.

THE IDEA
    Instead of rendering and encoding every frame, each scene of a
    cutscene is shown as a short burst of SEG_FRAMES (30) poses taken
    evenly across that scene, played slowly (2-5 seconds), then it
    cuts straight to black and on to the next scene. The story still
    runs start to finish; only the poses that are shown are rendered.
    A 2730-frame cutscene becomes 300 rendered frames.

Each scene's burst lasts L/90 seconds, clamped to 2-5 s, where L is
the scene length in frames. 30 poses over 2-5 s is 6-15 poses per
second, which reads as stop-motion.

Poses are on a fixed grid (scene start + k * step) so Blender can
render them with  -s START -e END -j STEP -a  and the encoder can
find them again.
"""
import os, sys, shutil, subprocess

FPS = 30
SEG_FRAMES = 30
MIN_DUR = 2.0
MAX_DUR = 5.0
BLACK_HOLD = 0.5      # seconds of black between scenes
END_BLACK = 1.0       # seconds of black at the very end


def find_ffmpeg():
    for c in [shutil.which("ffmpeg"),
              r"C:\ffmpeg\bin\ffmpeg.exe",
              r"C:\Program Files\ffmpeg\bin\ffmpeg.exe",
              r"C:\ProgramData\chocolatey\bin\ffmpeg.exe"]:
        if c and os.path.exists(c):
            return c
    return None


def plan(starts, last_frame):
    """Scene list: start, step, frames, burst duration, black windows
    are worked out later. Every scene must be >= SEG_FRAMES long."""
    segs = []
    for i, s in enumerate(starts):
        e = (starts[i + 1] - 1) if i + 1 < len(starts) else last_frame
        length = e - s + 1
        step = max(1, length // SEG_FRAMES)
        frames = [s + k * step for k in range(SEG_FRAMES)]
        frames = [f for f in frames if f <= e]
        dur = min(MAX_DUR, max(MIN_DUR, length / 90.0))
        segs.append({"start": s, "end": e, "step": step,
                     "frames": frames, "dur": dur})
    return segs


def timeline(segs):
    """Start time of each burst in the finished video, and the black
    windows between them."""
    t = 0.0
    out = []
    black = []
    for i, seg in enumerate(segs):
        seg["t0"] = t
        seg["per"] = seg["dur"] / len(seg["frames"])
        t += seg["dur"]
        gap = BLACK_HOLD if i < len(segs) - 1 else END_BLACK
        black.append((t, t + gap))
        t += gap
    return t, black


def blender_script(blend, segs, label):
    """PowerShell that renders only the poses that are used.
    Re-runnable: Blender skips frames that already exist."""
    lines = [
        "# Renders only the frames the %s stop-motion cut uses." % label,
        "# Safe to stop and re-run; existing frames are skipped.",
        '$B = "C:\\Program Files (x86)\\Steam\\steamapps\\common\\Blender\\blender.exe"',
        '$F = Join-Path $PSScriptRoot "%s"' % blend,
        "",
    ]
    for i, s in enumerate(segs):
        lines.append("Write-Host 'scene %d of %d: frames %d..%d step %d'"
                     % (i + 1, len(segs), s["frames"][0],
                        s["frames"][-1], s["step"]))
        lines.append('& $B -b $F -s %d -e %d -j %d -a'
                     % (s["frames"][0], s["frames"][-1], s["step"]))
    lines.append("Write-Host 'done'")
    return "\r\n".join(lines) + "\r\n"


def missing(segs, frames_dir):
    out = []
    for s in segs:
        for f in s["frames"]:
            if not os.path.exists(os.path.join(frames_dir,
                                               "f_%04d.png" % f)):
                out.append(f)
    return out


def encode(segs, frames_dir, work_dir, grade, out_path, backup_path,
           audio=None, preview=False):
    """audio = dict(path, in_t, out_t) with times in seconds, or None."""
    ff = find_ffmpeg()
    if not ff:
        sys.exit("FFmpeg not found (expected C:\\ffmpeg\\bin\\ffmpeg.exe)")

    total, black = timeline(segs)
    os.makedirs(work_dir, exist_ok=True)

    # One black frame the same size as the renders.
    first = os.path.join(frames_dir, "f_%04d.png" % segs[0]["frames"][0])
    black_png = os.path.join(work_dir, "black.png")
    subprocess.run([ff, "-y", "-v", "error", "-i", first, "-vf",
                    "drawbox=x=0:y=0:w=iw:h=ih:color=black:t=fill",
                    "-frames:v", "1", black_png], check=True)

    def q(p):
        return p.replace("\\", "/").replace("'", "'\\''")

    lines = []
    for i, seg in enumerate(segs):
        for f in seg["frames"]:
            lines.append("file '%s'" % q(os.path.join(
                frames_dir, "f_%04d.png" % f)))
            lines.append("duration %.6f" % seg["per"])
        gap = BLACK_HOLD if i < len(segs) - 1 else END_BLACK
        lines.append("file '%s'" % q(black_png))
        lines.append("duration %.6f" % gap)
    lines.append("file '%s'" % q(black_png))      # concat quirk

    list_path = os.path.join(work_dir, "list.txt")
    with open(list_path, "w", newline="\n") as fh:
        fh.write("\n".join(lines) + "\n")

    # Grade each pose ONCE (so grain freezes with the pose), then
    # expand to 30 fps, then force the gaps to true black - the
    # grade's grain would otherwise lift them to grey.
    vf = [grade] if not preview else ["scale=640:-2", grade]
    vf.append("fps=%d" % FPS)
    for a, b in black:
        vf.append("drawbox=x=0:y=0:w=iw:h=ih:color=black:t=fill"
                  ":enable='between(t,%.3f,%.3f)'" % (a - 0.001, b - 0.001))
    graph = "[0:v]" + ",".join(vf) + "[v]"

    cmd = [ff, "-y", "-f", "concat", "-safe", "0", "-i", list_path]
    if audio and os.path.exists(audio["path"]):
        ms = int(audio["in_t"] * 1000)
        graph += (";[1:a]adelay=%d|%d,apad,afade=t=out:st=%.3f:d=0.25[a]"
                  % (ms, ms, audio["out_t"]))
        cmd += ["-i", audio["path"], "-filter_complex", graph,
                "-map", "[v]", "-map", "[a]",
                "-c:a", "aac", "-b:a", "192k", "-t", "%.3f" % total]
        print("applause %.2fs -> %.2fs" % (audio["in_t"], audio["out_t"]))
    else:
        cmd += ["-filter_complex", graph, "-map", "[v]",
                "-t", "%.3f" % total]
        print("encoding silent")

    cmd += ["-c:v", "libx264", "-preset", "fast" if preview else "slow",
            "-crf", "23" if preview else "17",
            "-pix_fmt", "yuv420p", "-movflags", "+faststart", out_path]

    if (not preview and os.path.exists(out_path)
            and not os.path.exists(backup_path)):
        shutil.copy2(out_path, backup_path)
        print("kept the old video -> %s" % backup_path)

    print("%d scenes, %d poses, %.1f s" %
          (len(segs), sum(len(s["frames"]) for s in segs), total))
    subprocess.run(cmd, check=True)
    print("Done: %s (%.1f MB)" % (out_path,
                                  os.path.getsize(out_path) / 1048576.0))
