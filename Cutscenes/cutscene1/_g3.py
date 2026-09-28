
import bpy
sc=bpy.context.scene
sc.render.resolution_percentage=50
sc.render.image_settings.file_format='PNG'
for f in (1520,1560,1620,1660,1700,1740):
    sc.frame_set(f); sc.render.filepath=r"C:\Users\imaha\source\repos\Comp307Game\Cutscenes\cutscene1\_g3_%d.png" % f
    bpy.ops.render.render(write_still=True)
