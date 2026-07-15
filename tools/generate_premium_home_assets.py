"""Deterministically render premium storybook camp assets for Unity Resources."""

from __future__ import annotations

import math
import random
from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter


ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "Assets/Resources/PenguinRun/StorybookUI/PremiumHome"
S = 2
CELL = 512
INK = (54, 55, 61, 255)
WOOD = (126, 79, 48, 255)
WOOD_DARK = (76, 52, 40, 255)
WOOD_LIGHT = (174, 112, 61, 255)
SNOW = (239, 244, 240, 255)
SNOW_SHADE = (178, 202, 215, 255)
CANVAS = (217, 192, 151, 255)
CANVAS_DARK = (142, 104, 78, 255)
TEAL = (54, 105, 110, 255)
RED = (175, 72, 55, 255)
GOLD = (244, 177, 72, 255)
BLUE = (81, 119, 151, 255)


def sc(v):
    if isinstance(v, (tuple, list)):
        return tuple(int(x * S) for x in v)
    return int(v * S)


def canvas(width=CELL, height=CELL):
    return Image.new("RGBA", (sc(width), sc(height)), (0, 0, 0, 0))


def poly(draw, points, fill, outline=INK, width=3):
    pts = [sc(p) for p in points]
    draw.polygon(pts, fill=fill)
    if outline:
        draw.line(pts + [pts[0]], fill=outline, width=sc(width), joint="curve")


def line(draw, points, fill=INK, width=3):
    draw.line([sc(p) for p in points], fill=fill, width=sc(width), joint="curve")


def ellipse(draw, box, fill, outline=INK, width=3):
    draw.ellipse(sc(box), fill=fill, outline=outline, width=sc(width) if outline else 1)


def rounded(draw, box, radius, fill, outline=INK, width=3):
    draw.rounded_rectangle(sc(box), radius=sc(radius), fill=fill, outline=outline,
                           width=sc(width) if outline else 1)


def wood(draw, box, angle=0, light=False):
    x0, y0, x1, y1 = box
    color = WOOD_LIGHT if light else WOOD
    if angle == 0:
        rounded(draw, box, 5, color, WOOD_DARK, 3)
        for frac in (0.30, 0.67):
            y = y0 + (y1 - y0) * frac
            line(draw, [(x0 + 8, y), (x1 - 8, y + 1)], WOOD_DARK, 1)
    else:
        half = (y1 - y0) / 2
        cx, cy = (x0 + x1) / 2, (y0 + y1) / 2
        length = x1 - x0
        dx, dy = math.cos(angle) * length / 2, math.sin(angle) * length / 2
        px, py = -math.sin(angle) * half, math.cos(angle) * half
        poly(draw, [(cx-dx-px, cy-dy-py), (cx+dx-px, cy+dy-py),
                    (cx+dx+px, cy+dy+py), (cx-dx+px, cy-dy+py)], color, WOOD_DARK, 3)


def snow_cap(draw, points):
    poly(draw, points, SNOW, SNOW_SHADE, 2)


def glow(image, center, radius, color=(255, 173, 62, 180)):
    layer = canvas(image.width // S, image.height // S)
    d = ImageDraw.Draw(layer)
    cx, cy = sc(center)
    r = sc(radius)
    d.ellipse((cx-r, cy-r, cx+r, cy+r), fill=color)
    layer = layer.filter(ImageFilter.GaussianBlur(sc(radius * 0.45)))
    image.alpha_composite(layer)


def flame(draw, cx, base, size):
    poly(draw, [(cx, base-size), (cx-size*.52, base-size*.25), (cx-size*.34, base),
                (cx+size*.34, base), (cx+size*.55, base-size*.32)], (232, 91, 42, 255), (116, 51, 35, 255), 2)
    poly(draw, [(cx, base-size*.72), (cx-size*.25, base-size*.20), (cx, base-size*.06),
                (cx+size*.25, base-size*.22)], (255, 211, 83, 255), None)


def lantern(draw, x, y, scale=1.0):
    w, h = 32*scale, 47*scale
    rounded(draw, (x-w/2, y-h, x+w/2, y), 5, (240, 184, 82, 230), WOOD_DARK, 3)
    line(draw, [(x-w*.34, y-h*.78), (x+w*.34, y-h*.78)], (255, 236, 161, 255), 3)
    line(draw, [(x-w*.25, y-h), (x-w*.38, y-h-11*scale),
                (x+w*.38, y-h-11*scale), (x+w*.25, y-h)], WOOD_DARK, 3)


def stones(draw, cx, cy, rx, count):
    for i in range(count):
        a = math.pi * 2 * i / count
        x, y = cx + math.cos(a)*rx, cy + math.sin(a)*rx*.38
        ellipse(draw, (x-18, y-10, x+18, y+11), (118, 126, 129, 255), (69, 73, 75, 255), 2)


def draw_bonfire(level):
    im = canvas(); d = ImageDraw.Draw(im)
    glow(im, (256, 350), 72 + level*17)
    stones(d, 256, 412, 75 + level*13, 9 + level*2)
    wood(d, (198, 378, 314, 397), angle=.24)
    wood(d, (198, 378, 314, 397), angle=-.24, light=True)
    flame(d, 256, 390, 84 + level*12)
    if level >= 2:
        wood(d, (145, 184, 164, 405)); wood(d, (348, 184, 367, 405))
        wood(d, (145, 184, 367, 204), light=True)
        line(d, [(196, 203), (196, 290)], WOOD_DARK, 4)
        ellipse(d, (171, 276, 221, 322), (64, 70, 73, 255), INK, 3)
        line(d, [(182, 322), (177, 345)], INK, 3); line(d, [(210, 322), (216, 345)], INK, 3)
        snow_cap(d, [(136, 179), (156, 166), (174, 181), (214, 174), (257, 183),
                     (298, 174), (335, 181), (357, 166), (376, 181), (369, 199), (143, 199)])
    if level >= 3:
        for x in (113, 399):
            wood(d, (x, 121, x+25, 411), light=True)
            snow_cap(d, [(x-10, 120), (x+12, 101), (x+36, 120), (x+26, 134), (x, 132)])
            line(d, [(x+12, 151), (x-5 if x > 250 else x+42, 177)], RED, 10)
        line(d, [(138, 159), (374, 159)], (70, 51, 41, 255), 4)
        for x in (175, 215, 255, 295, 335):
            poly(d, [(x-12, 158), (x+12, 158), (x, 187)], RED if x % 80 else TEAL, WOOD_DARK, 1)
        lantern(d, 122, 255, .72); lantern(d, 411, 255, .72)
    return im


def tent_body(d, level, color=CANVAS):
    poly(d, [(90, 408), (198, 172-level*12), (326, 166-level*8), (429, 408)], color, CANVAS_DARK, 4)
    poly(d, [(200, 174-level*12), (257, 408), (326, 166-level*8)], (193, 157, 112, 255), CANVAS_DARK, 3)
    line(d, [(90, 408), (429, 408)], WOOD_DARK, 5)
    snow_cap(d, [(87, 405), (112, 382), (159, 393), (205, 378), (259, 394),
                 (311, 379), (357, 394), (407, 382), (435, 405), (423, 421), (98, 421)])


def draw_workshop(level):
    im = canvas(); d = ImageDraw.Draw(im)
    if level == 1:
        tent_body(d, level, (188, 161, 119, 255))
        rounded(d, (220, 280, 300, 409), 7, (61, 53, 43, 255), WOOD_DARK, 4)
        wood(d, (118, 330, 206, 352), light=True); wood(d, (126, 349, 143, 410)); wood(d, (187, 349, 204, 410))
        line(d, [(146, 310), (174, 345)], INK, 6); ellipse(d, (134, 297, 152, 315), (110, 117, 119, 255), INK, 2)
    else:
        rounded(d, (88, 213-level*12, 420, 410), 9, (134, 92, 57, 255), WOOD_DARK, 5)
        for x in range(110, 410, 42): wood(d, (x, 230-level*12, x+30, 405), light=x % 84 == 0)
        poly(d, [(66, 229-level*12), (248, 118-level*12), (449, 226-level*12)],
             (89, 72, 58, 255), WOOD_DARK, 5)
        snow_cap(d, [(58, 226-level*12), (113, 195-level*12), (157, 188-level*12),
                     (210, 151-level*12), (250, 114-level*12), (285, 148-level*12),
                     (341, 176-level*12), (398, 199-level*12), (458, 225-level*12),
                     (447, 244-level*12), (69, 244-level*12)])
        rounded(d, (214, 280, 301, 410), 5, (50, 43, 38, 255), WOOD_DARK, 4)
        glow(im, (258, 331), 42, (255, 132, 47, 125))
        rounded(d, (112, 267, 178, 325), 4, (157, 201, 205, 255), WOOD_DARK, 3)
        line(d, [(145, 267), (145, 325)], WOOD_DARK, 2); line(d, [(112, 296), (178, 296)], WOOD_DARK, 2)
        wood(d, (338, 151-level*12, 374, 264), light=True)
        snow_cap(d, [(331, 151-level*12), (356, 137-level*12), (382, 151-level*12), (376, 164-level*12), (336, 164-level*12)])
        for x, shape in ((130, "hammer"), (364, "saw")):
            line(d, [(x, 340), (x+22, 380)], INK, 5)
            if shape == "hammer": rounded(d, (x-12, 329, x+21, 344), 3, (103, 111, 113, 255), INK, 2)
            else: poly(d, [(x-10, 329), (x+28, 340), (x+18, 353)], (156, 164, 165, 255), INK, 2)
        if level == 3:
            wood(d, (64, 169, 83, 407)); wood(d, (430, 169, 449, 407))
            line(d, [(75, 192), (439, 192)], WOOD_DARK, 4)
            for x in (115, 175, 235, 295, 355, 415):
                poly(d, [(x-14, 191), (x+14, 191), (x, 218)], GOLD if x % 120 else RED, WOOD_DARK, 1)
    return im


def draw_companion_hut(level):
    im = canvas(); d = ImageDraw.Draw(im)
    width = 250 + level*44; left = 256-width/2; right = 256+width/2
    rounded(d, (left, 220-level*20, right, 412), 48, (117, 91, 66, 255), WOOD_DARK, 5)
    for x in range(int(left+18), int(right-12), 38):
        wood(d, (x, 244-level*20, min(x+28, right-10), 404), light=(x//38)%2==0)
    poly(d, [(left-28, 245-level*20), (256, 126-level*18), (right+30, 245-level*20)],
         (71, 86, 88, 255), WOOD_DARK, 5)
    snow_cap(d, [(left-36, 241-level*20), (left+22, 215-level*20), (89+level*18, 207-level*20),
                 (166, 172-level*20), (256, 120-level*18), (339, 168-level*18),
                 (right-25, 207-level*20), (right+38, 241-level*20), (right+26, 260-level*20),
                 (left-26, 260-level*20)])
    rounded(d, (218, 288-level*5, 296, 412), 36, (50, 44, 41, 255), WOOD_DARK, 4)
    ellipse(d, (241, 342, 251, 352), GOLD, WOOD_DARK, 2)
    for x in (left+43, right-95):
        rounded(d, (x, 277-level*7, x+52, 326-level*7), 8, (148, 202, 207, 255), WOOD_DARK, 3)
        line(d, [(x+26, 278-level*7), (x+26, 325-level*7)], WOOD_DARK, 2)
    ellipse(d, (224, 158-level*16, 288, 213-level*16), (213, 204, 178, 255), WOOD_DARK, 3)
    poly(d, [(238, 184-level*16), (256, 170-level*16), (274, 184-level*16),
             (269, 199-level*16), (243, 199-level*16)], (66, 76, 78, 255), None)
    if level >= 2:
        lantern(d, left+22, 333, .75); lantern(d, right-20, 333, .75)
        wood(d, (left-10, 394, right+10, 414), light=True)
    if level == 3:
        rounded(d, (170, 211, 342, 256), 12, TEAL, WOOD_DARK, 3)
        line(d, [(190, 232), (322, 232)], (239, 219, 174, 255), 3)
        for x in (142, 370):
            wood(d, (x, 182, x+18, 405)); poly(d, [(x+9, 179), (x+56 if x < 256 else x-38, 198), (x+9, 222)], RED, WOOD_DARK, 2)
    return im


def books(d, x, y, count=5):
    colors = [RED, TEAL, GOLD, BLUE, CANVAS_DARK]
    for i in range(count):
        w = 17 + (i % 2)*5
        rounded(d, (x+i*19, y-(i%3)*3, x+i*19+w, y+53), 2, colors[i % len(colors)], WOOD_DARK, 2)
        line(d, [(x+i*19+3, y+10), (x+i*19+w-3, y+10)], (239, 220, 172, 255), 2)


def draw_library(level):
    im = canvas(); d = ImageDraw.Draw(im)
    tent_body(d, level, (204, 177, 130, 255))
    poly(d, [(205, 171-level*11), (260, 409), (326, 165-level*8)], (128, 67, 56, 255), CANVAS_DARK, 3)
    rounded(d, (220, 286, 300, 409), 34, (54, 49, 46, 255), WOOD_DARK, 4)
    books(d, 103, 338, 5)
    if level >= 2:
        wood(d, (89, 296, 205, 313), light=True); wood(d, (91, 312, 107, 411)); wood(d, (188, 312, 204, 411))
        books(d, 104, 245, 5)
        lantern(d, 385, 347, .85)
        ellipse(d, (226, 191-level*10, 290, 244-level*10), BLUE, WOOD_DARK, 3)
        for a in range(0, 360, 45):
            x = 258 + math.cos(math.radians(a))*22; y = 217-level*10 + math.sin(math.radians(a))*18
            ellipse(d, (x-2, y-2, x+2, y+2), GOLD, None)
    if level == 3:
        for x in (72, 438): wood(d, (x, 155, x+18, 411), light=True)
        line(d, [(81, 179), (447, 179)], WOOD_DARK, 4)
        for x in range(112, 430, 53): poly(d, [(x-13, 178), (x+13, 178), (x, 204)], TEAL if x%2 else RED, WOOD_DARK, 1)
        ellipse(d, (350, 125, 431, 207), (91, 140, 164, 255), WOOD_DARK, 4)
        line(d, [(391, 116), (391, 219)], GOLD, 4); line(d, [(340, 166), (441, 166)], GOLD, 4)
        line(d, [(366, 222), (347, 405)], WOOD_DARK, 6); line(d, [(417, 222), (438, 405)], WOOD_DARK, 6)
    return im


def draw_lookout(level):
    im = canvas(); d = ImageDraw.Draw(im)
    bottom = 420; top = 253-level*48; left = 182-level*16; right = 330+level*18
    wood(d, (left, top, left+20, bottom), light=True); wood(d, (right-20, top, right, bottom), light=True)
    for y in range(int(top+22), 407, 45):
        line(d, [(left+10, y), (right-10, y+30)], WOOD_DARK, 5)
        line(d, [(right-10, y), (left+10, y+30)], WOOD_DARK, 5)
    wood(d, (left-35, top-8, right+35, top+18), light=True)
    for x in range(int(left-25), int(right+20), 35): wood(d, (x, top-54, x+13, top-4))
    line(d, [(left-25, top-48), (right+25, top-48)], WOOD_DARK, 5)
    snow_cap(d, [(left-42, top-55), (left+8, top-71), (69+level*54, top-63),
                 (right-12, top-72), (right+43, top-55), (right+34, top-40), (left-34, top-40)])
    line(d, [(198, bottom), (287, top+20)], WOOD_DARK, 7)
    for i in range(7): line(d, [(205+i*12, bottom-i*22), (233+i*12, bottom-i*22)], WOOD_LIGHT, 5)
    lantern(d, right+16, top+8, .72)
    if level >= 2:
        pole_x = (left+right)/2
        line(d, [(pole_x, top-70), (pole_x, top-178)], WOOD_DARK, 7)
        poly(d, [(pole_x+3, top-171), (pole_x+78, top-150), (pole_x+3, top-125)], RED, WOOD_DARK, 3)
        rounded(d, (left-15, top-27, right+15, top+4), 4, TEAL, WOOD_DARK, 3)
    if level == 3:
        line(d, [(left-60, top-24), (left-60, top+120)], WOOD_DARK, 6)
        line(d, [(left-60, top-7), (left+15, top+20)], WOOD_DARK, 5)
        ellipse(d, (left-86, top+102, left-34, top+152), (103, 119, 122, 255), WOOD_DARK, 3)
        line(d, [(right-10, top-21), (right+80, top-64)], INK, 10)
        ellipse(d, (right+66, top-82, right+103, top-47), BLUE, WOOD_DARK, 4)
        rounded(d, (right+5, top-31, right+71, top-14), 5, (111, 119, 119, 255), INK, 3)
    return im


def texture(im, seed):
    random.seed(seed)
    px = im.load(); w, h = im.size
    for _ in range((w*h)//55):
        x, y = random.randrange(w), random.randrange(h)
        r, g, b, a = px[x, y]
        if a > 20:
            delta = random.randint(-12, 10)
            px[x, y] = (max(0, min(255, r+delta)), max(0, min(255, g+delta)),
                        max(0, min(255, b+delta)), a)
    return im


def save_atlas(name, painter):
    atlas = Image.new("RGBA", (sc(CELL*3), sc(CELL)), (0, 0, 0, 0))
    for level in (1, 2, 3):
        seed = sum((index + 1) * ord(char) for index, char in enumerate(name + str(level)))
        art = texture(painter(level), seed & 0xFFFF)
        atlas.alpha_composite(art, (sc(CELL*(level-1)), 0))
    atlas = atlas.resize((CELL*3, CELL), Image.Resampling.LANCZOS)
    atlas.save(OUT / f"{name}.png", optimize=True)


def foreground():
    im = canvas(1792, 512); d = ImageDraw.Draw(im)
    # Left supply cluster.
    rounded(d, (40, 344, 174, 470), 8, WOOD, WOOD_DARK, 5)
    line(d, [(49, 372), (164, 372)], WOOD_DARK, 3); line(d, [(73, 347), (73, 468)], WOOD_DARK, 3)
    wood(d, (184, 390, 430, 422), light=True); wood(d, (225, 419, 246, 481)); wood(d, (373, 419, 394, 481))
    line(d, [(190, 442), (156, 482)], WOOD_DARK, 5); line(d, [(422, 442), (456, 482)], WOOD_DARK, 5)
    lantern(d, 488, 472, 1.2)
    # Sparse center rocks keep gameplay/readability clear.
    for x, y, rx in ((690, 455, 46), (773, 470, 32), (1005, 464, 40), (1112, 473, 29)):
        ellipse(d, (x-rx, y-rx*.45, x+rx, y+rx*.45), (93, 111, 126, 210), (64, 78, 89, 220), 3)
        snow_cap(d, [(x-rx*.8, y-rx*.22), (x-rx*.25, y-rx*.52), (x+rx*.35, y-rx*.45), (x+rx*.8, y-rx*.18)])
    # Right sled, branches and lantern.
    wood(d, (1338, 398, 1590, 428), light=True); wood(d, (1382, 346, 1561, 373))
    for x in (1404, 1533): wood(d, (x, 365, x+18, 448))
    line(d, [(1350, 448), (1320, 480), (1588, 480), (1617, 447)], WOOD_DARK, 7)
    lantern(d, 1278, 474, 1.15)
    line(d, [(1640, 476), (1713, 332)], WOOD_DARK, 8); line(d, [(1672, 420), (1744, 384)], WOOD_DARK, 5)
    for x, y in ((1695, 363), (1722, 390), (1658, 436), (1750, 374)):
        poly(d, [(x, y), (x+28, y-9), (x+10, y+15)], (48, 91, 83, 255), WOOD_DARK, 2)
    snow_cap(d, [(1624, 470), (1670, 450), (1710, 466), (1755, 444), (1788, 469), (1780, 489), (1634, 489)])
    return im.resize((1792, 512), Image.Resampling.LANCZOS)


def soft_effect(name, color, smoke=False):
    im = canvas(); d = ImageDraw.Draw(im)
    if smoke:
        for i, (x, y, r, a) in enumerate(((255,390,54,70),(226,325,72,62),(287,262,91,48),(236,185,104,34))):
            ellipse(d, (x-r, y-r, x+r, y+r), (*color[:3], a), None)
        im = im.filter(ImageFilter.GaussianBlur(sc(22)))
    else:
        cx = cy = sc(256)
        for r in range(sc(210), 4, -5):
            t = 1-r/sc(210); a = int(3 + 74*t*t)
            d.ellipse((cx-r, cy-r, cx+r, cy+r), fill=(*color[:3], a))
        im = im.filter(ImageFilter.GaussianBlur(sc(12)))
    im.resize((512,512), Image.Resampling.LANCZOS).save(OUT / f"{name}.png", optimize=True)


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    save_atlas("camp_bonfire_atlas", draw_bonfire)
    save_atlas("camp_workshop_atlas", draw_workshop)
    save_atlas("camp_companion_hut_atlas", draw_companion_hut)
    save_atlas("camp_library_atlas", draw_library)
    save_atlas("camp_lookout_atlas", draw_lookout)
    foreground().save(OUT / "camp_foreground_props.png", optimize=True)
    soft_effect("camp_smoke_soft", (220, 230, 231), smoke=True)
    soft_effect("camp_glow_soft", (255, 181, 70), smoke=False)
    print(f"Generated premium camp assets in {OUT}")


if __name__ == "__main__":
    main()
