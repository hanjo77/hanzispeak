import bpy
import os

# === Enable the built-in OBJ importer ===
bpy.ops.preferences.addon_enable(module="io_scene_obj")

# === Set these to absolute paths ===
input_folder = "C:\\Users\\hanjo\\Hanjo de hanzi\\Python\\glb"
output_folder = "C:\\Users\\hanjo\\Hanjo de hanzi\\Python\\fbx"

# === Clean the scene ===
bpy.ops.wm.read_factory_settings(use_empty=True)

# === Iterate over OBJ files ===
for filename in os.listdir(input_folder):
    if filename.lower().endswith(".obj"):
        obj_path = os.path.join(input_folder, filename)
        fbx_path = os.path.join(output_folder, os.path.splitext(filename)[0] + ".fbx")

        print(f"Importing {obj_path}")
        bpy.ops.import_scene.obj(filepath=obj_path)

        print(f"Exporting {fbx_path}")
        bpy.ops.export_scene.fbx(filepath=fbx_path, use_selection=False)

        # Clean the scene for the next file
        bpy.ops.object.select_all(action='SELECT')
        bpy.ops.object.delete()

