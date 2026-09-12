
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
