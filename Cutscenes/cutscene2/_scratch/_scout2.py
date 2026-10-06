
import bpy
sc = bpy.context.scene
sc.render.engine = 'BLENDER_WORKBENCH'
sc.render.resolution_percentage = 45
sc.display.shading.light = 'STUDIO'
sc.display.shading.color_type = 'MATERIAL'
for n in ("SCOUT_TOP","SCOUT_SEAT","SCOUT_STAGE"):
    sc.camera = bpy.data.objects[n]
    sc.render.filepath = r"C:\Users\imaha\source\repos\Comp307Game\Cutscenes\cutscene2\_s2_" + n + ".png"
    bpy.ops.render.render(write_still=True)
