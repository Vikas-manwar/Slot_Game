"""
Art pipeline for the Slot Game project.

Turns the provided source art in /SourceArt into Unity-ready sprites in /Assets/UI:
  * slices the multi-state button sheets into one PNG per state (for Button sprite-swap)
  * crops the shared 816x624 machine layers (reel background, lever) to their content
  * trims panel art to its visible bounds (borders are 9-sliced in Unity)
  * draws the two bonus symbols (WILD star, SCATTER gem) in the same pixel-art palette
    as the provided Cherry / Bell / BAR / 7 symbols
  * draws a win-highlight frame and payline markers

Run from the repo root:  python3 Tools/build_art.py
Layout constants printed at the end are mirrored in Assets/Scripts/Editor/SlotSceneBuilder.cs.
"""
import os
from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SRC = os.path.join(ROOT, "SourceArt")
UI = os.path.join(ROOT, "Assets", "UI")

# Palette sampled from the provided symbols so new art blends in.
OUTLINE = (63, 59, 147, 255)
DEEP = (25, 6, 87, 255)
PALE = (221, 246, 247, 255)
STEEL = (161, 179, 198, 255)
SLATE = (110, 121, 166, 255)
GOLD_LIGHT = (252, 196, 88, 255)
GOLD_MID = (255, 165, 92, 255)
GOLD_DARK = (192, 139, 76, 255)
ORANGE_RED = (228, 59, 0, 255)
CRIMSON = (191, 25, 41, 255)
WHITE = (255, 255, 255, 255)


def out(*parts):
    path = os.path.join(UI, *parts)
    os.makedirs(os.path.dirname(path), exist_ok=True)
    return path


def load(name):
    return Image.open(os.path.join(SRC, name)).convert("RGBA")


def union_bbox(images):
    boxes = [im.getchannel("A").getbbox() for im in images]
    return (min(b[0] for b in boxes), min(b[1] for b in boxes),
            max(b[2] for b in boxes), max(b[3] for b in boxes))


def slice_vertical_states(sheet_name, prefix, states):
    """Sheets of N equal-height cells stacked vertically, one per button state."""
    sheet = load(sheet_name)
    cell_h = sheet.height // len(states)
    cells = [sheet.crop((0, i * cell_h, sheet.width, (i + 1) * cell_h)) for i in range(len(states))]
    box = union_bbox(cells)  # same rect for every state so the button never jumps
    for cell, state in zip(cells, states):
        cell.crop(box).save(out("Buttons", f"{prefix}_{state}.png"))


def slice_yes_no():
    """2 columns (YES, NO) x 3 rows (normal, highlighted, pressed)."""
    sheet = load("Buttons_YesNo_Sheet.png")
    states = ["Normal", "Highlighted", "Pressed"]
    col_w, row_h = sheet.width // 2, sheet.height // 3
    for col, label in enumerate(["Yes", "No"]):
        cells = [sheet.crop((col * col_w, r * row_h, (col + 1) * col_w, (r + 1) * row_h)) for r in range(3)]
        box = union_bbox(cells)
        for cell, state in zip(cells, states):
            cell.crop(box).save(out("Buttons", f"{label}_{state}.png"))


def trim(name, dest):
    im = load(name)
    im.crop(im.getchannel("A").getbbox()).save(dest)
    return im.getchannel("A").getbbox()


# --------------------------------------------------------------------------- bonus symbols
def draw_wild():
    """Golden star over a BAR-style plate reading WILD."""
    im = Image.new("RGBA", (96, 96), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)

    import math
    cx, cy, r_out, r_in = 48, 34, 34, 18
    pts = []
    for i in range(10):
        ang = -math.pi / 2 + i * math.pi / 5
        r = r_out if i % 2 == 0 else r_in
        pts.append((cx + r * math.cos(ang), cy + r * math.sin(ang)))

    d.polygon(pts, fill=OUTLINE)                                   # outline pass
    inner = [(cx + (x - cx) * 0.86, cy + (y - cy) * 0.86) for x, y in pts]
    d.polygon(inner, fill=GOLD_MID)
    # Facet shading: each star arm split into a lit and a shaded half.
    for i in range(0, 10, 2):
        tip = inner[i]
        left, right = inner[(i - 1) % 10], inner[(i + 1) % 10]
        d.polygon([(cx, cy), tip, left], fill=GOLD_LIGHT)
        d.polygon([(cx, cy), tip, right], fill=GOLD_DARK if i in (4, 6) else GOLD_MID)
    d.ellipse((cx - 5, cy - 9, cx + 1, cy - 3), fill=(255, 244, 214, 255))  # specular

    # WILD plate, styled after the provided BAR symbol.
    d.rectangle((10, 64, 85, 91), fill=DEEP)
    d.rectangle((12, 66, 83, 89), fill=STEEL)
    d.rectangle((14, 68, 81, 87), fill=PALE)
    font = ImageFont.truetype(os.path.join(UI, "Fonts", "BebasNeue-Regular.ttf"), 22)
    d.fontmode = "1"  # no anti-aliasing: keeps the pixel-art look
    text = "WILD"
    w = d.textlength(text, font=font)
    x = 48 - w / 2
    d.text((x + 1, 66), text, font=font, fill=SLATE)
    d.text((x, 65), text, font=font, fill=OUTLINE)
    im.save(out("Symbols", "Symbol_Wild.png"))


def draw_scatter():
    """Faceted blue gem with sparkles: the free-spins SCATTER."""
    im = Image.new("RGBA", (96, 96), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    ICE = (170, 230, 255, 255)
    SKY = (95, 170, 245, 255)
    BLUE = (60, 110, 220, 255)
    NAVY = (45, 55, 165, 255)

    top, crown_y, girdle_y, bottom = 14, 22, 38, 86
    outline = [(28, top), (68, top), (88, girdle_y), (48, bottom), (8, girdle_y)]
    d.polygon(outline, fill=DEEP)
    shrink = [(29, top + 3), (67, top + 3), (84, girdle_y), (48, bottom - 5), (12, girdle_y)]
    d.polygon(shrink, fill=BLUE)

    # Crown facets
    d.polygon([(29, top + 3), (40, top + 3), (34, girdle_y), (12, girdle_y)], fill=SKY)
    d.polygon([(40, top + 3), (56, top + 3), (48, girdle_y), (34, girdle_y)], fill=ICE)
    d.polygon([(56, top + 3), (67, top + 3), (84, girdle_y), (62, girdle_y)], fill=SKY)
    d.polygon([(56, top + 3), (62, girdle_y), (48, girdle_y)], fill=PALE)
    # Pavilion facets
    d.polygon([(12, girdle_y), (34, girdle_y), (48, bottom - 5)], fill=BLUE)
    d.polygon([(34, girdle_y), (48, girdle_y), (48, bottom - 5)], fill=SKY)
    d.polygon([(48, girdle_y), (62, girdle_y), (48, bottom - 5)], fill=BLUE)
    d.polygon([(62, girdle_y), (84, girdle_y), (48, bottom - 5)], fill=NAVY)
    d.line([(12, girdle_y), (84, girdle_y)], fill=OUTLINE, width=2)

    # Sparkles
    for sx, sy, s in ((80, 12, 6), (14, 70, 4), (76, 66, 3)):
        d.polygon([(sx, sy - s), (sx + 1, sy - 1), (sx + s, sy), (sx + 1, sy + 1),
                   (sx, sy + s), (sx - 1, sy + 1), (sx - s, sy), (sx - 1, sy - 1)], fill=WHITE)
    im.save(out("Symbols", "Symbol_Scatter.png"))


# --------------------------------------------------------------------------- UI helpers
def draw_win_frame():
    """Gold frame drawn over a winning symbol cell (tinted & pulsed in Unity)."""
    size = 100
    im = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    d.rounded_rectangle((0, 0, size - 1, size - 1), radius=12, outline=DEEP, width=8)
    d.rounded_rectangle((2, 2, size - 3, size - 3), radius=10, outline=GOLD_DARK, width=5)
    d.rounded_rectangle((3, 3, size - 4, size - 4), radius=9, outline=GOLD_LIGHT, width=2)
    im.save(out("Sprites", "Win_Frame.png"))


def draw_payline_marker():
    """Small arrow placed at both ends of the payline (flipped for the right side)."""
    im = Image.new("RGBA", (16, 20), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    d.polygon([(0, 0), (15, 10), (0, 19)], fill=DEEP)
    d.polygon([(1, 3), (11, 10), (1, 16)], fill=ORANGE_RED)
    d.polygon([(1, 3), (11, 10), (1, 10)], fill=GOLD_MID)
    im.save(out("Sprites", "Payline_Marker.png"))


def white_pixel():
    Image.new("RGBA", (4, 4), WHITE).save(out("Sprites", "White.png"))


def main():
    # Provided symbols: copied as-is (96x96 pixel art).
    for name in ("Cherry", "Bell", "Bar", "Seven"):
        load(f"Symbol_{name}.png").save(out("Symbols", f"Symbol_{name}.png"))
    draw_wild()
    draw_scatter()

    # Machine layers. Frame and background stay full size; the frame's
    # transparent windows line up with the reel rectangles below.
    load("Background.png").save(out("Machine", "Background.png"))
    load("Machine_Frame.png").save(out("Machine", "Machine_Frame.png"))
    reels = load("Machine_ReelBackgrounds.png")
    reel_rect = (229, 246, 337, 456)
    reels.crop(reel_rect).save(out("Machine", "Reel_Background.png"))
    lever_up = trim("Lever_Up.png", out("Machine", "Lever_Up.png"))
    lever_down = trim("Lever_Down.png", out("Machine", "Lever_Down.png"))

    slice_vertical_states("Button_ArrowLeft_Sheet.png", "ArrowLeft", ["Normal", "Highlighted", "Pressed", "Disabled"])
    slice_vertical_states("Button_ArrowRight_Sheet.png", "ArrowRight", ["Normal", "Highlighted", "Pressed", "Disabled"])
    slice_vertical_states("Button_Close_Sheet.png", "Close", ["Normal", "Highlighted", "Pressed", "Disabled"])
    slice_yes_no()

    blue = trim("Panel_Blue.png", out("Panels", "Panel_Blue.png"))
    orange = trim("Panel_Orange.png", out("Panels", "Panel_Orange.png"))

    draw_win_frame()
    draw_payline_marker()
    white_pixel()

    print("Reel rect (machine px):", reel_rect)
    print("Lever up bbox:", lever_up, "Lever down bbox:", lever_down)
    print("Panel blue bbox:", blue, "Panel orange bbox:", orange)


if __name__ == "__main__":
    main()
