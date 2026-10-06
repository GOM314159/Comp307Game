
import bpy
sc=bpy.context.scene
sc.render.engine='BLENDER_EEVEE'; sc.render.resolution_percentage=50
sc.frame_set(1)
for n in ("CAM_B1","CAM_B1_WIDE"):
    sc.camera=bpy.data.objects[n]
    sc.render.filepath=r"C:\Users\imaha\source\repos\Comp307Game\Cutscenes\cutscene2\_t_"+n+".png"
    bpy.ops.render.render(write_still=True)
