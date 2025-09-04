import bpy
import os

# === CONFIGURATION ===
characters = ['汉', '字', '學', '爱', '发', '口', '只']  # List of Chinese characters
font_path = '"C:\Users\hanjo\Downloads\NotoSansSC-VariableFont_wght.ttf"'  # Must be full path to a .ttf
output_dir = 'C:/Users/hanjo/Unity/hanzispeak/Python/fbx-new'  # Must exist
extrude_depth = 1  # Depth of 3D extrusion
font_size = 100.0      # Optional scaling factor

# === CLEANUP ===
bpy.ops.wm.read_homefile(use_empty=True)  # Reset scene

# === FUNCTION TO CREATE AND EXPORT EACH CHARACTER ===
def create_character_fbx(char, index):
    bpy.ops.object.text_add(enter_editmode=False, location=(index * 2.5, 0, 0))
    obj = bpy.context.object
    obj.data.body = char
    obj.data.extrude = extrude_depth
    obj.data.size = font_size

    # Load the custom font
    font = bpy.data.fonts.load(font_path)
    obj.data.font = font

    # Convert to mesh for exporting
    bpy.ops.object.convert(target='MESH')
    
    # Export as FBX
    fbx_filename = f"{char}.fbx"
    export_path = os.path.join(output_dir, fbx_filename)
    bpy.ops.export_scene.fbx(filepath=export_path, use_selection=True)
    print(f"Exported {char} to {export_path}")

# === GENERATE FBX FOR EACH CHARACTER ===
for i, ch in enumerate(characters):
    create_character_fbx(ch, i)

print("All characters exported.")
