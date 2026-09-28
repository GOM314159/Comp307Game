
import bpy
sc=bpy.context.scene
sc.render.resolution_percentage=50
sc.render.image_settings.file_format='PNG'
for f in (1420,1500,1545,1590,1635,1680,1725,1765):
    sc.frame_set(f); sc.render.filepath=r"C:\Users\imaha\source\repos\Comp307Game\Cutscenes\cutscene1\_v_%d.png" % f
    bpy.ops.render.render(write_still=True)
