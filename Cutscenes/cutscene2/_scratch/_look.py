
import bpy
sc = bpy.context.scene
sc.render.engine = 'BLENDER_WORKBENCH'
sc.render.resolution_percentage = 40
sc.display.shading.light = 'STUDIO'
sc.display.shading.color_type = 'MATERIAL'
for k in ("PX","NX","PY","NY"):
    sc.camera = bpy.data.objects["LOOK_"+k]
    sc.render.filepath = r"C:\Users\imaha\source\repos\Comp307Game\Cutscenes\cutscene2\_look_" + k + ".png"
    bpy.ops.render.render(write_still=True)
