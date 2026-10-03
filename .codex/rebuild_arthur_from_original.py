from pathlib import Path
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "Assets/_Sprite/Arthur/Arthur_4Direction_48px.png"
OUT = ROOT / "Assets/_Sprite/Arthur8DirectionV2"
FRAMES = OUT / "Frames"
CELL = 48

DIRECTIONS = [
    "Down", "DownRight", "Right", "UpRight",
    "Up", "UpLeft", "Left", "DownLeft",
]

# Every direction is based on one of the four original rows. Diagonals retain
# the original pixels and only shift scanlines by at most one pixel.
BASE = {
    "Down": ("Down", 0),
    "DownRight": ("Down", 1),
    "Right": ("Right", 0),
    "UpRight": ("Up", 1),
    "Up": ("Up", 0),
    "UpLeft": ("Up", -1),
    "Left": ("Left", 0),
    "DownLeft": ("Down", -1),
}
ROWS = {"Down": 0, "Right": 1, "Up": 2, "Left": 3}


def crop_original(sheet, direction, frame):
    x = frame * CELL
    y = ROWS[direction] * CELL
    return sheet.crop((x, y, x + CELL, y + CELL))


def diagonal_from_original(image, sign):
    if sign == 0:
        return image.copy()
    out = Image.new("RGBA", image.size, (0, 0, 0, 0))
    # Shift the upper half one pixel; keep the feet anchored. This changes the
    # pose cue without repainting the character, palette, face, or clothes.
    for y in range(CELL):
        shift = sign if y < CELL // 2 else 0
        row = image.crop((0, y, CELL, y + 1))
        out.alpha_composite(row, (shift, y))
    return out


def idle_breath(image, head_dy):
    """Keep Arthur's planted stance fixed and move only the head very subtly."""
    if head_dy == 0:
        return image.copy()
    out = Image.new("RGBA", image.size, (0, 0, 0, 0))
    out.alpha_composite(image.crop((0, 24, CELL, CELL)), (0, 24))
    out.alpha_composite(image.crop((0, 0, CELL, 24)), (0, head_dy))
    return out


def save_sheet(rows, cols, path):
    sheet = Image.new("RGBA", (cols * CELL, len(rows) * CELL), (0, 0, 0, 0))
    for y, row in enumerate(rows):
        for x, frame in enumerate(row):
            sheet.alpha_composite(frame, (x * CELL, y * CELL))
    sheet.save(path)


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    FRAMES.mkdir(parents=True, exist_ok=True)
    sheet = Image.open(SOURCE).convert("RGBA")
    if sheet.size != (CELL * 4, CELL * 4):
        raise ValueError(f"Expected 192x192 source, got {sheet.size}")

    idle_rows = []
    walk_rows = []
    generated_walk = {}
    idle_head_offsets = [0, 0, 1, 0]
    walk_sequence = [0, 1, 2, 3, 2, 1]

    for direction in DIRECTIONS:
        base_direction, diagonal_sign = BASE[direction]
        # Frame 00 is the planted neutral pose. Frame 01 is a stepping pose and
        # must never be used for idle because it looks like movement anticipation.
        standing = diagonal_from_original(crop_original(sheet, base_direction, 0), diagonal_sign)
        idle = [idle_breath(standing, dy) for dy in idle_head_offsets]
        walk = [
            diagonal_from_original(crop_original(sheet, base_direction, source_frame), diagonal_sign)
            for source_frame in walk_sequence
        ]
        idle_rows.append(idle)
        walk_rows.append(walk)
        generated_walk[direction] = walk
        for i, frame in enumerate(idle):
            frame.save(FRAMES / f"Arthur8_Idle_{direction}_{i:02d}.png")
        for i, frame in enumerate(walk):
            frame.save(FRAMES / f"Arthur8_Walk_{direction}_{i:02d}.png")

    # Keep the existing filenames so Unity retains their .meta GUIDs.
    save_sheet(idle_rows, 4, OUT / "Arthur8_Idle_4frames_48x64.png")
    save_sheet(walk_rows, 6, OUT / "Arthur8_Walk_6frames_48x64.png")

    preview = Image.new("RGBA", (CELL * 8, CELL), (32, 35, 39, 255))
    for i, row in enumerate(idle_rows):
        preview.alpha_composite(row[0], (i * CELL, 0))
    preview.resize((preview.width * 4, preview.height * 4), Image.Resampling.NEAREST).save(
        OUT / "Arthur8_Directions_Preview.png"
    )

    gif_frames = []
    for phase in range(6):
        grid = Image.new("RGBA", (CELL * 4, CELL * 2), (32, 35, 39, 255))
        for index, direction in enumerate(DIRECTIONS):
            grid.alpha_composite(generated_walk[direction][phase], ((index % 4) * CELL, (index // 4) * CELL))
        gif_frames.append(grid.resize((grid.width * 3, grid.height * 3), Image.Resampling.NEAREST))
    gif_frames[0].save(
        OUT / "Arthur8_Walk_Preview.gif",
        save_all=True,
        append_images=gif_frames[1:],
        duration=110,
        loop=0,
        disposal=2,
    )
    print("Rebuilt Arthur from original sprite: 32 idle + 48 walk frames")


if __name__ == "__main__":
    main()
