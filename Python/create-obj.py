import freetype
import numpy as np
import trimesh
from shapely.geometry import Polygon, MultiPolygon
from shapely.ops import unary_union

from gtts import gTTS
from pypinyin import pinyin, Style

import asyncio
import edge_tts

import os
import csv
import json


import pygltflib  # pip install pygltflib

def generate_pronunciation(char, output_dir="audio"):
    tts = gTTS(text=char, lang='zh-cn', slow=False)
    os.makedirs(output_dir, exist_ok=True)
    filename = f"{output_dir}/{char}.wav"
    tts.save(filename)
    print(f"✅ Success: {char} → {filename}")

    return filename

async def generate_male_voice(char, output_path="audio"):
    voice = "zh-CN-YunxiNeural"  # Natural male voice
    os.makedirs(output_path, exist_ok=True)
    communicate = edge_tts.Communicate(char, voice)
    await communicate.save(f"{output_path}/{char}.wav")
    print(f"✅ Success: {char} → {output_path}/{char}.wav")

import freetype
import trimesh
import numpy as np
from shapely.geometry import Polygon, MultiPolygon
from shapely.ops import unary_union

def export_character_fbx(char, font_path, output_path, extrude_depth=0.2):
    try:
        # Initialize font
        face = freetype.Face(font_path)
        face.set_char_size(48 * 64)
        
        # Load character
        face.load_char(char)
        outline = face.glyph.outline
        
        # Process contours with proper point handling
        contours = []
        start = 0
        
        for i in range(outline.n_contours):
            end = outline.contours[i]
            points = outline.points[start:end+1]
            tags = outline.tags[start:end+1]
            start = end + 1
            
            # Convert points to proper coordinate array
            verts = []
            for point in points:
                if hasattr(point, 'x'):  # Handle both object and tuple cases
                    x, y = point.x, point.y
                else:
                    x, y = point[0], point[1]
                verts.append([x/64.0, -y/64.0])  # Scale and flip Y
            
            # Ensure closed polygon
            if len(verts) >= 3:
                if not np.array_equal(verts[0], verts[-1]):
                    verts.append(verts[0])  # Close the loop
                
                # Create and validate polygon
                try:
                    poly = Polygon(verts)
                    if not poly.is_valid:
                        poly = poly.buffer(0)
                    if poly.area > 1e-6:
                        contours.append(poly)
                except Exception as e:
                    print(f"⚠️ Polygon creation failed for {char}: {str(e)}")
                    continue
        
        if not contours:
            print(f"⚠️ No valid contours for: {char}")
            return False
        
        # Create 3D mesh
        merged = unary_union(contours)
        meshes = []
        
        if merged.geom_type == "MultiPolygon":
            for poly in merged.geoms:
                try:
                    mesh = trimesh.creation.extrude_polygon(poly, height=extrude_depth)
                    meshes.append(mesh)
                except Exception as e:
                    print(f"⚠️ Extrusion failed for part of {char}: {str(e)}")
                    continue
        else:
            try:
                meshes.append(trimesh.creation.extrude_polygon(merged, height=extrude_depth))
            except Exception as e:
                print(f"⚠️ Extrusion failed for {char}: {str(e)}")
                return False
        
        if not meshes:
            print(f"⚠️ No valid meshes for: {char}")
            return False
            
        # Combine and export
        final_mesh = trimesh.util.concatenate(meshes)
        final_mesh.apply_transform(trimesh.transformations.scale_matrix([1, -1, 1]))
        
        # FBX export with validation
        try:
            final_mesh.export(
                output_path,
                file_type='fbx',
                include_normals=True,
                include_color=False
            )
            print(f"✅ Success: {char} → {output_path}")
            return True
        except Exception as e:
            print(f"❌ FBX export failed for {char}: {str(e)}")
            return False
            
    except Exception as e:
        print(f"❌ Critical error processing {char}: {str(e)}")
        return False
    
def export_character_gltf(char, font_path, output_path, extrude_depth=0.2):
    try:
        # Load font and get outline
        face = freetype.Face(font_path)
        face.set_char_size(48 * 64)
        face.load_char(char)
        outline = face.glyph.outline
        
        # Process contours
        polygons = []
        start, end = 0, 0
        for i in range(outline.n_contours):
            end = outline.contours[i]
            points = outline.points[start:end+1]
            verts = [[p[0]/64.0, -p[1]/64.0] for p in points]  # Flip Y
            if len(verts) >= 3:
                poly = Polygon(verts)
                if not poly.is_valid:
                    poly = poly.buffer(0)
                if poly.area > 1e-6:
                    polygons.append(poly)
            start = end + 1
        
        if not polygons:
            return False
        
        # Create 3D mesh
        merged = unary_union(polygons)
        meshes = []
        
        if merged.geom_type == "MultiPolygon":
            for poly in merged.geoms:
                mesh = trimesh.creation.extrude_polygon(poly, extrude_depth)
                meshes.append(mesh)
        else:
            meshes.append(trimesh.creation.extrude_polygon(merged, extrude_depth))
        
        final_mesh = trimesh.util.concatenate(meshes)

        trimesh.exchange.export.export_mesh(final_mesh, output_path)
        
        return True
    except Exception as e:
        print(f"Failed on {char}: {str(e)}")
        return False

def character_to_3d_extruded(char, font_path, output_path, extrude_depth=0.2):
    # Load font
    face = freetype.Face(font_path)
    face.set_char_size(48 * 64)  # Size in 1/64ths of a point
    
    # Load glyph
    face.load_char(char)
    outline = face.glyph.outline
    
    # Get contours
    contours = []
    start, end = 0, 0
    for i in range(outline.n_contours):
        end = outline.contours[i]
        points = outline.points[start:end+1]
        tags = outline.tags[start:end+1]
        start = end + 1
        
        verts = []
        for j in range(len(points)):
            x, y = points[j]
            verts.append([x / 64.0, -y / 64.0])  # Flip Y axis
        contours.append(verts)
    
    # Convert to valid polygons
    polygons = []
    for contour in contours:
        if len(contour) >= 3:
            polygon = Polygon(contour)
            if not polygon.is_valid:
                polygon = polygon.buffer(0)
            if polygon.area > 1e-6:
                polygons.append(polygon)
    
    if not polygons:
        print(f"⚠️ No valid polygons for: {char}")
        return False
    
    # Handle MultiPolygon cases (critical fix)
    merged_geom = unary_union(polygons)
    
    meshes = []
    if merged_geom.geom_type == "MultiPolygon":
        for poly in merged_geom.geoms:
            if poly.geom_type == "Polygon":
                mesh = trimesh.creation.extrude_polygon(poly, extrude_depth)
                meshes.append(mesh)
    elif merged_geom.geom_type == "Polygon":
        meshes.append(trimesh.creation.extrude_polygon(merged_geom, extrude_depth))
    
    if not meshes:
        print(f"⚠️ No valid geometry for: {char}")
        return False
    
    # Combine all parts
    final_mesh = trimesh.util.concatenate(meshes)
    print(output_path)
    try:
        # Create output directory if it doesn't exist
        os.makedirs(output_dir, exist_ok=True)
        
        # Create safe filename (Unicode + ASCII fallback)
        filename = f"{char}.obj"  # Keep original character in filename
        safe_path = os.path.join(output_dir, filename)
        
        # Force UTF-8 encoding for file operations
        with open(safe_path, 'w', encoding='utf-8') as f:
            final_mesh.export(f.name, file_type='obj')  # Using file handle
            
        print(f"✅ Exported {char} to {filename}")
        return True
    except Exception as e:
        # Fallback to pinyin if Unicode fails
        print('failed')


def convert_obj_to_fbx(obj_path, fbx_path):
    """Convert OBJ to FBX using Blender"""
    blender_script = f"""


import bpy
bpy.ops.wm.obj_import(filepath='{os.path.abspath(obj_path)}')
bpy.ops.export_scene.fbx(
    filepath='{os.path.abspath(fbx_path)}',
    use_selection=True,
    global_scale=100.0
)
"""
    with open("temp_converter.py", "w") as f:
        f.write(blender_script)
    
    os.system(f"blender --background --python temp_converter.py")
    os.remove("temp_converter.py")

OUTPUT_DIR = "data"
os.makedirs(OUTPUT_DIR, exist_ok=True)

# Prepare database files
csv_file = open(f"{OUTPUT_DIR}/pinyin_database.csv", "w", encoding="utf-8")
csv_writer = csv.writer(csv_file)
csv_writer.writerow(["hanzi", "pinyin"])
json_db = []

# Test with problematic characters
font_path = "NotoSansCJKsc-Bold.otf"
for char in ["一", "七", "三", "上", "下", "不", "专", "业", "东", "个", "中", "为", "主", "么", "义", "乐", "九", "也", "习", "乡", "书", "买", "了", "争", "事", "二", "五", "京", "亲", "人", "什", "介", "从", "他", "以", "们", "件", "价", "任", "份", "伊", "优", "会", "传", "伤", "但", "低", "佛", "作", "你", "保", "信", "修", "健", "兄", "先", "全", "八", "公", "六", "兰", "关", "兴", "养", "再", "写", "农", "决", "冷", "准", "出", "分", "划", "创", "判", "利", "别", "到", "前", "办", "功", "务", "劣", "动", "助", "势", "化", "北", "医", "十", "单", "南", "博", "危", "历", "去", "友", "反", "发", "口", "只", "叫", "可", "史", "右", "司", "吃", "合", "同", "名", "后", "吗", "吧", "听", "告", "员", "呢", "命", "和", "品", "哪", "哲", "唱", "商", "喜", "喝", "四", "因", "园", "困", "国", "图", "在", "地", "场", "城", "基", "境", "多", "大", "天", "太", "失", "好", "如", "妹", "姐", "姻", "婚", "子", "字", "季", "学", "宇", "宗", "宙", "定", "实", "室", "害", "家", "容", "对", "导", "小", "少", "就", "居", "展", "工", "左", "市", "师", "帮", "平", "年", "幸", "广", "床", "应", "店", "府", "度", "康", "建", "开", "弟", "当", "影", "很", "律", "得", "德", "心", "志", "快", "态", "怎", "怕", "思", "息", "情", "想", "意", "感", "慢", "成", "我", "战", "戚", "户", "房", "所", "手", "技", "把", "投", "护", "报", "挑", "据", "授", "改", "放", "政", "效", "教", "数", "文", "料", "断", "斯", "新", "方", "旅", "日", "旧", "时", "明", "易", "星", "春", "是", "晴", "最", "月", "有", "朋", "服", "期", "术", "机", "杂", "权", "材", "村", "条", "来", "构", "析", "果", "染", "查", "标", "校", "样", "桌", "椅", "欢", "歌", "此", "母", "比", "毕", "民", "气", "水", "污", "汽", "没", "治", "法", "济", "海", "源", "火", "灯", "点", "炼", "热", "然", "爱", "父", "牌", "物", "特", "狗", "猫", "率", "环", "现", "球", "理", "生", "用", "由", "电", "画", "略", "的", "目", "看", "真", "督", "知", "短", "研", "社", "票", "福", "离", "种", "科", "程", "究", "穿", "窗", "站", "竞", "等", "简", "管", "米", "系", "素", "纸", "组", "织", "绍", "经", "结", "络", "统", "绩", "维", "缺", "网", "老", "考", "而", "肉", "股", "能", "脑", "自", "舞", "节", "苹", "茶", "药", "菜", "营", "蕉", "虽", "融", "行", "衣", "西", "要", "见", "观", "视", "角", "解", "计", "认", "讨", "议", "论", "证", "评", "识", "试", "话", "说", "读", "谁", "调", "责", "败", "质", "贸", "资", "赛", "走", "起", "超", "跨", "路", "跳", "车", "辑", "过", "运", "近", "还", "这", "进", "远", "通", "逻", "遇", "道", "那", "邻", "都", "里", "重", "量", "金", "钟", "钱", "销", "锻", "长", "门", "问", "间", "闻", "阴", "际", "院", "险", "难", "雨", "雪", "面", "革", "音", "项", "领", "题", "风", "飞", "饭", "馆", "香", "验", "高", "鱼", "鸡"]:  # Simple, multi-part, and hollow chars
    py = pinyin(char, style=Style.TONE3)[0][0]  # e.g. "ni3" for 你
    asyncio.run(generate_male_voice(char))
    # generate_pronunciation(char)
    output_dir="glb"
    os.makedirs(output_dir, exist_ok=True)
    character_to_3d_extruded(char, font_path, f"{output_dir}/{char}.fbx", 10)
    csv_writer.writerow([char, py])
    json_db.append({
        "hanzi": char,
        "pinyin": py
    })
csv_file.close()
with open(f"{OUTPUT_DIR}/pinyin_database.json", "w", encoding="utf-8") as f:
    json.dump(json_db, f, ensure_ascii=False, indent=2)

print(f"\n🎉 Generated {len(json_db)} assets in {OUTPUT_DIR}/")

