#!/usr/bin/env python3
"""
Encode PNG sequence + audio to MP4
Run this after rendering is complete: python encode_to_mp4.py
"""

import subprocess
import os
import sys
from pathlib import Path

# Paths
FRAMES_DIR = Path(r"C:\Users\imaha\source\repos\Comp307Game\Cutscenes\intro\frames")
AUDIO_FILE = Path(r"C:\Users\imaha\source\repos\Comp307Game\Cutscenes\intro\intro_VO.wav")
OUTPUT_DIR = Path(r"C:\Users\imaha\source\repos\Comp307Game\Assets\Cutscene Videos")
OUTPUT_FILE = OUTPUT_DIR / "intro_cutscene.mp4"

# Verify inputs exist
if not FRAMES_DIR.exists():
    print(f"ERROR: Frames directory not found: {FRAMES_DIR}")
    sys.exit(1)

if not AUDIO_FILE.exists():
    print(f"ERROR: Audio file not found: {AUDIO_FILE}")
    sys.exit(1)

# Create output directory if needed
OUTPUT_DIR.mkdir(parents=True, exist_ok=True)

# Verify frames exist
frame_files = sorted([f for f in FRAMES_DIR.glob("f_*.png")])
if not frame_files:
    print(f"ERROR: No PNG frames found in {FRAMES_DIR}")
    sys.exit(1)

max_frame = max([int(f.stem.replace('f_', '')) for f in frame_files])
print(f"Found {len(frame_files)} frames (f_0001.png through f_{max_frame:04d}.png)")

if max_frame < 1590:
    print(f"WARNING: Expected 1590 frames, but only found {max_frame}")
    response = input("Continue anyway? (y/n): ")
    if response.lower() != 'y':
        sys.exit(1)

# Find FFmpeg
ffmpeg_paths = [
    r"C:\Program Files\ffmpeg\bin\ffmpeg.exe",
    r"C:\Program Files (x86)\ffmpeg\bin\ffmpeg.exe",
    "ffmpeg"  # Try system PATH
]

ffmpeg_exe = None
for path in ffmpeg_paths:
    try:
        result = subprocess.run([path, "-version"], capture_output=True, timeout=5)
        if result.returncode == 0:
            ffmpeg_exe = path
            break
    except:
        pass

if not ffmpeg_exe:
    print("ERROR: FFmpeg not found!")
    print("Please install FFmpeg from: https://ffmpeg.org/download.html")
    print("Or update the ffmpeg_paths in this script.")
    sys.exit(1)

print(f"Using FFmpeg: {ffmpeg_exe}")

# Build FFmpeg command
print("\n" + "=" * 60)
print("ENCODING TO MP4")
print("=" * 60)
print(f"Input frames: {FRAMES_DIR}\\f_*.png ({len(frame_files)} files)")
print(f"Input audio: {AUDIO_FILE}")
print(f"Output: {OUTPUT_FILE}")
print(f"Codec: H.264 (libx264)")
print(f"Quality: CRF 18 (high quality)")
print(f"Frame rate: 30 fps")
print("=" * 60)

cmd = [
    ffmpeg_exe,
    "-y",  # Overwrite output file
    "-framerate", "30",
    "-pattern_type", "glob",
    "-i", str(FRAMES_DIR / "f_*.png"),
    "-i", str(AUDIO_FILE),
    "-c:v", "libx264",  # H.264 video codec
    "-preset", "fast",  # Speed/quality tradeoff
    "-crf", "18",  # Quality (0=lossless, 23=default, 51=worst)
    "-c:a", "aac",  # AAC audio codec
    "-b:a", "192k",  # Audio bitrate
    "-shortest",  # Stop at shortest stream (audio)
    str(OUTPUT_FILE)
]

try:
    print("\nStarting encode... (this may take 2-5 minutes)")
    result = subprocess.run(cmd, text=True)

    if result.returncode == 0:
        if OUTPUT_FILE.exists():
            size_mb = OUTPUT_FILE.stat().st_size / (1024 * 1024)
            print(f"\n✓ Encode successful!")
            print(f"  Output: {OUTPUT_FILE}")
            print(f"  Size: {size_mb:.1f} MB")
        else:
            print(f"\n✗ Output file was not created")
            sys.exit(1)
    else:
        print(f"\n✗ Encode failed with return code {result.returncode}")
        sys.exit(1)

except KeyboardInterrupt:
    print("\n\nEncode cancelled by user")
    sys.exit(1)
except Exception as e:
    print(f"\n✗ Error: {e}")
    sys.exit(1)

print("\nNext step: Import intro_cutscene.mp4 into Unity under Assets/Cutscene Videos/")
print("Then configure CutsceneManager to play it.")