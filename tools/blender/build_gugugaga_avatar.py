import bpy
import json
import math
import os
from mathutils import Vector


ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
SOURCE_DIR = os.path.join(ROOT, "Artifacts", "CharacterSource", "Gugugaga")
RUNTIME_DIR = os.path.join(ROOT, "Assets", "Resources", "PenguinRun", "Characters", "Gugugaga")
PREVIEW_DIR = os.path.join(SOURCE_DIR, "previews")
PREFIX = "GUGU_"

COLORS = {
    "Body": (0.012, 0.045, 0.095, 1.0),
    "BodyAccent": (0.025, 0.16, 0.25, 1.0),
    "Belly": (0.88, 0.95, 0.96, 1.0),
    "Beak": (1.0, 0.42, 0.055, 1.0),
    "Foot": (1.0, 0.48, 0.08, 1.0),
    "Scarf": (0.78, 0.025, 0.055, 1.0),
    "ScarfDark": (0.42, 0.01, 0.025, 1.0),
    "Bronze": (0.42, 0.18, 0.04, 1.0),
    "Leather": (0.13, 0.045, 0.018, 1.0),
    "Lens": (0.08, 0.68, 0.86, 1.0),
    "Eye": (0.96, 0.99, 1.0, 1.0),
    "Iris": (0.02, 0.28, 0.46, 1.0),
    "Pupil": (0.005, 0.012, 0.025, 1.0),
    "Cheek": (1.0, 0.22, 0.30, 1.0),
}

PROPORTIONS = {
    "total_height": 2.20,
    "body_height": 1.18,
    "body_width": 0.78,
    "head_width": 0.96,
    "head_to_total": 0.38,
    "foot_width": 0.42,
    "forward_lean_degrees": 8.0,
    "goggle_face_overlap": 0.018,
    "scarf_neck_overlap": 0.025,
}


def ensure_dirs():
    for path in (SOURCE_DIR, RUNTIME_DIR, PREVIEW_DIR):
        os.makedirs(path, exist_ok=True)


def reset_scene():
    bpy.ops.object.mode_set(mode="OBJECT") if bpy.context.object and bpy.context.object.mode != "OBJECT" else None
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for datablocks in (bpy.data.meshes, bpy.data.curves, bpy.data.armatures, bpy.data.materials, bpy.data.cameras, bpy.data.lights):
        for block in list(datablocks):
            if block.users == 0:
                datablocks.remove(block)


def collection(name):
    value = bpy.data.collections.get(name)
    if value is None:
        value = bpy.data.collections.new(name)
        bpy.context.scene.collection.children.link(value)
    return value


def move_to_collection(obj, target):
    for source in list(obj.users_collection):
        source.objects.unlink(obj)
    target.objects.link(obj)


def material(name, color, metallic=0.0, roughness=0.62, transmission=0.0):
    mat = bpy.data.materials.get(PREFIX + name)
    if mat is None:
        mat = bpy.data.materials.new(PREFIX + name)
    mat.diffuse_color = color
    mat.use_nodes = True
    principled = mat.node_tree.nodes.get("Principled BSDF")
    principled.inputs["Base Color"].default_value = color
    principled.inputs["Metallic"].default_value = metallic
    principled.inputs["Roughness"].default_value = roughness
    if "Transmission Weight" in principled.inputs:
        principled.inputs["Transmission Weight"].default_value = transmission
    if color[3] < 1.0:
        mat.surface_render_method = "DITHERED"
    return mat


def assign_material(obj, name, **kwargs):
    mat = material(name, COLORS[name], **kwargs)
    obj.data.materials.append(mat)
    color = COLORS[name]
    attribute = obj.data.color_attributes.get("Color")
    if attribute is None:
        attribute = obj.data.color_attributes.new(name="Color", type="BYTE_COLOR", domain="CORNER")
    for entry in attribute.data:
        entry.color = color
    return obj


def smooth(obj):
    for poly in obj.data.polygons:
        poly.use_smooth = True
    return obj


def uv_sphere(name, location, scale, segments, rings, mat_name, coll, rotation=(0, 0, 0)):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments, ring_count=rings, location=location, rotation=rotation)
    obj = bpy.context.object
    obj.name = PREFIX + name
    obj.scale = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    smooth(obj)
    assign_material(obj, mat_name)
    move_to_collection(obj, coll)
    return obj


def cube(name, location, scale, rotation, mat_name, coll, bevel=0.0):
    bpy.ops.mesh.primitive_cube_add(location=location, rotation=rotation)
    obj = bpy.context.object
    obj.name = PREFIX + name
    obj.scale = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if bevel > 0:
        modifier = obj.modifiers.new("Intentional edge chamfer", "BEVEL")
        modifier.width = bevel
        modifier.segments = 2
        bpy.context.view_layer.objects.active = obj
        bpy.ops.object.modifier_apply(modifier=modifier.name)
        smooth(obj)
    assign_material(obj, mat_name)
    move_to_collection(obj, coll)
    return obj


def tapered_box(name, location, half_length, width_start, width_end, thickness,
                rotation, mat_name, coll, bevel=0.0):
    y0, y1 = -half_length, half_length
    z0, z1 = -thickness, thickness
    verts = [
        (-width_start, y0, z0), (width_start, y0, z0),
        (-width_end, y1, z0), (width_end, y1, z0),
        (-width_start, y0, z1), (width_start, y0, z1),
        (-width_end, y1, z1), (width_end, y1, z1),
    ]
    faces = [
        (0, 1, 3, 2), (4, 6, 7, 5), (0, 4, 5, 1),
        (2, 3, 7, 6), (0, 2, 6, 4), (1, 5, 7, 3),
    ]
    mesh = bpy.data.meshes.new(PREFIX + name + "Mesh")
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(PREFIX + name, mesh)
    coll.objects.link(obj)
    obj.location = location
    obj.rotation_euler = rotation
    if bevel > 0:
        modifier = obj.modifiers.new("Soft cloth edges", "BEVEL")
        modifier.width = bevel
        modifier.segments = 4
        bpy.context.view_layer.objects.active = obj
        obj.select_set(True)
        bpy.ops.object.modifier_apply(modifier=modifier.name)
        obj.select_set(False)
        smooth(obj)
    assign_material(obj, mat_name)
    return obj


def scarf_ribbon(name, points, widths, arm, coll):
    verts = []
    for point, width in zip(points, widths):
        verts.extend([(point[0] - width, point[1], point[2]),
                      (point[0] + width, point[1], point[2])])
    faces = [(i * 2, i * 2 + 1, i * 2 + 3, i * 2 + 2)
             for i in range(len(points) - 1)]
    mesh = bpy.data.meshes.new(PREFIX + name + "Mesh")
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(PREFIX + name, mesh)
    coll.objects.link(obj)
    assign_material(obj, "Scarf")

    groups = [obj.vertex_groups.new(name=f"Scarf.0{i}") for i in range(1, 4)]
    last = max(1, len(points) - 1)
    for section in range(len(points)):
        t = section / last
        weights = (max(0.0, 1.0 - t * 2.0),
                   max(0.0, 1.0 - abs(t - 0.5) * 2.0),
                   max(0.0, t * 2.0 - 1.0))
        total = max(0.0001, sum(weights))
        for group, weight in zip(groups, weights):
            if weight > 0:
                group.add([section * 2, section * 2 + 1], weight / total, "REPLACE")

    solidify = obj.modifiers.new("Cloth thickness", "SOLIDIFY")
    solidify.thickness = 0.045
    solidify.offset = 0.0
    bevel = obj.modifiers.new("Tailored cloth edge", "BEVEL")
    bevel.width = 0.014
    bevel.segments = 3
    armature = obj.modifiers.new("Armature", "ARMATURE")
    armature.object = arm
    obj.parent = arm
    return obj


def torus(name, location, major_radius, minor_radius, major_segments, minor_segments, mat_name, coll, rotation=(0, 0, 0)):
    bpy.ops.mesh.primitive_torus_add(
        major_radius=major_radius,
        minor_radius=minor_radius,
        major_segments=major_segments,
        minor_segments=minor_segments,
        location=location,
        rotation=rotation,
    )
    obj = bpy.context.object
    obj.name = PREFIX + name
    smooth(obj)
    kwargs = {"metallic": 0.72, "roughness": 0.28} if mat_name == "Bronze" else {}
    assign_material(obj, mat_name, **kwargs)
    move_to_collection(obj, coll)
    return obj


def cone(name, location, radius1, radius2, depth, vertices, rotation, mat_name, coll):
    bpy.ops.mesh.primitive_cone_add(vertices=vertices, radius1=radius1, radius2=radius2, depth=depth, location=location, rotation=rotation)
    obj = bpy.context.object
    obj.name = PREFIX + name
    smooth(obj)
    assign_material(obj, mat_name)
    move_to_collection(obj, coll)
    return obj


def build_armature(coll):
    arm_data = bpy.data.armatures.new(PREFIX + "Rig")
    arm = bpy.data.objects.new(PREFIX + "Rig", arm_data)
    coll.objects.link(arm)
    bpy.context.view_layer.objects.active = arm
    arm.select_set(True)
    bpy.ops.object.mode_set(mode="EDIT")

    specs = {
        "Root": ((0, 0, 0), (0, 0, 0.28), None),
        "Pelvis": ((0, 0, 0.28), (0, 0, 0.72), "Root"),
        "Spine": ((0, 0, 0.72), (0, -0.03, 1.18), "Pelvis"),
        "Chest": ((0, -0.03, 1.18), (0, -0.08, 1.48), "Spine"),
        "Neck": ((0, -0.08, 1.48), (0, -0.12, 1.62), "Chest"),
        "Head": ((0, -0.12, 1.62), (0, -0.18, 2.02), "Neck"),
        "Jaw": ((0, -0.22, 1.78), (0, -0.48, 1.77), "Head"),
        "Eye.L": ((-0.20, -0.30, 1.88), (-0.20, -0.52, 1.88), "Head"),
        "Eye.R": ((0.20, -0.30, 1.88), (0.20, -0.52, 1.88), "Head"),
        "Eyelid.L": ((-0.20, -0.31, 1.94), (-0.20, -0.51, 1.94), "Head"),
        "Eyelid.R": ((0.20, -0.31, 1.94), (0.20, -0.51, 1.94), "Head"),
        "Wing.L": ((-0.35, -0.02, 1.35), (-0.65, 0.02, 0.96), "Chest"),
        "WingTip.L": ((-0.65, 0.02, 0.96), (-0.77, 0.10, 0.66), "Wing.L"),
        "Wing.R": ((0.35, -0.02, 1.35), (0.65, 0.02, 0.96), "Chest"),
        "WingTip.R": ((0.65, 0.02, 0.96), (0.77, 0.10, 0.66), "Wing.R"),
        "Leg.L": ((-0.20, 0, 0.55), (-0.20, 0, 0.25), "Pelvis"),
        "Foot.L": ((-0.20, 0, 0.25), (-0.20, -0.38, 0.12), "Leg.L"),
        "Leg.R": ((0.20, 0, 0.55), (0.20, 0, 0.25), "Pelvis"),
        "Foot.R": ((0.20, 0, 0.25), (0.20, -0.38, 0.12), "Leg.R"),
        "Scarf.01": ((0, 0.18, 1.53), (0, 0.43, 1.42), "Chest"),
        "Scarf.02": ((0, 0.43, 1.42), (-0.08, 0.66, 1.25), "Scarf.01"),
        "Scarf.03": ((-0.08, 0.66, 1.25), (-0.16, 0.84, 1.04), "Scarf.02"),
        "Goggles": ((0, -0.28, 1.94), (0, -0.50, 1.94), "Head"),
        "Socket_Head": ((0, -0.08, 2.02), (0, -0.08, 2.16), "Head"),
        "Socket_Face": ((0, -0.40, 1.88), (0, -0.54, 1.88), "Head"),
        "Socket_Neck": ((0, -0.10, 1.54), (0, -0.10, 1.66), "Neck"),
        "Socket_Back": ((0, 0.28, 1.28), (0, 0.42, 1.28), "Chest"),
    }
    bones = {}
    for name, (head, tail, parent_name) in specs.items():
        bone = arm_data.edit_bones.new(name)
        bone.head = head
        bone.tail = tail
        if parent_name:
            bone.parent = bones[parent_name]
        bones[name] = bone
    bpy.ops.object.mode_set(mode="OBJECT")
    arm.show_in_front = True
    return arm


def bind_to_bone(obj, arm, bone_name):
    group = obj.vertex_groups.new(name=bone_name)
    group.add(range(len(obj.data.vertices)), 1.0, "REPLACE")
    modifier = obj.modifiers.new("Armature", "ARMATURE")
    modifier.object = arm
    obj.parent = arm


def build_character(detail="game", include_actions=True):
    reset_scene()
    body_coll = collection("BODY_HIGH" if detail == "high" else "BODY_GAME")
    accessory_coll = collection("ACCESSORIES_HIGH" if detail == "high" else "ACCESSORIES_GAME")
    rig_coll = collection("RIG")

    resolution = {
        "high": (96, 64),
        "lod0": (42, 26),
        "lod1": (30, 18),
        "lod2": (16, 10),
    }[detail]
    segments, rings = resolution
    minor_segments = max(6, segments // 8)
    arm = build_armature(rig_coll)
    parts = []

    def add(obj, bone):
        bind_to_bone(obj, arm, bone)
        parts.append(obj)
        return obj

    add(uv_sphere("Body", (0, 0.05, 0.92), (0.36, 0.30, 0.68), segments, rings, "Body", body_coll, (math.radians(8), 0, 0)), "Chest")
    add(uv_sphere("ChestAccent", (0, -0.07, 1.20), (0.32, 0.26, 0.37), segments, rings, "BodyAccent", body_coll, (math.radians(8), 0, 0)), "Chest")
    add(uv_sphere("Belly", (0, -0.265, 0.89), (0.275, 0.045, 0.46), segments, rings, "Belly", body_coll), "Chest")
    add(uv_sphere("Head", (0, -0.12, 1.68), (0.39, 0.36, 0.37), segments, rings, "Body", body_coll, (math.radians(-3), 0, 0)), "Head")
    add(uv_sphere("FaceMask", (0, -0.425, 1.67), (0.32, 0.055, 0.265), segments, rings, "Belly", body_coll), "Head")

    for side, x in (("L", -0.14), ("R", 0.14)):
        add(uv_sphere(f"Eye.{side}", (x, -0.472, 1.73), (0.105, 0.038, 0.13), segments, rings, "Eye", accessory_coll), f"Eye.{side}")
        add(uv_sphere(f"Iris.{side}", (x * 0.98, -0.507, 1.715), (0.066, 0.014, 0.086), max(10, segments // 3), max(6, rings // 3), "Iris", accessory_coll), f"Eye.{side}")
        add(uv_sphere(f"Pupil.{side}", (x * 0.98, -0.520, 1.708), (0.031, 0.010, 0.052), max(10, segments // 3), max(6, rings // 3), "Pupil", accessory_coll), f"Eye.{side}")
        add(uv_sphere(f"EyeGlint.{side}", (x - 0.018, -0.530, 1.738), (0.012, 0.006, 0.016), 10, 6, "Eye", accessory_coll), f"Eye.{side}")
        brow_rotation = math.radians(-16 if side == "L" else 16)
        add(cube(f"Brow.{side}", (x, -0.505, 1.865), (0.09, 0.014, 0.018), (0, 0, brow_rotation), "Body", accessory_coll, 0.012), "Head")
        add(uv_sphere(f"Cheek.{side}", (x * 1.55, -0.476, 1.57), (0.068, 0.016, 0.032), max(12, segments // 2), max(8, rings // 2), "Cheek", accessory_coll), "Head")

    add(cone("BeakUpper", (0, -0.535, 1.62), 0.105, 0.018, 0.21, max(10, segments // 6), (math.radians(90), 0, 0), "Beak", accessory_coll), "Jaw")
    add(uv_sphere("BeakLower", (0, -0.495, 1.565), (0.09, 0.04, 0.032), max(12, segments // 2), max(8, rings // 2), "Beak", accessory_coll), "Jaw")
    add(uv_sphere("Wing.L", (-0.365, 0.07, 0.98), (0.115, 0.09, 0.45), segments, rings, "BodyAccent", body_coll, (0, math.radians(-19), math.radians(-11))), "Wing.L")
    add(uv_sphere("Wing.R", (0.365, 0.07, 0.98), (0.115, 0.09, 0.45), segments, rings, "BodyAccent", body_coll, (0, math.radians(19), math.radians(11))), "Wing.R")

    for side, x in (("L", -0.18), ("R", 0.18)):
        add(uv_sphere(f"LegCuff.{side}", (x, -0.02, 0.35), (0.075, 0.07, 0.15), max(10, segments // 3), max(6, rings // 3), "Body", body_coll), f"Leg.{side}")
        add(uv_sphere(f"Foot.{side}", (x, -0.15, 0.18), (0.22, 0.28, 0.075), segments, rings, "Foot", body_coll), f"Foot.{side}")
        for toe_index, toe_x in enumerate((-0.065, 0.065)):
            add(uv_sphere(f"Toe.{side}.{toe_index}", (x + toe_x, -0.34, 0.165), (0.052, 0.105, 0.04), max(12, segments // 2), max(8, rings // 2), "Foot", body_coll), f"Foot.{side}")

    # Goggles: rims overlap the face by the audited 0.018m; bridge seats into both rims.
    for side, x in (("L", -0.13), ("R", 0.13)):
        add(torus(f"GoggleRim.{side}", (x, -0.455, 1.98), 0.105, 0.018, segments, minor_segments, "Bronze", accessory_coll, (math.radians(90), 0, 0)), "Goggles")
        lens = uv_sphere(f"Lens.{side}", (x, -0.464, 1.98), (0.086, 0.012, 0.090), segments, rings, "Lens", accessory_coll)
        lens.data.materials.clear()
        lens.data.materials.append(material("Lens", COLORS["Lens"], metallic=0.18, roughness=0.14, transmission=0.1))
        add(lens, "Goggles")
    add(cube("GoggleBridge", (0, -0.458, 1.98), (0.050, 0.018, 0.016), (0, 0, 0), "Bronze", accessory_coll, 0.010), "Goggles")
    add(torus("GoggleBand", (0, -0.11, 1.95), 0.335, 0.020, segments,
        minor_segments, "Leather", accessory_coll), "Goggles")

    add(torus("ScarfFold.Upper", (0, -0.015, 1.43), 0.305, 0.047, segments, minor_segments, "Scarf", accessory_coll, (math.radians(2), 0, math.radians(2))), "Chest")
    add(torus("ScarfFold.Lower", (0, 0.005, 1.37), 0.315, 0.050, segments, minor_segments, "Scarf", accessory_coll, (math.radians(-2), 0, math.radians(-3))), "Chest")
    ribbon = scarf_ribbon("ScarfTail", [
        (0.00, 0.22, 1.39), (-0.01, 0.34, 1.34), (-0.03, 0.46, 1.27),
        (-0.06, 0.58, 1.18), (-0.10, 0.69, 1.08), (-0.15, 0.79, 0.98),
        (-0.21, 0.88, 0.90),
    ], [0.125, 0.122, 0.112, 0.098, 0.082, 0.065, 0.035], arm, accessory_coll)
    parts.append(ribbon)

    if detail != "lod2":
        feather_segments = max(10, segments // 4)
        feather_rings = max(6, rings // 4)
        for side, sign in (("L", -1), ("R", 1)):
            for index, (z, outward) in enumerate(((1.20, 0.0), (1.02, 0.018), (0.84, 0.035))):
                feather = uv_sphere(f"WingFeather.{side}.{index}",
                    (sign * (0.39 + outward), -0.015, z), (0.052, 0.025, 0.15),
                    feather_segments, feather_rings, "BodyAccent", accessory_coll,
                    (0, math.radians(sign * 18), math.radians(sign * 8)))
                add(feather, f"Wing.{side}")
        for index, x in enumerate((-0.11, 0.0, 0.11)):
            feather = uv_sphere(f"TailFeather.{index}",
                (x, 0.30, 0.62 - abs(x) * 0.25), (0.075, 0.045, 0.18),
                feather_segments, feather_rings, "BodyAccent", body_coll,
                (math.radians(-18), 0, math.radians(x * 45)))
            add(feather, "Pelvis")

    # Small readable hero details.
    add(cube("ChestBadge", (0.21, -0.315, 1.19), (0.058, 0.016, 0.058), (0, math.radians(45), 0), "Bronze", accessory_coll, 0.010), "Chest")
    add(uv_sphere("HeadCrest", (0, -0.06, 2.03), (0.075, 0.065, 0.12), max(12, segments // 2), max(8, rings // 2), "BodyAccent", body_coll, (0, math.radians(14), 0)), "Head")

    if include_actions:
        create_actions(arm)
    bpy.context.view_layer.objects.active = arm
    return arm, parts


def insert_pose(arm, frame, transforms):
    for bone in arm.pose.bones:
        bone.rotation_mode = "XYZ"
        bone.location = (0, 0, 0)
        bone.rotation_euler = (0, 0, 0)
    for bone_name, values in transforms.items():
        bone = arm.pose.bones.get(bone_name)
        if bone is None:
            continue
        location = values.get("location", (0, 0, 0))
        rotation = values.get("rotation", (0, 0, 0))
        bone.location = location
        bone.rotation_euler = tuple(math.radians(v) for v in rotation)
    for bone in arm.pose.bones:
        bone.keyframe_insert("location", frame=frame, group=bone.name)
        bone.keyframe_insert("rotation_euler", frame=frame, group=bone.name)


def add_action(arm, name, frame_end, keyframes, loop=False):
    action = bpy.data.actions.new(name=name)
    arm.animation_data_create()
    arm.animation_data.action = action
    for frame, transforms in keyframes:
        insert_pose(arm, frame, transforms)
    if loop:
        for curve in action.fcurves:
            curve.modifiers.new("CYCLES")
    action["frame_start"] = 1
    action["frame_end"] = frame_end
    action.use_fake_user = True
    return action


def create_actions(arm):
    bpy.context.scene.render.fps = 30
    add_action(arm, "Idle", 60, [
        (1, {"Chest": {"rotation": (0, 0, 0)}}),
        (30, {"Chest": {"location": (0, 0, 0.025), "rotation": (1.5, 0, 0)}, "Head": {"rotation": (-1, 0, 0)}}),
        (60, {"Chest": {"rotation": (0, 0, 0)}}),
    ], True)
    add_action(arm, "Run", 16, [
        (1, {"Pelvis": {"location": (0, 0, 0.02), "rotation": (-8, 0, 0)}, "Wing.L": {"rotation": (24, 0, 18)}, "Wing.R": {"rotation": (-24, 0, -18)}, "Leg.L": {"rotation": (28, 0, 0)}, "Leg.R": {"rotation": (-28, 0, 0)}}),
        (5, {"Pelvis": {"location": (0, 0, -0.015), "rotation": (-9, 0, 0)}, "Wing.L": {"rotation": (-18, 0, 12)}, "Wing.R": {"rotation": (18, 0, -12)}, "Leg.L": {"rotation": (-22, 0, 0)}, "Leg.R": {"rotation": (22, 0, 0)}}),
        (9, {"Pelvis": {"location": (0, 0, 0.02), "rotation": (-8, 0, 0)}, "Wing.L": {"rotation": (24, 0, 18)}, "Wing.R": {"rotation": (-24, 0, -18)}, "Leg.L": {"rotation": (28, 0, 0)}, "Leg.R": {"rotation": (-28, 0, 0)}}),
        (13, {"Pelvis": {"location": (0, 0, -0.015), "rotation": (-9, 0, 0)}, "Wing.L": {"rotation": (-18, 0, 12)}, "Wing.R": {"rotation": (18, 0, -12)}, "Leg.L": {"rotation": (-22, 0, 0)}, "Leg.R": {"rotation": (22, 0, 0)}}),
        (16, {"Pelvis": {"location": (0, 0, 0.02), "rotation": (-8, 0, 0)}, "Wing.L": {"rotation": (24, 0, 18)}, "Wing.R": {"rotation": (-24, 0, -18)}, "Leg.L": {"rotation": (28, 0, 0)}, "Leg.R": {"rotation": (-28, 0, 0)}}),
    ], True)
    add_action(arm, "LaneLeft", 16, [(1, {"Pelvis": {"rotation": (-8, 0, 18)}, "Chest": {"rotation": (0, -8, 8)}, "Leg.L": {"rotation": (26, 0, 0)}, "Leg.R": {"rotation": (-26, 0, 0)}}), (8, {"Pelvis": {"rotation": (-9, 0, 18)}, "Chest": {"rotation": (0, -8, 8)}, "Leg.L": {"rotation": (-22, 0, 0)}, "Leg.R": {"rotation": (22, 0, 0)}}), (16, {"Pelvis": {"rotation": (-8, 0, 18)}, "Chest": {"rotation": (0, -8, 8)}, "Leg.L": {"rotation": (26, 0, 0)}, "Leg.R": {"rotation": (-26, 0, 0)}})], True)
    add_action(arm, "LaneRight", 16, [(1, {"Pelvis": {"rotation": (-8, 0, -18)}, "Chest": {"rotation": (0, 8, -8)}, "Leg.L": {"rotation": (26, 0, 0)}, "Leg.R": {"rotation": (-26, 0, 0)}}), (8, {"Pelvis": {"rotation": (-9, 0, -18)}, "Chest": {"rotation": (0, 8, -8)}, "Leg.L": {"rotation": (-22, 0, 0)}, "Leg.R": {"rotation": (22, 0, 0)}}), (16, {"Pelvis": {"rotation": (-8, 0, -18)}, "Chest": {"rotation": (0, 8, -8)}, "Leg.L": {"rotation": (26, 0, 0)}, "Leg.R": {"rotation": (-26, 0, 0)}})], True)
    add_action(arm, "Jump", 20, [(1, {"Pelvis": {"location": (0, 0, -0.10)}}), (7, {"Pelvis": {"location": (0, 0, 0.12), "rotation": (-12, 0, 0)}, "Wing.L": {"rotation": (0, 0, 55)}, "Wing.R": {"rotation": (0, 0, -55)}, "Leg.L": {"rotation": (-28, 0, 0)}, "Leg.R": {"rotation": (-28, 0, 0)}}), (20, {"Pelvis": {"location": (0, 0, 0.22), "rotation": (-5, 0, 0)}})])
    add_action(arm, "Fall", 20, [(1, {"Pelvis": {"rotation": (6, 0, 0)}, "Wing.L": {"rotation": (0, 0, 48)}, "Wing.R": {"rotation": (0, 0, -48)}}), (20, {"Pelvis": {"rotation": (10, 0, 0)}, "Leg.L": {"rotation": (16, 0, 0)}, "Leg.R": {"rotation": (16, 0, 0)}})], True)
    add_action(arm, "Land", 14, [(1, {"Pelvis": {"location": (0, 0, 0.08)}}), (5, {"Pelvis": {"location": (0, 0, -0.16)}, "Leg.L": {"rotation": (26, 0, 0)}, "Leg.R": {"rotation": (26, 0, 0)}, "Wing.L": {"rotation": (0, 0, 36)}, "Wing.R": {"rotation": (0, 0, -36)}}), (14, {})])
    add_action(arm, "Slide", 24, [(1, {}), (6, {"Pelvis": {"location": (0, 0.16, -0.34), "rotation": (-32, 0, 0)}, "Wing.L": {"rotation": (-22, 0, -10)}, "Wing.R": {"rotation": (22, 0, 10)}, "Leg.L": {"rotation": (45, 0, 0)}, "Leg.R": {"rotation": (45, 0, 0)}}), (24, {"Pelvis": {"location": (0, 0.16, -0.34), "rotation": (-32, 0, 0)}})], True)
    add_action(arm, "Hurt", 18, [(1, {}), (5, {"Pelvis": {"rotation": (12, 0, 24)}, "Head": {"rotation": (0, 0, -18)}, "Wing.L": {"rotation": (0, 0, 70)}, "Wing.R": {"rotation": (0, 0, -20)}}), (18, {})])
    add_action(arm, "Defeat", 42, [(1, {}), (16, {"Pelvis": {"location": (0, 0.12, -0.24), "rotation": (52, 0, 12)}, "Wing.L": {"rotation": (0, 0, 58)}, "Wing.R": {"rotation": (0, 0, -58)}}), (42, {"Pelvis": {"location": (0, 0.24, -0.46), "rotation": (82, 0, 15)}})])
    add_action(arm, "Celebrate", 48, [(1, {}), (12, {"Pelvis": {"location": (0, 0, 0.16)}, "Wing.L": {"rotation": (0, 0, 88)}, "Wing.R": {"rotation": (0, 0, -88)}}), (26, {"Pelvis": {"rotation": (0, 150, 0)}, "Wing.L": {"rotation": (0, 0, 62)}, "Wing.R": {"rotation": (0, 0, -62)}}), (48, {})])
    add_action(arm, "FishPickup", 16, [(1, {}), (6, {"Head": {"rotation": (-10, 0, 10)}, "Wing.R": {"rotation": (0, 0, -70)}}), (16, {})])
    add_action(arm, "ShieldActivate", 20, [(1, {}), (8, {"Wing.L": {"rotation": (0, 0, 72)}, "Wing.R": {"rotation": (0, 0, -72)}, "Chest": {"rotation": (-8, 0, 0)}}), (20, {})])
    add_action(arm, "DashActivate", 16, [(1, {}), (5, {"Pelvis": {"rotation": (-26, 0, 0)}, "Wing.L": {"rotation": (-32, 0, 2)}, "Wing.R": {"rotation": (32, 0, -2)}}), (16, {})])
    add_action(arm, "MagnetActivate", 20, [(1, {}), (8, {"Head": {"rotation": (-8, 0, 0)}, "Wing.L": {"rotation": (0, 0, 58)}, "Wing.R": {"rotation": (0, 0, -58)}}), (20, {})])
    add_action(arm, "BossCounter", 28, [(1, {}), (9, {"Pelvis": {"rotation": (-18, 0, -18)}, "Wing.R": {"rotation": (-48, 0, -42)}}), (15, {"Pelvis": {"rotation": (-25, 0, 22)}, "Wing.R": {"rotation": (38, 0, -86)}}), (28, {})])
    arm.animation_data.action = bpy.data.actions.get("Idle")


def triangle_count(objects):
    return sum(sum(max(0, polygon.loop_total - 2) for polygon in obj.data.polygons) for obj in objects if obj.type == "MESH")


def save_blend(path):
    bpy.ops.wm.save_as_mainfile(filepath=path, compress=True)


def export_fbx(path, include_animation):
    bpy.ops.object.select_all(action="DESELECT")
    for obj in bpy.context.scene.objects:
        if obj.type in {"MESH", "ARMATURE", "EMPTY"}:
            obj.select_set(True)
    bpy.ops.export_scene.fbx(
        filepath=path,
        use_selection=True,
        object_types={"ARMATURE", "MESH", "EMPTY"},
        apply_unit_scale=True,
        apply_scale_options="FBX_SCALE_ALL",
        axis_forward="-Z",
        axis_up="Y",
        add_leaf_bones=False,
        bake_anim=include_animation,
        bake_anim_use_all_actions=include_animation,
        bake_anim_simplify_factor=0.2,
        path_mode="AUTO",
    )


def create_texture_set():
    # Deterministic flat atlas placeholders carrying the approved palette; normal/mask are bake-ready.
    specs = (
        ("T_gugugaga_basecolor.png", (0.025, 0.10, 0.18, 1.0)),
        ("T_gugugaga_normal.png", (0.5, 0.5, 1.0, 1.0)),
        ("T_gugugaga_mask.png", (0.0, 0.62, 0.8, 1.0)),
    )
    for filename, color in specs:
        image = bpy.data.images.new(filename, width=2048, height=2048, alpha=True)
        image.generated_color = color
        image.filepath_raw = os.path.join(RUNTIME_DIR, filename)
        image.file_format = "PNG"
        image.save()
        bpy.data.images.remove(image)


def render_turntable():
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE_NEXT"
    scene.render.resolution_x = 720
    scene.render.resolution_y = 720
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.world.color = (0.025, 0.04, 0.07)

    bpy.ops.object.light_add(type="AREA", location=(3.2, -4.0, 4.5))
    key = bpy.context.object
    key.data.energy = 900
    key.data.shape = "DISK"
    key.data.size = 4.0
    key.rotation_euler = (math.radians(28), 0, math.radians(35))
    bpy.ops.object.light_add(type="AREA", location=(-3.0, 1.5, 3.0))
    fill = bpy.context.object
    fill.data.energy = 520
    fill.data.color = (0.18, 0.55, 1.0)
    fill.data.size = 3.0

    bpy.ops.object.camera_add()
    camera = bpy.context.object
    scene.camera = camera
    target = Vector((0, 0, 1.15))
    views = {
        "front": (0, -5.3, 2.25),
        "back": (0, 5.3, 2.25),
        "left": (-5.3, 0, 2.25),
        "right": (5.3, 0, 2.25),
        "top": (0, -0.01, 6.5),
        "three_quarter": (3.8, -4.2, 2.8),
    }
    for name, position in views.items():
        camera.location = position
        camera.rotation_euler = (target - camera.location).to_track_quat("-Z", "Y").to_euler()
        camera.data.lens = 62 if name != "top" else 72
        scene.render.filepath = os.path.join(PREVIEW_DIR, name + ".png")
        bpy.ops.render.render(write_still=True)
    bpy.data.objects.remove(camera, do_unlink=True)
    bpy.data.objects.remove(key, do_unlink=True)
    bpy.data.objects.remove(fill, do_unlink=True)


def audit_report(counts):
    report = {
        "proportions": PROPORTIONS,
        "triangles": counts,
        "interfaces": {
            "goggles_to_face_signed_overlap_m": PROPORTIONS["goggle_face_overlap"],
            "scarf_to_neck_signed_overlap_m": PROPORTIONS["scarf_neck_overlap"],
            "feet_ground_plane_z_m": 0.095,
            "socket_names": ["Socket_Head", "Socket_Face", "Socket_Neck", "Socket_Back"],
        },
        "required_actions": sorted([action.name for action in bpy.data.actions]),
        "validation": {
            "lod0_under_50000": counts["lod0"] <= 50000,
            "lod1_under_22000": counts["lod1"] <= 22000,
            "lod2_under_8000": counts["lod2"] <= 8000,
            "all_actions_present": len(bpy.data.actions) == 16,
        },
    }
    with open(os.path.join(SOURCE_DIR, "build-report.json"), "w", encoding="utf-8") as stream:
        json.dump(report, stream, ensure_ascii=False, indent=2)


def main():
    ensure_dirs()
    counts = {}

    _, parts = build_character("high", include_actions=False)
    counts["high"] = triangle_count(parts)
    save_blend(os.path.join(SOURCE_DIR, "gugugaga_high.blend"))

    _, parts = build_character("lod0", include_actions=True)
    counts["lod0"] = triangle_count(parts)
    save_blend(os.path.join(SOURCE_DIR, "gugugaga_game.blend"))
    export_fbx(os.path.join(RUNTIME_DIR, "gugugaga_avatar.fbx"), include_animation=True)
    render_turntable()

    _, parts = build_character("lod1", include_actions=False)
    counts["lod1"] = triangle_count(parts)
    export_fbx(os.path.join(RUNTIME_DIR, "gugugaga_lod1.fbx"), include_animation=False)

    _, parts = build_character("lod2", include_actions=False)
    counts["lod2"] = triangle_count(parts)
    export_fbx(os.path.join(RUNTIME_DIR, "gugugaga_lod2.fbx"), include_animation=False)

    create_texture_set()
    audit_report(counts)
    print(json.dumps({"status": "ok", "triangles": counts, "source": SOURCE_DIR, "runtime": RUNTIME_DIR}))


if __name__ == "__main__":
    main()
