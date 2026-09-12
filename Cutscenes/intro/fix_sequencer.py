
import bpy
import os

# Open the blend file
blend_path = r'C:\Users\imaha\source\repos\Comp307Game\Cutscenes\intro\intro_cutscene.blend'
bpy.ops.wm.open_mainfile(filepath=blend_path)

edit_scene = bpy.data.scenes.get('EDIT')
if edit_scene:
    bpy.context.window.scene = edit_scene
    sequencer = edit_scene.sequence_editor
    
    # Find and remove SHOTS
    for strip in list(sequencer.strips):
        if strip.name == 'SHOTS':
            bpy.context.scene.sequence_editor.sequences.remove(strip)
            print("Deleted SHOTS")
            break
    
    # Add image sequence
    frames_path = r'C:\Users\imaha\source\repos\Comp307Game\Cutscenes\intro\frames'
    first_image = os.path.join(frames_path, "f_0001.png")
    
    new_strip = sequencer.sequences.new_image(
        name="FRAMES",
        filepath=first_image,
        channel=1,
        frame_start=1
    )
    print(f"Added FRAMES with {len(new_strip.elements)} elements")

# Save
bpy.ops.wm.save_mainfile()
print("Saved")
