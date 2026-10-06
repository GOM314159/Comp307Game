
import bpy
sc = bpy.context.scene
sc.render.engine = 'BLENDER_WORKBENCH'
sc.render.resolution_percentage = 45
sc.render.image_settings.file_format = 'PNG'
sc.display.shading.light = 'STUDIO'
sc.display.shading.color_type = 'MATERIAL'
for n in ("SCOUT_TOP","SCOUT_X","SCOUT_X2"):
    sc.camera = bpy.data.objects[n]
    sc.render.filepath = r"C:\Users\imaha\source\repos\Comp307Game\Cutscenes\cutscene2\_scout_" + n + ".png"
    bpy.ops.render.render(write_still=True)
