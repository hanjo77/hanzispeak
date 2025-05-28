#!/usr/bin/env python3
# render_hanzi_png.py
"""
Render PNG files for a list of strings (e.g. Chinese characters).

Usage examples
--------------
# 1) From a text file (one string per line)
python render_hanzi_png.py \
    --font NotoSansCJKsc-Regular.otf \
    --fontsize 240             \
    --padding 40               \
    --bg '#FFFFFF' --fg '#000000' \
    --outdir pngs              \
    --file hanzi_list.txt

# 2) Directly from CLI strings
python render_hanzi_png.py \
    --font myfont.ttf "你好" "世界"
"""

import argparse
import os
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont


def render_text(text: str,
                font: ImageFont.FreeTypeFont,
                padding: int,
                fg: str,
                bg: str,
                out_path: Path):
    """Render a single string and save as PNG."""
    # Measure text
    dummy_img = Image.new("RGB", (1, 1))
    draw = ImageDraw.Draw(dummy_img)
    left, top, right, bottom = draw.textbbox((0, 0), text, font=font)
    width, height = right - left, bottom - top

    mode = "RGBA" if (bg.lower() == "transparent" or len(bg) in (8, 9)) else "RGB"
    img  = Image.new(mode, (width + 2 * padding, height + 2 * padding),
                    color=(0, 0, 0, 0) if bg.lower() == "transparent" else bg)
    draw = ImageDraw.Draw(img)
    draw.text((padding - left, padding - top), text, font=font, fill=fg)
    img.save(out_path, format="PNG")
    print(f"✓ {out_path.name}")


def parse_args():
    ap = argparse.ArgumentParser(
        description="Render a list of strings to individual PNG files.")
    ap.add_argument("--font", required=True,
                    help="Path to .ttf or .otf font file that supports Hanzi")
    ap.add_argument("--fontsize", type=int, default=128,
                    help="Point size (default 128)")
    ap.add_argument("--padding", type=int, default=32,
                    help="Pixels around glyph (default 32)")
    ap.add_argument("--bg", default="#FFFFFF",
                    help="Background colour (CSS hex or 'transparent')")
    ap.add_argument("--fg", default="#000000",
                    help="Text colour (CSS hex)")
    ap.add_argument("--outdir", default="out_png",
                    help="Directory for generated PNGs")
    group = ap.add_mutually_exclusive_group(required=True)
    group.add_argument("--file",
                       help="UTF-8 text file, one string per line")
    group.add_argument("strings", nargs="*",
                       help="Strings given directly on the CLI")
    return ap.parse_args()


def main():
    args = parse_args()

    font = ImageFont.truetype(args.font, args.fontsize)

    outdir = Path(args.outdir)
    outdir.mkdir(parents=True, exist_ok=True)

    if args.file:
        with open(args.file, encoding="utf-8") as fh:
            items = [line.strip() for line in fh if line.strip()]
    else:
        items = args.strings

    for text in items:
        safe_name = "_".join(f"{ord(c):X}" for c in text)
        out_path = outdir / f"{safe_name}.png"
        render_text(text, font, args.padding,
                    fg=args.fg, bg=args.bg, out_path=out_path)


if __name__ == "__main__":
    main()
