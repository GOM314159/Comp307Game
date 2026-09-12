#!/usr/bin/env python3
"""
Render frames 1471-1590 for intro cutscene
Run this script with: python render_frames_1471_1590.py
"""

import subprocess
import os
import sys

# Path to Blender executable - UPDATE THIS if different
BLENDER_EXE = r"C:\Program Files\Blender Foundation\Blender 5.2\blender.exe"

# Path to blend file
BLEND_FILE = r"C:\Users\imaha\source\repos\Comp307Game\Cutscenes\intro\intro_cutscene.blend"

# Check if Blender exists
if not os.path.exists(BLENDER_EXE):
    print(f"ERROR: Blender not found at {BLENDER_EXE}")
    print("Please update the BLENDER_EXE path in this script.")
    sys.exit(1)

# Check if blend file exists
if not os.path.exists(BLEND_FILE):
    print(f"ERROR: Blend file not found at {BLEND_FILE}")
    sys.exit(1)

# Python code to run in Blender
blender_script = r"""
import bpy

scene = bpy.context.scene
scene.render.use_overwrite = False
scene.frame_start = 1
scene.frame_end = 1590

print("=" * 60)
print("RENDERING FRAMES 1471-1590")
print("=" * 60)
print(f"Timeline: {scene.frame_start}-{scene.frame_end}")
print(f"Output: {scene.render.filepath}")
print(f"use_overwrite: {scene.render.use_overwrite}")
print(f"FPS: {scene.render.fps}")
print(f"Existing frames will be skipped, rendering only 1471-1590")
print("=" * 60)

bpy.ops.render.render(animation=True)

print("\nRender complete!")
"""

# Write script to temp file
script_path = r"C:\Users\imaha\source\repos\Comp307Game\Cutscenes\intro\render_script.py"
with open(script_path, 'w') as f:
    f.write(blender_script)

# Run Blender in background with the script
print(f"Starting Blender render process...")
print(f"Blender: {BLENDER_EXE}")
print(f"Blend file: {BLEND_FILE}")
print(f"This will take approximately 4-5 minutes...")
print()

cmd = [
    BLENDER_EXE,
    "--background",
    BLEND_FILE,
    "--python",
    script_path
]

try:
    result = subprocess.run(cmd, capture_output=False, text=True)
    if result.returncode == 0:
        print("\n✓ Render completed successfully!")
    else:
        print(f"\n✗ Render failed with return code {result.returncode}")
except Exception as e:
    print(f"\n✗ Error: {e}")
    sys.exit(1)

# Verify frames
frames_dir = r"C:\Users\imaha\source\repos\Comp307Game\Cutscenes\intro\frames"
if os.path.exists(frames_dir):
    frame_files = sorted([f for f in os.listdir(frames_dir) if f.startswith('f_') and f.endswith('.png')])
    if frame_files:
        max_frame = max([int(f.replace('f_', '').replace('.png', '')) for f in frame_files])
        print(f"\nFrame verification:")
        print(f"  Total frames: {len(frame_files)}")
        print(f"  Highest frame: {max_frame}")
        if max_frame >= 1590:
            print(f"  ✓ All 1590 frames rendered successfully!")
        else:
            print(f"  ⚠ Expected 1590 frames, but found {max_frame}")
