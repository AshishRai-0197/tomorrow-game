"""
Tomorrow logo + app icon, rendered headless in Blender (never touches an open file).

  /Applications/Blender.app/Contents/MacOS/Blender -b --factory-startup --python Tools/make_logo.py

Outputs:
  Assets/Game/Resources/UI/Logo.png   (1400x560, transparent) - title screen
  Assets/Game/Art/Icon.png            (1024x1024)             - Android / app icon
  ModelSources/Logo.blend
"""
import bpy, math, os

ROOT = os.path.abspath(os.path.join(os.path.dirname(bpy.data.filepath or __file__), ".."))
if not os.path.isdir(os.path.join(ROOT, "Assets")):
    ROOT = os.path.expanduser("~/Unity_Projects/Cozy_Characters")
FONT = os.path.join(ROOT, "Assets/Game/Resources/Fonts/Fredoka.ttf")


def srgb(h):
    h = h.lstrip("#"); c = [int(h[i:i + 2], 16) / 255 for i in (0, 2, 4)]
    return tuple(((x + 0.055) / 1.055) ** 2.4 if x > 0.04045 else x / 12.92 for x in c) + (1,)


def flat(name, hexcol, alpha=1.0):
    m = bpy.data.materials.new(name); m.use_nodes = True
    nt = m.node_tree; nt.nodes.clear()
    out = nt.nodes.new("ShaderNodeOutputMaterial"); em = nt.nodes.new("ShaderNodeEmission")
    em.inputs["Color"].default_value = srgb(hexcol)
    if alpha < 1:
        tr = nt.nodes.new("ShaderNodeBsdfTransparent"); mix = nt.nodes.new("ShaderNodeMixShader")
        mix.inputs[0].default_value = alpha
        nt.links.new(tr.outputs[0], mix.inputs[1]); nt.links.new(em.outputs[0], mix.inputs[2])
        nt.links.new(mix.outputs[0], out.inputs[0])
        m.blend_method = "BLEND" if hasattr(m, "blend_method") else None
    else:
        nt.links.new(em.outputs[0], out.inputs[0])
    return m


def gradient(name, top, bottom, axis="Y"):
    m = bpy.data.materials.new(name); m.use_nodes = True
    nt = m.node_tree; nt.nodes.clear()
    out = nt.nodes.new("ShaderNodeOutputMaterial"); em = nt.nodes.new("ShaderNodeEmission")
    tc = nt.nodes.new("ShaderNodeTexCoord"); sep = nt.nodes.new("ShaderNodeSeparateXYZ")
    ramp = nt.nodes.new("ShaderNodeValToRGB")
    ramp.color_ramp.elements[0].color = srgb(bottom); ramp.color_ramp.elements[1].color = srgb(top)
    ramp.color_ramp.elements[0].position = 0.15; ramp.color_ramp.elements[1].position = 0.85
    nt.links.new(tc.outputs["Generated"], sep.inputs[0]); nt.links.new(sep.outputs[axis], ramp.inputs[0])
    nt.links.new(ramp.outputs[0], em.inputs["Color"]); nt.links.new(em.outputs[0], out.inputs[0])
    return m


def reset():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    sc = bpy.context.scene
    for eng in ("BLENDER_EEVEE", "BLENDER_EEVEE_NEXT", "CYCLES"):
        try: sc.render.engine = eng; break
        except TypeError: pass
    if sc.render.engine == "CYCLES": sc.cycles.samples = 32
    sc.render.film_transparent = True
    sc.view_settings.view_transform = "Standard"
    sc.render.image_settings.file_format = "PNG"
    sc.render.image_settings.color_mode = "RGBA"
    return sc


def text(body, size, z, mat, offset=0.0, extrude=0.0, pos=(0, 0), font=None):
    cu = bpy.data.curves.new("T", "FONT"); cu.body = body; cu.size = size
    cu.align_x = "CENTER"; cu.align_y = "CENTER"
    if font: cu.font = font
    cu.offset = offset; cu.extrude = extrude
    ob = bpy.data.objects.new("T", cu); bpy.context.collection.objects.link(ob)
    ob.location = (pos[0], pos[1], z); ob.data.materials.append(mat)
    return ob


def disc(name, r, z, mat, pos=(0, 0), verts=64):
    bpy.ops.mesh.primitive_circle_add(vertices=verts, radius=r, fill_type="NGON", location=(pos[0], pos[1], z))
    ob = bpy.context.active_object; ob.name = name; ob.data.materials.append(mat); return ob


def bar(name, w, h, z, mat, pos, angle):
    bpy.ops.mesh.primitive_plane_add(size=1, location=(pos[0], pos[1], z))
    ob = bpy.context.active_object; ob.name = name
    ob.scale = (w, h, 1); ob.rotation_euler = (0, 0, angle)
    # pivot at one end so it reads like a clock hand
    ob.location = (pos[0] + math.cos(angle + math.pi / 2) * h / 2, pos[1] + math.sin(angle + math.pi / 2) * h / 2, z)
    ob.data.materials.append(mat); return ob


def clock(center, r, z, rim_hex="#4A2A18"):
    """A little brass clock with a tomorrow-blue glow ring behind it."""
    disc("Glow", r * 1.32, z - 0.03, flat("Glow", "#BFE3FF"), center)
    disc("Rim", r * 1.1, z - 0.02, flat("ClockRim", rim_hex), center)
    disc("Brass", r * 1.0, z - 0.01, flat("Brass", "#E3A64A"), center)
    disc("Face", r * 0.82, z, flat("Face", "#FFF4DC"), center)
    ink = flat("Ink", "#4A2A18")
    for i in range(12):
        a = i * math.pi / 6
        big = i % 3 == 0
        bpy.ops.mesh.primitive_circle_add(vertices=16, radius=r * (0.07 if big else 0.04), fill_type="NGON",
                                          location=(center[0] + math.cos(a) * r * 0.66, center[1] + math.sin(a) * r * 0.66, z + 0.01))
        bpy.context.active_object.data.materials.append(ink)
    bar("Hour", r * 0.11, r * 0.42, z + 0.02, ink, center, math.radians(-60))   # ~ 2 o'clock... ticking toward tomorrow
    bar("Minute", r * 0.08, r * 0.6, z + 0.02, flat("Hand", "#E8743B"), center, math.radians(-5))
    disc("Pin", r * 0.09, z + 0.03, ink, center, 24)


def camera(sc, w, h, ortho):
    sc.render.resolution_x, sc.render.resolution_y = w, h
    cam = bpy.data.objects.new("Cam", bpy.data.cameras.new("Cam"))
    cam.data.type = "ORTHO"; cam.data.ortho_scale = ortho
    cam.location = (0, 0, 10); bpy.context.collection.objects.link(cam); sc.camera = cam


# ------------------------------------------------------------------ logo
# Blender's text offset makes spikes at sharp joins, so the word is rendered once as a clean
# mask and the bold face, cream rim, brown outline and drop shadow are grown in image space.
import numpy as np

W, H, SS = 1400, 560, 2          # final size, supersampling


def render_layer(build, path):
    sc = reset(); build(); camera(sc, W * SS, H * SS, 11.0)
    sc.render.filepath = path; bpy.ops.render.render(write_still=True)
    img = bpy.data.images.load(path)
    a = np.array(img.pixels[:], dtype=np.float32).reshape(H * SS, W * SS, 4)
    bpy.data.images.remove(img); os.remove(path)
    return a


def dilate(a, r):
    out = a.copy()
    for dy in range(-r, r + 1):
        for dx in range(-r, r + 1):
            if dx * dx + dy * dy <= r * r:
                out = np.maximum(out, np.roll(np.roll(a, dy, 0), dx, 1))
    return out


def over(dst, rgb, alpha):
    alpha = alpha[..., None]
    dst[..., :3] = rgb * alpha + dst[..., :3] * (1 - alpha)
    dst[..., 3:] = alpha + dst[..., 3:] * (1 - alpha)


def lin(hexcol):  # 8-bit PNG pixels in bpy are display (sRGB) values, not linear
    h = hexcol.lstrip("#"); return np.array([int(h[i:i + 2], 16) / 255 for i in (0, 2, 4)], dtype=np.float32)


tmp = os.path.join(ROOT, "Temp"); os.makedirs(tmp, exist_ok=True)
word_pos = (-0.55, -0.15)
mask = render_layer(lambda: text("Tomorrow", 2.15, 0, flat("White", "#FFFFFF"), font=bpy.data.fonts.load(FONT), pos=word_pos),
                    os.path.join(tmp, "logo_mask.png"))[..., 3]
clk = render_layer(lambda: clock((4.05, 0.75), 0.62, 0.2), os.path.join(tmp, "logo_clock.png"))

bold = dilate(mask, 7)
rim = dilate(bold, 6)
outline = dilate(bold, 26)
shadow = np.roll(np.roll(outline, -18, 0), 10, 1) * 0.4
ys = np.where(bold.max(1) > 0.5)[0]
top, bot = ys.min(), ys.max()
k = np.clip((np.arange(H * SS) - top) / max(bot - top, 1), 0, 1)[:, None, None]   # 0 at top row of pixels
# Blender pixel rows start at the bottom: row 0 = bottom, so flip the gradient
grad = lin("#F08A3C") * (1 - k) + lin("#FFE7A6") * k
hl = (np.roll(bold, -10, 0) * bold) * 0.0
img = np.zeros((H * SS, W * SS, 4), np.float32)
over(img, lin("#2A160C"), shadow)
over(img, lin("#4A2A18"), outline)
over(img, lin("#FFF6E2"), rim)
over(img, grad, bold)
over(img, clk[..., :3], clk[..., 3])
small = img.reshape(H, SS, W, SS, 4).mean((1, 3))
rows = np.where(small[..., 3].max(1) > 0.01)[0]; cols = np.where(small[..., 3].max(0) > 0.01)[0]
m = 12
small = small[max(rows.min() - m, 0):rows.max() + m, max(cols.min() - m, 0):cols.max() + m]
out = bpy.data.images.new("Logo", small.shape[1], small.shape[0], alpha=True)
out.pixels[:] = small.ravel().tolist()
os.makedirs(os.path.join(ROOT, "Assets/Game/Resources/UI"), exist_ok=True)
out.filepath_raw = os.path.join(ROOT, "Assets/Game/Resources/UI/Logo.png"); out.file_format = "PNG"; out.save()
os.makedirs(os.path.join(ROOT, "ModelSources"), exist_ok=True)
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(ROOT, "ModelSources/Logo.blend"))

# ------------------------------------------------------------------ icon
sc = reset()
sc.render.film_transparent = False
w = sc.world or bpy.data.worlds.new("W"); sc.world = w; w.use_nodes = True
bg = next(n for n in w.node_tree.nodes if n.type == "BACKGROUND")
bg.inputs["Color"].default_value = srgb("#F6C77A"); bg.inputs["Strength"].default_value = 1.0
# warm radial-ish backdrop: layered discs
for i, (r, col) in enumerate([(3.2, "#F2B260"), (2.5, "#F6C77A"), (1.9, "#FBDDA0")]):
    disc("Bg%d" % i, r, -1 + i * 0.01, flat("Bg%d" % i, col), (0, 0.1))
clock((0, 0.15), 1.25, 0.2)
# tomorrow box peeking in at the bottom right
bpy.ops.mesh.primitive_plane_add(size=1, location=(1.05, -1.05, 0.5))
bx = bpy.context.active_object; bx.scale = (0.95, 0.75, 1); bx.rotation_euler = (0, 0, math.radians(-8))
bx.data.materials.append(flat("Box", "#5AA9F0"))
bpy.ops.mesh.primitive_plane_add(size=1, location=(1.05, -0.72, 0.52))
lid = bpy.context.active_object; lid.scale = (1.05, 0.2, 1); lid.rotation_euler = (0, 0, math.radians(-8))
lid.data.materials.append(flat("Lid", "#3D7FC4"))
bpy.ops.mesh.primitive_plane_add(size=1, location=(1.05, -1.05, 0.53))
rib = bpy.context.active_object; rib.scale = (0.14, 0.75, 1); rib.rotation_euler = (0, 0, math.radians(-8))
rib.data.materials.append(flat("Ribbon", "#FFF4DC"))
camera(sc, 1024, 1024, 4.6)
os.makedirs(os.path.join(ROOT, "Assets/Game/Art"), exist_ok=True)
sc.render.filepath = os.path.join(ROOT, "Assets/Game/Art/Icon.png")
bpy.ops.render.render(write_still=True)
print("LOGO DONE")
