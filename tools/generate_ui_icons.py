"""Generate the two UI icons the result card still lacked.

The result card drew its three stat icons as raw Unicode glyphs -- U+279E, U+2740,
U+231B. None of them exist in the project's TMP font atlas, so on device they
rendered as tofu boxes and a stray asterisk. UiIcons already covers the third one
(Timer); these are the missing distance and fish marks.

Style matches the existing set: 100x100 RGBA, pure white silhouette on
transparent, stroke weights in the 7-9 px range so they read at 36 px.

Run:  python tools/generate_ui_icons.py
"""

import os

from PIL import Image, ImageDraw

SIZE = 100
WHITE = (255, 255, 255, 255)
OUT_DIR = os.path.join(
    os.path.dirname(os.path.dirname(os.path.abspath(__file__))),
    "Assets", "Resources", "PenguinRun", "UiIcons",
)


def new_canvas():
    image = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    return image, ImageDraw.Draw(image)


def make_distance(name):
    """A pennant on a pole -- the run already uses a flag for the end marker."""
    image, draw = new_canvas()
    # Pole.
    draw.line([(30, 14), (30, 90)], fill=WHITE, width=8)
    # Pennant, pointing the way the runner travels.
    draw.polygon([(34, 20), (84, 38), (34, 56)], fill=WHITE)
    # Ground tick, so the mark reads as a distance marker rather than a banner.
    draw.line([(16, 90), (52, 90)], fill=WHITE, width=8)
    image.save(os.path.join(OUT_DIR, name))


def make_fish(name):
    """Fish jerky -- the run currency."""
    image, draw = new_canvas()
    # Body.
    draw.ellipse([18, 34, 74, 68], fill=WHITE)
    # Tail.
    draw.polygon([(72, 51), (92, 34), (92, 68)], fill=WHITE)
    # Eye, punched back out so it stays visible after the sprite is tinted.
    draw.ellipse([30, 44, 40, 54], fill=(0, 0, 0, 0))
    image.save(os.path.join(OUT_DIR, name))


def main():
    os.makedirs(OUT_DIR, exist_ok=True)
    make_distance("icon_distance.png")
    make_fish("icon_fish.png")
    for name in ("icon_distance.png", "icon_fish.png"):
        path = os.path.join(OUT_DIR, name)
        print(f"{name:22s} {os.path.getsize(path):6d} bytes")


if __name__ == "__main__":
    main()
