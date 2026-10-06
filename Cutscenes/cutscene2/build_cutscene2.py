"""Finishes cutscene2.blend: assets, cameras, lighting, animation.

Run against the existing file, which already holds the theatre, Vashin,
the chandelier and the rafter limb:

    blender --background Cutscenes/cutscene2/cutscene2.blend ^
            --python Cutscenes/cutscene2/build_cutscene2.py

Idempotent - it removes and rebuilds anything it owns, so run it as
many times as you like while tuning.
"""
import bpy, bmesh, math, mathutils, os, re

SC   = bpy.context.scene
HERE = os.path.dirname(os.path.abspath(bpy.data.filepath))

# =====================================================================
# THE BEATS
# =====================================================================
B1, B2, B3, B4, B5, END = 1, 421, 721, 1081, 1351, 1830

# Where things are, all measured from the imported set rather than
# assumed: stage x -17..-9.5, house x -9.5..-5, floor z -0.2,
# ceiling z 9, great curtain upstage at far -X.
STAGE_Z   = -0.2
VASHIN_AT = (-13.5, 57.0)
CHAND_AT  = (-13.5, 57.0)
CHAND_TOP = 7.8
CHAND_BOT = 6.2

# Camera convention, verified by eye not assumed:
#   euler (90,0,yaw):  yaw=+90 -> -X,  yaw=-90 -> +X
#                      yaw=  0 -> +Y,  yaw=180 -> -Y
# Everything below uses look_at() instead, which sidesteps it.


def look_at(cam, eye, target):
    eye = mathutils.Vector(eye); target = mathutils.Vector(target)
    cam.location = eye
    cam.rotation_euler = (eye - target).to_track_quat('Z', 'Y').to_euler()


def purge(prefix):
    for o in [o for o in bpy.data.objects if o.name.startswith(prefix)]:
        bpy.data.objects.remove(o, do_unlink=True)


def emat(name, colour, strength, rough=0.5):
    m = bpy.data.materials.get(name)
    if m:
        return m
    m = bpy.data.materials.new(name); m.use_nodes = True
    nt = m.node_tree
    for n in list(nt.nodes):
        if n.type != 'OUTPUT_MATERIAL':
            nt.nodes.remove(n)
    b = nt.nodes.new("ShaderNodeBsdfPrincipled")
    b.inputs['Base Color'].default_value = (*colour, 1)
    b.inputs['Roughness'].default_value = rough
    if 'Emission Color' in b.inputs:
        b.inputs['Emission Color'].default_value = (*colour, 1)
        b.inputs['Emission Strength'].default_value = strength
    nt.links.new(b.outputs[0], nt.nodes["Material Output"].inputs[0])
    return m


def mesh_obj(name, bm, mat, parent=None):
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me); bm.free()
    o = bpy.data.objects.new(name, me)
    SC.collection.objects.link(o)
    o.data.materials.append(mat)
    if parent:
        o.parent = parent
    return o


def empty(name):
    o = bpy.data.objects.get(name)
    if o is None:
        o = bpy.data.objects.new(name, None)
        SC.collection.objects.link(o)
    o.empty_display_size = 0.1
    return o


M_FACE  = emat("M_MaskFace",  (0.92, 0.91, 0.88), 0.55, 0.42)
M_TEAR  = emat("M_MaskTear",  (0.42, 0.10, 0.62), 2.6,  0.35)
M_SMILE = emat("M_MaskSmile", (0.70, 0.04, 0.06), 3.2,  0.30)
M_EYE   = emat("M_MaskEye",   (0.02, 0.02, 0.02), 0.0,  0.60)
M_METAL = emat("M_Cutters",   (0.09, 0.09, 0.11), 0.0,  0.28)


# =====================================================================
# THE MASK
# =====================================================================
# Built in the XZ plane facing -Y, then aimed wherever it is needed.
# This is the antagonist's face for the rest of the game, so it is
# modelled rather than faked with a texture card: it has to hold up
# when it comes out of the dark in beat 3 and again in beat 5.

def build_mask():
    purge("MASK")
    mask = empty("MASK")
    H = 0.22                                     # mask height, metres

    bm = bmesh.new()
    bmesh.ops.create_uvsphere(bm, u_segments=28, v_segments=20, radius=1.0)
    bmesh.ops.scale(bm, vec=(H*0.36, H*0.17, H*0.5), verts=bm.verts)
    mesh_obj("MASK_FACE", bm, M_FACE, mask)

    for i, sx in enumerate((-1, 1)):
        bm = bmesh.new()
        bmesh.ops.create_uvsphere(bm, u_segments=14, v_segments=10, radius=1.0)
        bmesh.ops.scale(bm, vec=(H*0.085, H*0.05, H*0.11), verts=bm.verts)
        e = mesh_obj("MASK_EYE_%d" % i, bm, M_EYE, mask)
        e.location = (sx*H*0.155, -H*0.10, H*0.10)

    # Tear tracks run from under each eye straight down. Self-lit,
    # because in beat 3 they are most of what survives the darkness.
    for i, sx in enumerate((-1, 1)):
        bm = bmesh.new()
        bmesh.ops.create_cone(bm, cap_ends=True, cap_tris=False, segments=8,
                              radius1=H*0.028, radius2=H*0.016, depth=H*0.40)
        t = mesh_obj("MASK_TEAR_%d" % i, bm, M_TEAR, mask)
        t.location = (sx*H*0.155, -H*0.125, -H*0.12)

    # The smile. bmesh has no torus primitive, so sweep the lower arc
    # by hand - a grin has to be an arc, not a ring, or it reads as a
    # circle painted on a face.
    MAJ, MIN, ARC, TUBE = H*0.26, H*0.022, 20, 8
    bm = bmesh.new(); rings = []
    for i in range(ARC + 1):
        a = math.radians(200 + 140.0 * i / ARC)
        c = mathutils.Vector((MAJ*math.cos(a), 0, MAJ*math.sin(a)))
        n = mathutils.Vector((math.cos(a), 0, math.sin(a)))
        ring = []
        for j in range(TUBE):
            b = 2*math.pi*j/TUBE
            ring.append(bm.verts.new(
                c + n*(MIN*math.cos(b))
                  + mathutils.Vector((0, 1, 0))*(MIN*math.sin(b))))
        rings.append(ring)
    bm.verts.ensure_lookup_table()
    for i in range(len(rings)-1):
        for j in range(TUBE):
            k = (j+1) % TUBE
            bm.faces.new((rings[i][j], rings[i][k],
                          rings[i+1][k], rings[i+1][j]))
    s = mesh_obj("MASK_SMILE", bm, M_SMILE, mask)
    s.location = (0, -H*0.115, H*0.07)
    return mask


# =====================================================================
# WIRE CUTTERS AND THE ARMS THAT HOLD THEM
# =====================================================================

def box(name, size, loc, rot, mat, parent):
    bm = bmesh.new()
    bmesh.ops.create_cube(bm, size=1.0)        # spans -0.5..0.5
    bmesh.ops.scale(bm, vec=size, verts=bm.verts)   # so scale by FULL extent
    o = mesh_obj(name, bm, mat, parent)
    o.location = loc; o.rotation_euler = rot
    return o


def build_cutters():
    purge("CUT")
    cut = empty("CUTTERS")
    box("CUT_HANDLE_A", (0.030, 0.030, 0.26), (0, 0, -0.13),
        (0, math.radians(7), 0), M_METAL, cut)
    box("CUT_HANDLE_B", (0.030, 0.030, 0.26), (0, 0, -0.13),
        (0, math.radians(-7), 0), M_METAL, cut)
    box("CUT_JAW_A", (0.016, 0.052, 0.085), (0.012, 0, 0.045),
        (0, math.radians(-16), 0), M_METAL, cut)
    box("CUT_JAW_B", (0.016, 0.052, 0.085), (-0.012, 0, 0.045),
        (0, math.radians(16), 0), M_METAL, cut)
    return cut


def build_arms():
    purge("PARM")
    stripe = bpy.data.materials.get("M_PuppetStripe")
    arms = empty("PUPPET_ARMS")
    for i, sx in enumerate((-1, 1)):
        bm = bmesh.new()
        bmesh.ops.create_cone(bm, cap_ends=True, cap_tris=False, segments=12,
                              radius1=0.032, radius2=0.026, depth=1.5)
        a = mesh_obj("PARM_%d" % i, bm, stripe or M_METAL, arms)
        a.location = (sx*0.14, 0.42, 0.0)
        a.rotation_euler = (math.radians(sx*-26), 0, 0)
    return arms


# =====================================================================
# CAMERAS
# =====================================================================

def build_cameras():
    purge("CAM_B")
    made = {}

    def cam(name, lens, eye, target, dof_at=None, fstop=2.2):
        cd = bpy.data.cameras.new(name)
        cd.lens = lens; cd.clip_start = 0.03; cd.clip_end = 400
        o = bpy.data.objects.new(name, cd)
        SC.collection.objects.link(o)
        look_at(o, eye, target)
        if dof_at is not None:
            cd.dof.use_dof = True
            cd.dof.focus_distance = (mathutils.Vector(eye)
                                     - mathutils.Vector(dof_at)).length
            cd.dof.aperture_fstop = fstop
        made[name] = o
        return o

    vx, vy = VASHIN_AT
    # 1 - from the house. He is small in a space far too big for him.
    cam("CAM_B1", 28, (-7.4, 57.4, 2.6), (vx, vy, 1.6),
        dof_at=(vx, vy, 1.2), fstop=2.4)
    # 2 - his eyeline, out into nothing.
    cam("CAM_B2", 28, (vx + 0.25, vy, 1.56), (-4.0, vy, 1.4))
    # 3 - closer, so the mask at the frame edge has somewhere to hide.
    cam("CAM_B3", 55, (-11.3, 57.9, 1.72), (vx, vy, 1.45),
        dof_at=(vx, vy, 1.45), fstop=2.0)
    # 4 - low, straight up the cable.
    cam("CAM_B4", 85, (vx + 0.1, vy + 1.25, 0.45),
        (CHAND_AT[0], CHAND_AT[1], CHAND_TOP + 0.6),
        dof_at=(CHAND_AT[0], CHAND_AT[1], CHAND_TOP), fstop=2.8)
    # 5 - static. Nothing follows the puppet out; it just leaves.
    cam("CAM_B5", 35, (-8.6, 57.7, 2.15), (vx, vy, 0.55),
        dof_at=(vx, vy, 0.6), fstop=2.6)
    return made


# =====================================================================
# MARKERS
# =====================================================================
# Camera-bound markers override scene.camera, which is what makes a
# single timeline cut between beats.

def bind_markers(cams):
    for m in list(SC.timeline_markers):
        SC.timeline_markers.remove(m)
    for i, (f, n) in enumerate(((B1, "CAM_B1"), (B2, "CAM_B2"),
                                (B3, "CAM_B3"), (B4, "CAM_B4"),
                                (B5, "CAM_B5"))):
        mk = SC.timeline_markers.new("BEAT%d" % (i+1), frame=f)
        mk.camera = cams[n]


# =====================================================================
# KEYING HELPERS
# =====================================================================
# Cutscene 1 lost a whole take to this: a channel keyed at only one
# frame extrapolates that value across the entire timeline. Every
# animated channel below is keyed at frame 1 and at every beat
# boundary, whether or not it changes there.

BEATS = (B1, B2, B3, B4, B5, END)


def key_loc(o, frame, loc):
    o.location = loc
    o.keyframe_insert("location", frame=frame)


def key_rot(o, frame, rot):
    o.rotation_euler = rot
    o.keyframe_insert("rotation_euler", frame=frame)


def fcurves_of(o):
    """Blender 5 slotted actions: walk layers/strips/channelbags."""
    ad = o.animation_data
    if not ad or not ad.action:
        return []
    act = ad.action
    out = []
    for layer in act.layers:
        for strip in layer.strips:
            for cb in strip.channelbags:
                out.extend(cb.fcurves)
    return out


def key_vis(o, frame, visible):
    o.hide_render = not visible
    o.hide_viewport = not visible
    o.keyframe_insert("hide_render", frame=frame)
    o.keyframe_insert("hide_viewport", frame=frame)
    for d in fcurves_of(o):
        if d.data_path in ("hide_render", "hide_viewport"):
            for k in d.keyframe_points:
                k.interpolation = 'CONSTANT'


def hide_all_but(o, ranges):
    """Visible only inside the given (start, end) frame ranges.
    Hiding a parent does NOT hide children in a render, so this is
    called per object."""
    pts = [(B1, False)]
    for s, e in ranges:
        pts.append((s, True))
        pts.append((e + 1, False))
    for f, v in pts:
        key_vis(o, max(f, 1), v)


# =====================================================================
# BUILD
# =====================================================================

mask  = build_mask()
cut   = build_cutters()
arms  = build_arms()
cams  = build_cameras()
bind_markers(cams)

vx, vy = VASHIN_AT
vash   = bpy.data.objects["VASHIN"]
chand  = bpy.data.objects["CHANDELIER"]
limb   = bpy.data.objects.get("PUPPET_LIMB")

# ---- the rafter limb: beat 1 only, and it never moves ----
if limb:
    for o in [limb]:
        hide_all_but(o, [(B1, B2 - 1)])

# ---- the mask ----
# Beat 3: pinned to the extreme frame edge, barely separable from
# black. Beat 5: the smile crossing the back of the stage.
mask.scale = (1, 1, 1)
key_loc(mask, B1,     (-11.9, 59.35, 1.62))
key_rot(mask, B1,     (0, 0, math.radians(-118)))
key_loc(mask, B3,     (-11.9, 59.35, 1.62))
key_loc(mask, B3+180, (-11.95, 59.20, 1.60))
key_loc(mask, B4,     (-11.95, 59.20, 1.60))
# Beat 5: walks out upstage-left, body in shadow, only this visible.
key_loc(mask, B5 + 150, (-15.6, 54.6, 1.30))
key_rot(mask, B5 + 150, (0, 0, math.radians(-150)))
key_loc(mask, END,      (-16.9, 51.4, 1.30))
key_rot(mask, END,      (0, 0, math.radians(-150)))
for part in mask.children:
    hide_all_but(part, [(B3, B4 - 1), (B5 + 120, END)])

# ---- cutters and arms: beat 4 only ----
cut.location = (CHAND_AT[0] + 0.02, CHAND_AT[1] - 0.30, CHAND_TOP + 0.62)
cut.rotation_euler = (math.radians(96), 0, 0)
arms.location = (CHAND_AT[0], CHAND_AT[1] - 0.45, CHAND_TOP + 0.95)
key_rot(cut, B1, cut.rotation_euler)
key_rot(cut, B4, cut.rotation_euler)
# The bite: jaws close over four frames near the end of the beat.
key_rot(cut, B4 + 200, cut.rotation_euler)
for part in list(cut.children) + list(arms.children):
    hide_all_but(part, [(B4, B5 - 1)])

# ---- the chandelier ----
# Still, still, still, then gone. The drop is 12 frames.
FALL = B5 + 36
key_loc(chand, B1,  chand.location)
key_loc(chand, B4,  chand.location)
key_loc(chand, FALL, chand.location)
rest = (chand.location.x, chand.location.y,
        chand.location.z - (CHAND_BOT - STAGE_Z) - 0.15)
key_loc(chand, FALL + 12, rest)
key_loc(chand, END, rest)
for fc in fcurves_of(chand):
    if fc.data_path == "location":
        for k in fc.keyframe_points:
            k.interpolation = 'LINEAR'       # gravity does not ease

# ---- Vashin ----
# Beat 1 rehearsal: small weight shifts. Beat 3: he turns, in steps.
# Beat 5: he is simply not standing any more.
base_rot = list(vash.rotation_euler)
key_rot(vash, B1, base_rot)
key_rot(vash, B2, base_rot)
for i, yaw in enumerate((0, -26, 34, -14, 48, -40)):
    f = B3 + int(i * (B4 - B3) / 6.0)
    key_rot(vash, f, (base_rot[0], base_rot[1],
                      base_rot[2] + math.radians(yaw)))
key_rot(vash, B4, (base_rot[0], base_rot[1],
                   base_rot[2] + math.radians(-40)))
key_rot(vash, FALL, (base_rot[0], base_rot[1],
                     base_rot[2] + math.radians(-40)))
# Felled.
key_rot(vash, FALL + 10, (base_rot[0] + math.radians(-82),
                          base_rot[1], base_rot[2] + math.radians(-40)))
key_rot(vash, END, (base_rot[0] + math.radians(-82),
                    base_rot[1], base_rot[2] + math.radians(-40)))
vbase = list(vash.location)
key_loc(vash, B1, vbase)
key_loc(vash, FALL, vbase)
key_loc(vash, FALL + 10, (vbase[0], vbase[1], vbase[2] - 0.72))
key_loc(vash, END, (vbase[0], vbase[1], vbase[2] - 0.72))

# ---- lighting per beat ----
work  = bpy.data.objects["WORK_LIGHT"]
rim   = bpy.data.objects["STAGE_RIM"]
bounce= bpy.data.objects["STAGE_BOUNCE"]

def key_energy(lamp, frame, e):
    lamp.data.energy = e
    lamp.data.keyframe_insert("energy", frame=frame)

# Beat 2 looks out into the house, so the stage light must not spill
# into the lens; beat 4 is lit only by whatever reaches the ceiling.
for lamp, lvl in ((work, [(B1, 600), (B2, 420), (B3, 520),
                          (B4, 300), (B5, 240), (FALL, 240),
                          (FALL+12, 95), (END, 95)]),
                  (rim,  [(B1, 140), (B2, 90), (B3, 170),
                          (B4, 120), (B5, 110), (END, 110)]),
                  (bounce, [(B1, 5), (B2, 2), (B3, 6),
                            (B4, 4), (B5, 3), (END, 3)])):
    for f, e in lvl:
        key_energy(lamp, f, e)

# A breath of light in the rafters so the limb in beat 1 is barely
# separable from the dark rather than absent from it.
raft = bpy.data.objects.get("RAFTER_HINT")
if raft is None:
    ld = bpy.data.lights.new("RAFTER_HINT", 'AREA')
    raft = bpy.data.objects.new("RAFTER_HINT", ld)
    SC.collection.objects.link(raft)
raft.data.type = 'AREA'
raft.data.size = 5.0
raft.data.color = (0.55, 0.62, 0.95)
raft.location = (-15.0, 59.0, 8.2)
raft.rotation_euler = (math.radians(180), 0, 0)
for f, e in ((B1, 9.0), (B2, 2.0), (B3, 3.0),
             (B4, 14.0), (B5, 3.0), (END, 3.0)):
    key_energy(raft, f, e)

# =====================================================================
# FINALISE
# =====================================================================
# fps LAST. Every import_scene.fbx overwrites scene.render.fps with
# the rate baked into the file - this is what silently shipped
# cutscene 1 at 24fps when every shot was timed for 30.
SC.render.fps = 30
SC.render.fps_base = 1.0
SC.frame_start = B1
SC.frame_end = END
SC.render.engine = 'BLENDER_EEVEE'
SC.render.resolution_x = 1920
SC.render.resolution_y = 1080
SC.render.resolution_percentage = 100
SC.render.image_settings.file_format = 'PNG'
SC.render.filepath = os.path.join(HERE, "frames", "f_")
SC.render.use_overwrite = False            # resumable

os.makedirs(os.path.join(HERE, "frames"), exist_ok=True)
bpy.ops.wm.save_mainfile()

print("[cutscene2] fps=%d range=%d-%d markers=%d  mask=%d parts"
      % (SC.render.fps, SC.frame_start, SC.frame_end,
         len(SC.timeline_markers), len(mask.children)))
