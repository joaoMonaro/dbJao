"""Finaliza as sheets geradas do Goku usando a paleta da referência frontal."""

from collections import deque
from pathlib import Path
import json

from PIL import Image, ImageDraw


HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
ASSETS = ROOT / "assets"
REFERENCE = ASSETS / "frame/goku/idle/idle_front_normal.png"
idle = Image.open(REFERENCE).convert("RGBA")
colors = sorted(color[:3] for _, color in idle.getcolors() if color[3])
palette = Image.new("P", (1, 1))
palette.putpalette([channel for color in colors for channel in color]
                   + list(colors[0]) * (256 - len(colors)))


def clean_alpha(image):
    """Retém o corpo conectado e remove ruído transparente da geração."""
    alpha = image.getchannel("A")
    points = {(x, y) for y in range(image.height) for x in range(image.width)
              if alpha.getpixel((x, y)) >= 128}
    largest = set()
    while points:
        seed = points.pop()
        component = {seed}
        queue = deque([seed])
        while queue:
            x, y = queue.popleft()
            for dx, dy in ((-1, -1), (0, -1), (1, -1), (-1, 0),
                           (1, 0), (-1, 1), (0, 1), (1, 1)):
                neighbor = (x + dx, y + dy)
                if neighbor in points:
                    points.remove(neighbor)
                    component.add(neighbor)
                    queue.append(neighbor)
        if len(component) > len(largest):
            largest = component
    if not largest:
        raise ValueError("Quadro sem personagem visível")
    mask = Image.new("L", image.size)
    for point in largest:
        mask.putpixel(point, 255)
    image.putalpha(mask)
    return image


def make_frames(name, columns, scale, anchors):
    source = Image.open(HERE / f"{name}-generated.png").convert("RGBA")
    cell_width, cell_height = source.width // columns, source.height // 2
    frames = []
    for index, (hip_x, feet_y) in enumerate(anchors):
        x, y = index % columns * cell_width, index // columns * cell_height
        cell = clean_alpha(source.crop((x, y, x + cell_width, y + cell_height)))
        # Uma escala fixa por sheet preserva as diferenças intencionais das poses.
        cell = cell.resize((round(cell_width * scale), round(cell_height * scale)),
                           Image.Resampling.NEAREST)
        alpha = cell.getchannel("A")
        cell = cell.convert("RGB").quantize(palette=palette, dither=Image.Dither.NONE)
        cell = cell.convert("RGBA")
        cell.putalpha(alpha)
        cell = clean_alpha(cell)
        canvas = Image.new("RGBA", (192, 192))
        # Os quadris, não o centro da silhueta, determinam o alinhamento horizontal.
        offset = (96 - round(hip_x * scale), 160 - round((feet_y + 1) * scale))
        bounds = cell.getchannel("A").getbbox()
        # Corrige o arredondamento do nearest na sola, sem alterar a escala.
        offset = (offset[0], 160 - bounds[3])
        if (bounds[0] + offset[0] < 0 or bounds[1] + offset[1] < 0
                or bounds[2] + offset[0] > 192 or bounds[3] + offset[1] > 192):
            raise ValueError(f"{name} quadro {index + 1} cortado pelo canvas")
        canvas.paste(cell, offset)
        frames.append(canvas)
    return frames


walk = make_frames("walk", 2, 0.26,
                   [(384, 586), (292, 586), (384, 535), (282, 535)])
attack = make_frames("attack", 3, 0.29,
                     [(256, 463), (256, 463), (224, 463),
                      (252, 467), (260, 467), (230, 467)])
animations = {"idle": [idle], "walk": walk, "attack": attack}
destinations = {
    "idle": ASSETS / "frame/goku/idle/idle_front.png",
    "walk": ASSETS / "sheets/goku/walk/player_walk_sheet.png",
    "attack": ASSETS / "sheets/goku/attack/attack.png",
}
manifest = {"canvas": [192, 192], "feet_anchor": [96, 159],
            "palette": [list(color) for color in colors], "animations": {}}
for name, frames in animations.items():
    sheet = Image.new("RGBA", (192 * len(frames), 192))
    frame_info = []
    for index, frame in enumerate(frames):
        sheet.paste(frame, (192 * index, 0))
        frame_info.append({"frame": index,
                           "alpha_bounds_exclusive": frame.getchannel("A").getbbox()})
    sheet.save(destinations[name])
    manifest["animations"][name] = {
        "file": str(destinations[name].relative_to(ROOT)),
        "fps": {"idle": 1, "walk": 8, "attack": 10}[name],
        "loop": name != "attack", "frames": frame_info,
    }

(HERE / "manifest.json").write_text(json.dumps(manifest, indent=2) + "\n")

overview = Image.new("RGB", (1152, 648), (35, 40, 51))
draw = ImageDraw.Draw(overview)
for row, (name, frames) in enumerate(animations.items()):
    for index, frame in enumerate(frames):
        x, y = index * 192, row * 216
        overview.paste(frame, (x, y), frame)
        draw.line((x, y + 160, x + 191, y + 160), fill=(80, 90, 100))
        draw.text((x + 60, y + 192), f"{name.upper()} {index + 1}", fill="white")
overview.save(HERE / "overview.png")

preview_frames, durations = [], []
pilaf = Image.open(ASSETS / "frame/pilaf/idle/idle_front.png").convert("RGBA")
sequence = [("IDLE", idle, 1000)]
sequence += [("WALK", frame, 125) for _ in range(4) for frame in walk]
sequence += [("IDLE", idle, 500)]
sequence += [("ATTACK", frame, 100) for _ in range(3) for frame in attack]
for label, frame, duration in sequence:
    preview = Image.new("RGB", (384, 216), (35, 40, 51))
    preview.paste(pilaf, (0, 0), pilaf)
    preview.paste(frame, (192, 0), frame)
    draw = ImageDraw.Draw(preview)
    draw.line((0, 160, 383, 160), fill=(80, 90, 100))
    draw.text((68, 194), "PEQUENO", fill="white")
    draw.text((263, 194), label, fill="white")
    preview_frames.append(preview.resize((768, 432), Image.Resampling.NEAREST))
    durations.append(duration)
preview_frames[0].save(HERE / "preview.gif", save_all=True,
                       append_images=preview_frames[1:], duration=durations,
                       loop=0, disposal=2)
print(json.dumps(manifest["animations"], indent=2))
