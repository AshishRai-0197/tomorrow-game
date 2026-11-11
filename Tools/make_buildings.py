"""
Town buildings for Tomorrow, modelled headless in Blender (never touches an open file).

  /Applications/Blender.app/Contents/MacOS/Blender -b --factory-startup --python Tools/make_buildings.py

Every model is all-quad, low-poly, UV-unwrapped, uses the same flat "Prop_*" palette as the other
props, has applied transforms, its origin on the ground at its centre, and FACES -Y in Blender
(the front a player sees). Writes:
  Assets/Models/Props/<Name>.glb      ModelSources/Buildings/<Name>.blend
and prints a QA report (tris, ngons, triangles, non-manifold edges, loose verts, UVs, file size).

Round parts use a "quad disc" lathe: the cap is a square grid mapped onto a circle, so caps are
quads too (no triangle fans, no ngons).
"""
import bpy, bmesh, math, os
from mathutils import Matrix, Vector

ROOT = os.path.expanduser("~/Unity_Projects/Cozy_Characters")
OUT_GLB = os.path.join(ROOT, "Assets/Models/Props")
OUT_BLEND = os.path.join(ROOT, "ModelSources/Buildings")
os.makedirs(OUT_BLEND, exist_ok=True)


# ------------------------------------------------------------------ materials
def srgb(h):
    h = h.lstrip("#"); c = [int(h[i:i + 2], 16) / 255 for i in (0, 2, 4)]
    return tuple(((x + 0.055) / 1.055) ** 2.4 if x > 0.04045 else x / 12.92 for x in c) + (1,)


PALETTE = {
    "Prop_Plaster": ("#F1DDBA", 0.9, 0), "Prop_Timber": ("#6B4428", 0.8, 0), "Prop_WoodLight": ("#B07D4F", 0.75, 0),
    "Prop_SignWood": ("#8A5A35", 0.7, 0), "Prop_Stone": ("#CDBFAC", 0.95, 0), "Prop_StoneDark": ("#9C8F80", 0.95, 0),
    "Prop_StoneLight": ("#E6DCCB", 0.95, 0), "Prop_RoofRed": ("#B5523B", 0.8, 0), "Prop_TealDark": ("#2F6F6B", 0.7, 0),
    "Prop_WindowGlow": ("#FFD98A", 0.4, 1), "Prop_Brass": ("#D9A441", 0.35, 0.8), "Prop_Cream": ("#FFF4DC", 0.6, 0),
    "Prop_Ink": ("#3B2417", 0.6, 0), "Prop_AwningRed": ("#D9583F", 0.85, 0), "Prop_AwningCream": ("#F7EBD0", 0.85, 0),
    "Prop_AwningPink": ("#EF8FB0", 0.85, 0), "Prop_Iron": ("#2E2B2A", 0.5, 0.7), "Prop_Water": ("#69B6E0", 0.1, 0),
    "Prop_Leaf": ("#6DA24B", 0.8, 0), "Prop_FlowerPink": ("#F08CB0", 0.7, 0), "Prop_FlowerYellow": ("#F6CF4C", 0.7, 0),
    "Prop_Apple": ("#D6352B", 0.5, 0), "Prop_Orange": ("#F09A2E", 0.6, 0), "Prop_Pear": ("#B5C94A", 0.6, 0),
    "Prop_Door": ("#7A3F2A", 0.7, 0),
}
_mats = {}


def mat(name):
    if name in _mats: return _mats[name]
    hexcol, rough, extra = PALETTE[name]
    m = bpy.data.materials.new(name); m.use_nodes = True
    b = next(n for n in m.node_tree.nodes if n.type == "BSDF_PRINCIPLED")
    b.inputs["Base Color"].default_value = srgb(hexcol)
    b.inputs["Roughness"].default_value = rough
    if name == "Prop_WindowGlow":
        b.inputs["Emission Color"].default_value = srgb(hexcol); b.inputs["Emission Strength"].default_value = 1.2
    elif name in ("Prop_Brass", "Prop_Iron"):
        b.inputs["Metallic"].default_value = extra
    if name == "Prop_Water":
        b.inputs["Alpha"].default_value = 0.72
        try: m.surface_render_method = "BLENDED"
        except Exception: pass
        try: m.blend_method = "BLEND"
        except Exception: pass
    m.diffuse_color = srgb(hexcol)
    _mats[name] = m
    return m


# ------------------------------------------------------------------ parts
parts = []


def new_obj(name, bm, material, smooth=False):
    me = bpy.data.meshes.new(name); bm.to_mesh(me); bm.free()
    for p in me.polygons: p.use_smooth = smooth
    ob = bpy.data.objects.new(name, me); bpy.context.collection.objects.link(ob)
    ob.data.materials.append(mat(material))
    parts.append(ob)
    return ob


def box(name, x0, x1, y0, y1, z0, z1, material, rot=None, pivot=None):
    """Axis-aligned box from min/max corners, optionally rotated (Euler degrees) about pivot."""
    bm = bmesh.new()
    bmesh.ops.create_cube(bm, size=1.0)
    sx, sy, sz = x1 - x0, y1 - y0, z1 - z0
    m = Matrix.Translation(((x0 + x1) / 2, (y0 + y1) / 2, (z0 + z1) / 2)) @ Matrix.Diagonal((sx, sy, sz, 1))
    if rot:
        pv = Vector(pivot) if pivot else Vector(((x0 + x1) / 2, (y0 + y1) / 2, (z0 + z1) / 2))
        R = (Matrix.Rotation(math.radians(rot[2]), 4, "Z") @ Matrix.Rotation(math.radians(rot[1]), 4, "Y")
             @ Matrix.Rotation(math.radians(rot[0]), 4, "X"))
        m = Matrix.Translation(pv) @ R @ Matrix.Translation(-pv) @ m
    bmesh.ops.transform(bm, matrix=m, verts=bm.verts)
    return new_obj(name, bm, material)


def disc_grid(m):
    """(m+1)x(m+1) square grid mapped onto the unit disc. Returns points and the boundary ring (CCW)."""
    pts = {}
    for i in range(m + 1):
        for j in range(m + 1):
            u, v = 2 * i / m - 1, 2 * j / m - 1
            pts[(i, j)] = (u * math.sqrt(1 - v * v / 2), v * math.sqrt(1 - u * u / 2))
    ring = [(i, 0) for i in range(m)] + [(m, j) for j in range(m)] + [(i, m) for i in range(m, 0, -1)] + [(0, j) for j in range(m, 0, -1)]
    return pts, ring


def lathe(name, profile, material, m=4, at=(0, 0, 0), rot=None, smooth=True, cap0=True, cap1=True, squash=(1, 1)):
    """Revolve [(radius, z), ...] around Z with 4*m segments; quad-grid caps on the ends."""
    pts, ring = disc_grid(m)
    bm = bmesh.new()
    rings = []
    for r, z in profile:
        rings.append([bm.verts.new((pts[k][0] * r * squash[0], pts[k][1] * r * squash[1], z)) for k in ring])
    n = len(ring)
    for a, b in zip(rings, rings[1:]):
        for k in range(n):
            bm.faces.new((a[k], a[(k + 1) % n], b[(k + 1) % n], b[k]))

    def cap(ring_verts, r, z):
        idx = {k: ring_verts[i] for i, k in enumerate(ring)}
        for i in range(1, m):
            for j in range(1, m):
                idx[(i, j)] = bm.verts.new((pts[(i, j)][0] * r * squash[0], pts[(i, j)][1] * r * squash[1], z))
        for i in range(m):
            for j in range(m):
                bm.faces.new((idx[(i, j)], idx[(i + 1, j)], idx[(i + 1, j + 1)], idx[(i, j + 1)]))
    if cap0: cap(rings[0], *profile[0])
    if cap1: cap(rings[-1], *profile[-1])
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    M = Matrix.Translation(at)
    if rot:
        M = M @ (Matrix.Rotation(math.radians(rot[2]), 4, "Z") @ Matrix.Rotation(math.radians(rot[1]), 4, "Y")
                 @ Matrix.Rotation(math.radians(rot[0]), 4, "X"))
    bmesh.ops.transform(bm, matrix=M, verts=bm.verts)
    ob = new_obj(name, bm, material, smooth)
    return ob


def ball(name, r, at, material, m=2):
    """Low-poly quad sphere (lathe of a semicircle with a small flat top/bottom)."""
    prof = [(r * math.sin(a), -r * math.cos(a)) for a in (0.35, 0.9, 1.5708, 2.25, 2.79)]
    return lathe(name, prof, material, m, at)


# ------------------------------------------------------------------ finish / QA / export
def finish(name):
    global parts
    for ob in bpy.context.scene.objects: ob.select_set(False)
    for ob in parts: ob.select_set(True)
    bpy.context.view_layer.objects.active = parts[0]
    bpy.ops.object.join()
    ob = bpy.context.view_layer.objects.active
    ob.name = name; ob.data.name = name
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    # sharp edges by angle so flat faces stay crisp and round parts stay soft
    bm = bmesh.new(); bm.from_mesh(ob.data)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    for e in bm.edges:
        if len(e.link_faces) == 2 and e.calc_face_angle(0) > math.radians(35): e.smooth = False
    bm.to_mesh(ob.data); bm.free()
    # UVs: smart project, uniform texel density across the model
    bpy.ops.object.mode_set(mode="EDIT"); bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.uv.smart_project(angle_limit=math.radians(66), island_margin=0.02, scale_to_bounds=False)
    bpy.ops.object.mode_set(mode="OBJECT")
    qa(ob)
    glb = os.path.join(OUT_GLB, name + ".glb")
    bpy.ops.export_scene.gltf(filepath=glb, export_format="GLB", use_selection=True, export_apply=True, export_yup=True)
    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT_BLEND, name + ".blend"), copy=True)
    REPORT[-1]["kb"] = os.path.getsize(glb) / 1024
    bpy.data.objects.remove(ob)
    parts = []


REPORT = []


def qa(ob):
    me = ob.data
    bm = bmesh.new(); bm.from_mesh(me)
    tris = sum(len(f.verts) - 2 for f in bm.faces)
    ngons = sum(1 for f in bm.faces if len(f.verts) > 4)
    triangles = sum(1 for f in bm.faces if len(f.verts) == 3)
    nonman = sum(1 for e in bm.edges if not e.is_manifold)
    loose = sum(1 for v in bm.verts if not v.link_edges)
    zero = sum(1 for f in bm.faces if f.calc_area() < 1e-8)
    bm.free()
    d = ob.dimensions
    REPORT.append(dict(name=ob.name, faces=len(me.polygons), tris=tris, ngons=ngons, triangles=triangles, nonmanifold=nonman,
                       loose=loose, zero_area=zero, uv=len(me.uv_layers) > 0, size="%.2f x %.2f x %.2f m" % (d.x, d.y, d.z),
                       transforms=tuple(ob.scale) == (1, 1, 1) and tuple(ob.rotation_euler) == (0, 0, 0)))


# ================================================================== models
def workshop_front():
    """False-front facade for Grandma Ada's workshop: sits against the north wall, door gap 1.1 m."""
    W = 3.75
    # plaster wall with the door gap, raised parapet on top
    box("WallL", -W, -0.55, -0.14, 0, 0, 2.3, "Prop_Plaster")
    box("WallR", 0.55, W, -0.14, 0, 0, 2.3, "Prop_Plaster")
    box("Lintel", -0.55, 0.55, -0.14, 0, 2.05, 2.3, "Prop_Plaster")
    box("Parapet", -W, W, -0.14, 0, 2.3, 2.85, "Prop_Plaster")
    box("ParapetMid", -1.6, 1.6, -0.14, 0, 2.85, 3.3, "Prop_Plaster")
    # timber frame
    for sx in (-1, 1):
        box("Post", sx * 3.58 - 0.09, sx * 3.58 + 0.09, -0.22, 0.02, 0, 2.95, "Prop_Timber")
        box("CorniceSide", min(sx * 1.6, sx * 3.85), max(sx * 1.6, sx * 3.85), -0.26, 0.02, 2.85, 2.95, "Prop_Timber")
        box("CorniceStep", sx * 1.6 - 0.06, sx * 1.6 + 0.06, -0.26, 0.02, 2.95, 3.3, "Prop_Timber")
        # diagonal braces in the upper panels
        box("Brace", sx * 2.6 - 0.05, sx * 2.6 + 0.05, -0.2, -0.12, 2.32, 2.84, "Prop_Timber", rot=(0, sx * 35, 0))
    box("CorniceTop", -1.72, 1.72, -0.26, 0.02, 3.3, 3.42, "Prop_Timber")
    box("Beam", -W, W, -0.2, 0, 2.22, 2.32, "Prop_Timber")
    for sx in (-1, 1):
        x0, x1 = sorted((sx * 0.6, sx * 3.8))
        box("Plinth", x0, x1, -0.2, 0, 0, 0.35, "Prop_StoneDark")
    # door frame
    for sx in (-1, 1):
        box("DoorPost", min(sx * 0.55, sx * 0.68), max(sx * 0.55, sx * 0.68), -0.24, 0.02, 0, 2.1, "Prop_Timber")
    box("DoorHead", -0.72, 0.72, -0.26, 0.02, 2.02, 2.16, "Prop_Timber")
    # windows with shutters and flower boxes
    for cx in (-2.15, 2.15):
        box("Pane", cx - 0.45, cx + 0.45, -0.17, -0.14, 0.9, 1.6, "Prop_WindowGlow")
        box("FrameT", cx - 0.5, cx + 0.5, -0.23, -0.12, 1.58, 1.66, "Prop_Timber")
        box("FrameB", cx - 0.5, cx + 0.5, -0.23, -0.12, 0.84, 0.92, "Prop_Timber")
        box("FrameL", cx - 0.5, cx - 0.42, -0.23, -0.12, 0.92, 1.58, "Prop_Timber")
        box("FrameR", cx + 0.42, cx + 0.5, -0.23, -0.12, 0.92, 1.58, "Prop_Timber")
        box("MullionV", cx - 0.03, cx + 0.03, -0.21, -0.15, 0.92, 1.58, "Prop_Timber")
        box("MullionH", cx - 0.42, cx + 0.42, -0.21, -0.15, 1.22, 1.28, "Prop_Timber")
        box("Sill", cx - 0.6, cx + 0.6, -0.3, -0.12, 0.78, 0.86, "Prop_Timber")
        box("ShutterL", cx - 0.76, cx - 0.52, -0.2, -0.14, 0.9, 1.6, "Prop_TealDark")
        box("ShutterR", cx + 0.52, cx + 0.76, -0.2, -0.14, 0.9, 1.6, "Prop_TealDark")
        box("FlowerBox", cx - 0.52, cx + 0.52, -0.4, -0.14, 0.6, 0.78, "Prop_TealDark")
        box("Leaves", cx - 0.48, cx + 0.48, -0.36, -0.18, 0.78, 0.84, "Prop_Leaf")
        for k, fx in enumerate((-0.36, -0.18, 0.0, 0.18, 0.36)):
            ball("Flower", 0.065, (cx + fx, -0.27, 0.89), "Prop_FlowerPink" if k % 2 == 0 else "Prop_FlowerYellow", m=1)
    # sign board with a brass rim and a clock emblem
    box("Sign", -1.15, 1.15, -0.26, -0.14, 2.42, 3.08, "Prop_SignWood")
    box("SignRimT", -1.2, 1.2, -0.29, -0.14, 3.05, 3.12, "Prop_Brass")
    box("SignRimB", -1.2, 1.2, -0.29, -0.14, 2.38, 2.45, "Prop_Brass")
    for sx in (-1, 1):
        box("SignRimS", min(sx * 1.13, sx * 1.2), max(sx * 1.13, sx * 1.2), -0.29, -0.14, 2.45, 3.05, "Prop_Brass")
        # little cog-wheels either side of the clock
        lathe("Cog", [(0.12, 0), (0.12, 0.04)], "Prop_Brass", m=2, at=(sx * 0.62, -0.27, 2.75), rot=(90, 0, 0), smooth=False)
        for a in range(8):
            ang = a * math.pi / 4
            box("Tooth", sx * 0.62 - 0.025, sx * 0.62 + 0.025, -0.3, -0.27, 2.75 + 0.11, 2.75 + 0.16, "Prop_Brass",
                rot=(0, math.degrees(ang), 0), pivot=(sx * 0.62, -0.285, 2.75))
    lathe("ClockRim", [(0.26, 0), (0.26, 0.05)], "Prop_Brass", m=4, at=(0, -0.27, 2.75), rot=(90, 0, 0))
    lathe("ClockFace", [(0.21, 0), (0.21, 0.07)], "Prop_Cream", m=4, at=(0, -0.27, 2.75), rot=(90, 0, 0))
    box("HandH", -0.015, 0.015, -0.36, -0.34, 2.75, 2.87, "Prop_Ink", rot=(0, 60, 0), pivot=(0, -0.35, 2.75))
    box("HandM", -0.012, 0.012, -0.37, -0.35, 2.75, 2.92, "Prop_Ink", rot=(0, -10, 0), pivot=(0, -0.36, 2.75))
    # door canopy on two brackets
    box("Canopy", -0.95, 0.95, -0.85, 0.0, 2.3, 2.38, "Prop_RoofRed", rot=(-18, 0, 0), pivot=(0, 0, 2.38))
    for sx in (-1, 1):
        box("Bracket", sx * 0.8 - 0.04, sx * 0.8 + 0.04, -0.62, -0.14, 2.05, 2.13, "Prop_Timber", rot=(40, 0, 0), pivot=(sx * 0.8, -0.14, 2.09))
    finish("Workshop_Front")


def clock_tower():
    """Stone clock tower, 8.9 m. Clock centres at z 4.62 on all four sides; open belfry for the lamp (z 5.3-6.9)."""
    box("Plinth", -1.05, 1.05, -1.05, 1.05, 0, 0.4, "Prop_StoneDark")
    box("Shaft", -0.9, 0.9, -0.9, 0.9, 0.4, 4.0, "Prop_Stone")
    for sx in (-1, 1):
        for sy in (-1, 1):
            for k in range(7):
                z = 0.5 + k * 0.5
                w = 0.3 if k % 2 == 0 else 0.22
                box("Quoin", sx * 0.93 - w / 2 * (1 if sx < 0 else 1), sx * 0.93 + w / 2, sy * 0.93 - w / 2, sy * 0.93 + w / 2, z, z + 0.24, "Prop_StoneLight")
    box("String", -1.0, 1.0, -1.0, 1.0, 4.0, 4.12, "Prop_StoneDark")
    box("ClockStage", -0.95, 0.95, -0.95, 0.95, 4.12, 5.2, "Prop_StoneLight")
    # clocks on all four faces
    for yaw in (0, 90, 180, 270):
        R = Matrix.Rotation(math.radians(yaw), 4, "Z")
        def place(ob):
            ob.data.transform(R); return ob
        place(lathe("Rim", [(0.5, 0), (0.5, 0.06)], "Prop_Brass", m=4, at=(0, -0.95, 4.62), rot=(90, 0, 0)))
        place(lathe("Face", [(0.42, 0), (0.42, 0.09)], "Prop_Cream", m=4, at=(0, -0.95, 4.62), rot=(90, 0, 0)))
        for h in range(12):
            a = h * 30
            big = h % 3 == 0
            place(box("Tick", -0.02 if not big else -0.03, 0.02 if not big else 0.03, -1.06, -1.04, 4.62 + 0.3, 4.62 + (0.38 if big else 0.36), "Prop_Ink",
                      rot=(0, a, 0), pivot=(0, -1.05, 4.62)))
        place(box("HandHour", -0.025, 0.025, -1.08, -1.06, 4.62, 4.84, "Prop_Ink", rot=(0, 60, 0), pivot=(0, -1.07, 4.62)))
        place(box("HandMin", -0.018, 0.018, -1.09, -1.07, 4.62, 4.94, "Prop_Ink", rot=(0, -8, 0), pivot=(0, -1.08, 4.62)))
    box("Cornice", -1.03, 1.03, -1.03, 1.03, 5.2, 5.32, "Prop_StoneDark")
    # open belfry: corner pillars, low balustrade, lintels
    for sx in (-1, 1):
        for sy in (-1, 1):
            box("Pillar", sx * 0.9 - 0.16, sx * 0.9 + 0.16, sy * 0.9 - 0.16, sy * 0.9 + 0.16, 5.32, 6.9, "Prop_Stone")
    for (x0, x1, y0, y1) in ((-0.74, 0.74, -0.9, -0.78), (-0.74, 0.74, 0.78, 0.9), (-0.9, -0.78, -0.74, 0.74), (0.78, 0.9, -0.74, 0.74)):
        box("Balustrade", x0, x1, y0, y1, 5.32, 5.62, "Prop_StoneLight")
        box("Lintel", x0, x1, y0, y1, 6.62, 6.9, "Prop_Stone")
    box("BelfryFloor", -0.78, 0.78, -0.78, 0.78, 5.32, 5.38, "Prop_StoneDark")
    box("TopCornice", -1.08, 1.08, -1.08, 1.08, 6.9, 7.06, "Prop_StoneDark")
    # square pyramid roof (4 segments: m=1), brass finial
    s2 = math.sqrt(2)
    lathe("Roof", [(1.12 * s2, 7.06), (1.12 * s2, 7.14), (0.1 * s2, 8.55)], "Prop_RoofRed", m=1, smooth=False)
    lathe("Finial", [(0.05, 8.55), (0.05, 8.85)], "Prop_Brass", m=2)
    ball("FinialBall", 0.1, (0, 0, 8.93), "Prop_Brass", m=2)
    # front door, step and slit windows
    box("DoorFrame", -0.48, 0.48, -1.1, -1.04, 0, 1.68, "Prop_StoneDark")
    box("Door", -0.36, 0.36, -1.13, -1.08, 0.0, 1.55, "Prop_Door")
    box("DoorBand", -0.36, 0.36, -1.15, -1.12, 0.45, 0.52, "Prop_Iron")
    box("DoorBand2", -0.36, 0.36, -1.15, -1.12, 1.1, 1.17, "Prop_Iron")
    box("Step", -0.6, 0.6, -1.45, -1.05, 0, 0.12, "Prop_StoneDark")
    for yaw in (0, 90, 270):
        R = Matrix.Rotation(math.radians(yaw), 4, "Z")
        ob = box("Slit", -0.1, 0.1, -0.93, -0.9, 2.4, 3.15, "Prop_Ink"); ob.data.transform(R)
        ob = box("SlitSill", -0.16, 0.16, -0.96, -0.9, 2.34, 2.4, "Prop_StoneLight"); ob.data.transform(R)
    finish("Clock_Tower")


def awning_frame(name, width, depth, z_front, z_back, stripes, colors, posts=True, post_back=False):
    """Striped canvas awning: front edge at y=-depth/2 (low), back edge at y=+depth/2 (high)."""
    tilt = math.degrees(math.atan2(z_back - z_front, depth))
    length = math.hypot(depth, z_back - z_front)
    sw = width / stripes
    zc = (z_front + z_back) / 2
    for i in range(stripes):
        x0 = -width / 2 + i * sw
        box("Stripe", x0, x0 + sw, -length / 2, length / 2, zc - 0.025, zc + 0.025, colors[i % 2], rot=(tilt, 0, 0), pivot=(0, 0, zc))
        # scalloped valance: alternating short flaps along the front edge
        box("Flap", x0 + 0.02, x0 + sw - 0.02, -depth / 2 - 0.03, -depth / 2 + 0.0, z_front - 0.22, z_front - 0.0, colors[i % 2])
    if posts:
        for sx in (-1, 1):
            lathe("PostFront", [(0.05, 0), (0.05, z_front)], "Prop_WoodLight", m=2, at=(sx * (width / 2 - 0.1), -depth / 2 + 0.08, 0))
            if post_back:
                lathe("PostBack", [(0.05, 0), (0.05, z_back)], "Prop_WoodLight", m=2, at=(sx * (width / 2 - 0.1), depth / 2 - 0.08, 0))
    box("FrontRail", -width / 2, width / 2, -depth / 2 - 0.0, -depth / 2 + 0.06, z_front - 0.04, z_front + 0.03, "Prop_WoodLight")
    box("BackRail", -width / 2, width / 2, depth / 2 - 0.06, depth / 2, z_back - 0.06, z_back + 0.02, "Prop_WoodLight")


def striped_awning():
    """Bakery awning: 3.6 m wide, 1.4 m deep, front edge 1.62 m, back edge 1.98 m (fixed to the house)."""
    awning_frame("Awning", 3.6, 1.4, 1.62, 1.98, 9, ("Prop_AwningRed", "Prop_AwningCream"))
    for sx in (-1, 1):  # wall brackets at the back
        box("WallBracket", sx * 1.7 - 0.04, sx * 1.7 + 0.04, 0.45, 0.7, 1.55, 1.95, "Prop_Timber", rot=(-35, 0, 0), pivot=(sx * 1.7, 0.7, 1.75))
    finish("Striped_Awning")


def market_stall():
    """Mira's market stall: 2.4 m counter (along X), customers stand at -Y. Counter top at 0.78 m."""
    L, D = 2.3, 0.72
    box("CounterTop", -L / 2 - 0.05, L / 2 + 0.05, -D / 2 - 0.05, D / 2, 0.74, 0.8, "Prop_WoodLight")
    box("CounterBody", -L / 2, L / 2, -D / 2, D / 2, 0.0, 0.74, "Prop_SignWood")
    for i in range(7):  # front planks
        x0 = -L / 2 + i * L / 7
        box("Plank", x0 + 0.015, x0 + L / 7 - 0.015, -D / 2 - 0.03, -D / 2, 0.08, 0.7, "Prop_WoodLight")
    box("Kick", -L / 2, L / 2, -D / 2 - 0.035, -D / 2, 0.0, 0.08, "Prop_Timber")
    # crates of produce on the counter
    for cx, fruit in ((-0.78, "Prop_Apple"), (0.78, "Prop_Orange")):   # middle stays free for the scale
        box("CrateBase", cx - 0.3, cx + 0.3, -0.28, 0.12, 0.8, 0.84, "Prop_Timber")
        for sy in (-1, 1):
            box("CrateSide", cx - 0.3, cx + 0.3, (-0.08 + sy * 0.2) - 0.02, (-0.08 + sy * 0.2) + 0.02, 0.84, 0.98, "Prop_WoodLight")
        for sx in (-1, 1):
            box("CrateEnd", cx + sx * 0.28 - 0.02, cx + sx * 0.28 + 0.02, -0.26, 0.1, 0.84, 0.98, "Prop_WoodLight")
        for i in range(3):
            for j in range(2):
                ball("Fruit", 0.075, (cx - 0.16 + i * 0.16, -0.16 + j * 0.16, 0.96), fruit, m=1)
    # price sign
    box("PriceBoard", -0.22, 0.22, -0.45, -0.42, 0.55, 0.75, "Prop_Ink", rot=(-10, 0, 0))
    # tall back posts + low front posts and a pink-striped roof
    awning_frame("Roof", 2.6, 1.3, 1.72, 2.05, 7, ("Prop_AwningPink", "Prop_AwningCream"), posts=True, post_back=True)
    finish("Market_Stall")


def fountain():
    """Town-square fountain: basin r 0.98, water at 0.3, centre bowl at 1.0 m."""
    lathe("Basin", [(0.92, 0), (0.92, 0.3), (0.98, 0.3), (0.98, 0.4), (0.84, 0.4), (0.84, 0.12)], "Prop_Stone", m=6)
    lathe("Water", [(0.835, 0.26), (0.835, 0.3)], "Prop_Water", m=6, smooth=False)
    lathe("Pillar", [(0.2, 0.12), (0.2, 0.22), (0.13, 0.28), (0.1, 0.78), (0.15, 0.84), (0.15, 0.9)], "Prop_StoneLight", m=4)
    lathe("Bowl", [(0.12, 0.88), (0.4, 0.98), (0.46, 1.06), (0.46, 1.1), (0.4, 1.1), (0.12, 1.01)], "Prop_Stone", m=6)
    lathe("BowlWater", [(0.395, 1.05), (0.395, 1.08)], "Prop_Water", m=6, smooth=False)
    lathe("Spout", [(0.06, 1.01), (0.045, 1.24), (0.09, 1.3), (0.06, 1.34)], "Prop_Brass", m=2)
    finish("Fountain")


def park_bench():
    """Chibi-scale park bench: 1.1 m long, seat at 0.24 m, back to 0.56 m. Faces -Y."""
    for i, y in enumerate((-0.12, -0.02, 0.08)):
        box("Slat", -0.55, 0.55, y - 0.045, y + 0.045, 0.22, 0.26, "Prop_WoodLight")
    for i, z in enumerate((0.36, 0.48)):
        box("BackSlat", -0.55, 0.55, 0.15, 0.19, z - 0.045, z + 0.045, "Prop_WoodLight", rot=(-12, 0, 0), pivot=(0, 0.13, 0.26))
    for sx in (-1, 1):
        x = sx * 0.46
        box("LegFront", x - 0.025, x + 0.025, -0.16, -0.11, 0, 0.22, "Prop_Iron")
        box("LegBack", x - 0.025, x + 0.025, 0.12, 0.17, 0, 0.58, "Prop_Iron", rot=(-12, 0, 0), pivot=(x, 0.14, 0.24))
        box("Rail", x - 0.025, x + 0.025, -0.16, 0.16, 0.19, 0.22, "Prop_Iron")
        box("Arm", x - 0.03, x + 0.03, -0.18, 0.12, 0.34, 0.37, "Prop_Iron")
        box("ArmPost", x - 0.02, x + 0.02, -0.16, -0.12, 0.26, 0.34, "Prop_Iron")
    finish("Park_Bench")


bpy.ops.wm.read_factory_settings(use_empty=True)
for f in (workshop_front, clock_tower, striped_awning, market_stall, fountain, park_bench):
    f()

print("\nQA REPORT")
print("%-16s %6s %6s %6s %5s %8s %6s %5s %4s %-22s %6s" % ("model", "faces", "tris", "ngons", "tri", "nonmani", "loose", "zero", "uv", "size", "KB"))
for r in REPORT:
    print("%-16s %6d %6d %6d %5d %8d %6d %5d %4s %-22s %6.1f" % (r["name"], r["faces"], r["tris"], r["ngons"], r["triangles"], r["nonmanifold"],
                                                               r["loose"], r["zero_area"], "yes" if r["uv"] else "NO", r["size"], r["kb"]))
print("BUILDINGS DONE")
