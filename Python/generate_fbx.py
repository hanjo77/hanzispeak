import bpy
import os

# === CONFIGURATION ===
characters = ["汉字", "一", "七", "三", "上", "下", "不", "专", "业", "东", "个", "中", "为", "主", "么", "义", "乐", "九", "也", "习", "乡", "书", "买", "了", "争", "事", "二", "五", "京", "亲", "人", "什", "介", "从", "他", "以", "们", "件", "价", "任", "份", "伊", "优", "会", "传", "伤", "但", "低", "佛", "作", "你", "保", "信", "修", "健", "兄", "先", "全", "八", "公", "六", "兰", "关", "兴", "养", "再", "写", "农", "决", "冷", "准", "出", "分", "划", "创", "判", "利", "别", "到", "前", "办", "功", "务", "劣", "动", "助", "势", "化", "北", "医", "十", "单", "南", "博", "危", "历", "去", "友", "反", "发", "口", "只", "叫", "可", "史", "右", "司", "吃", "合", "同", "名", "后", "吗", "吧", "听", "告", "员", "呢", "命", "和", "品", "哪", "哲", "唱", "商", "喜", "喝", "四", "因", "园", "困", "国", "图", "在", "地", "场", "城", "基", "境", "多", "大", "天", "太", "失", "好", "如", "妹", "姐", "姻", "婚", "子", "字", "季", "学", "宇", "宗", "宙", "定", "实", "室", "害", "家", "容", "对", "导", "小", "少", "就", "居", "展", "工", "左", "市", "师", "帮", "平", "年", "幸", "广", "床", "应", "店", "府", "度", "康", "建", "开", "弟", "当", "影", "很", "律", "得", "德", "心", "志", "快", "态", "怎", "怕", "思", "息", "情", "想", "意", "感", "慢", "成", "我", "战", "戚", "户", "房", "所", "手", "技", "把", "投", "护", "报", "挑", "据", "授", "改", "放", "政", "效", "教", "数", "文", "料", "断", "斯", "新", "方", "旅", "日", "旧", "时", "明", "易", "星", "春", "是", "晴", "最", "月", "有", "朋", "服", "期", "术", "机", "杂", "权", "材", "村", "条", "来", "构", "析", "果", "染", "查", "标", "校", "样", "桌", "椅", "欢", "歌", "此", "母", "比", "毕", "民", "气", "水", "污", "汽", "没", "治", "法", "济", "海", "源", "火", "灯", "点", "炼", "热", "然", "爱", "父", "牌", "物", "特", "狗", "猫", "率", "环", "现", "球", "理", "生", "用", "由", "电", "画", "略", "的", "目", "看", "真", "督", "知", "短", "研", "社", "票", "福", "离", "种", "科", "程", "究", "穿", "窗", "站", "竞", "等", "简", "管", "米", "系", "素", "纸", "组", "织", "绍", "经", "结", "络", "统", "绩", "维", "缺", "网", "老", "考", "而", "肉", "股", "能", "脑", "自", "舞", "节", "苹", "茶", "药", "菜", "营", "蕉", "虽", "融", "行", "衣", "西", "要", "见", "观", "视", "角", "解", "计", "认", "讨", "议", "论", "证", "评", "识", "试", "话", "说", "读", "谁", "调", "责", "败", "质", "贸", "资", "赛", "走", "起", "超", "跨", "路", "跳", "车", "辑", "过", "运", "近", "还", "这", "进", "远", "通", "逻", "遇", "道", "那", "邻", "都", "里", "重", "量", "金", "钟", "钱", "销", "锻", "长", "门", "问", "间", "闻", "阴", "际", "院", "险", "难", "雨", "雪", "面", "革", "音", "项", "领", "题", "风", "飞", "饭", "馆", "香", "验", "高", "鱼", "鸡"]
# characters = ['汉', '字', '學', '爱', '发', '口', '只']  # List of Chinese characters
font_path = 'C:/Users/hanjo/Unity/hanzispeak/Python/NotoSansCJKsc-Thin.otf'  # Must be full path to a .ttf
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
