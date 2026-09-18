"""Finaliza as animações de Sidra no porte normal; requer Pillow."""

from collections import deque
import json
from pathlib import Path

from PIL import Image, ImageDraw


HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
ASSETS = ROOT / "assets"
SIZE = 192
ALPHA_THRESHOLD = 128


def largest_silhouette(image: Image.Image) -> Image.Image:
    """Descarta o halo da geração e fragmentos desconectados do corpo."""
    alpha = image.getchannel("A")
    remaining = {
        (x, y)
        for y in range(image.height)
        for x in range(image.width)
        if alpha.getpixel((x, y)) >= ALPHA_THRESHOLD
    }
    largest: set[tuple[int, int]] = set()
    while remaining:
        start = remaining.pop()
        component = {start}
        pending = deque([start])
        while pending:
            x, y = pending.popleft()
            for dx, dy in ((-1, -1), (0, -1), (1, -1), (-1, 0),
                           (1, 0), (-1, 1), (0, 1), (1, 1)):
                neighbor = (x + dx, y + dy)
                if neighbor in remaining:
                    remaining.remove(neighbor)
                    component.add(neighbor)
                    pending.append(neighbor)
        if len(component) > len(largest):
            largest = component
    if not largest:
        raise ValueError("Quadro sem personagem visível")
    mask = Image.new("L", image.size)
    for point in largest:
        mask.putpixel(point, 255)
    image.putalpha(mask)
    return image


def make_idle() -> Image.Image:
    source = largest_silhouette(Image.open(HERE / "idle-generated.png").convert("RGBA"))
    sprite = source.crop(source.getchannel("A").getbbox()).resize(
        (64, 128), Image.Resampling.NEAREST
    )
    alpha = sprite.getchannel("A")
    rgb = Image.new("RGB", sprite.size, (0, 0, 0))
    rgb.paste(sprite, mask=alpha)
    sprite = rgb.quantize(colors=32, method=Image.Quantize.MEDIANCUT,
                          dither=Image.Dither.NONE).convert("RGBA")
    sprite.putalpha(alpha)
    frame = Image.new("RGBA", (SIZE, SIZE))
    frame.paste(sprite, (64, 32))
    if frame.getchannel("A").getbbox() != (64, 32, 128, 160):
        raise ValueError("Pose frontal fora do padrão normal")
    return frame


idle = make_idle()
colors = sorted(color[:3] for _, color in idle.getcolors() if color[3])
palette = Image.new("P", (1, 1))
palette.putpalette([channel for color in colors for channel in color]
                   + list(colors[0]) * (256 - len(colors)))


def make_sheet_frames(name: str, columns: int, scale: float,
                      hip_xs: list[int]) -> list[Image.Image]:
    source = Image.open(HERE / f"{name}-generated.png").convert("RGBA")
    cell_width, cell_height = source.width // columns, source.height // 2
    if len(hip_xs) != columns * 2:
        raise ValueError(f"Quantidade de âncoras inválida para {name}")
    frames = []
    for index, hip_x in enumerate(hip_xs):
        x, y = index % columns * cell_width, index // columns * cell_height
        cell = largest_silhouette(source.crop((x, y, x + cell_width, y + cell_height)))
        # Escala única por sequência: não amplia uma pose só porque está curvada.
        cell = cell.resize((round(cell_width * scale), round(cell_height * scale)),
                           Image.Resampling.NEAREST)
        alpha = cell.getchannel("A")
        cell = cell.convert("RGB").quantize(palette=palette,
                                            dither=Image.Dither.NONE).convert("RGBA")
        cell.putalpha(alpha)
        cell = largest_silhouette(cell)
        bounds = cell.getchannel("A").getbbox()
        # Os quadris seguem x=96; a sola apoiada termina em y=159.
        offset = (96 - round(hip_x * scale), 160 - bounds[3])
        placed = (bounds[0] + offset[0], bounds[1] + offset[1],
                  bounds[2] + offset[0], bounds[3] + offset[1])
        if min(placed[:2]) < 0 or max(placed[2:]) > SIZE:
            raise ValueError(f"{name} quadro {index + 1} cortado: {placed}")
        frame = Image.new("RGBA", (SIZE, SIZE))
        frame.paste(cell, offset)
        frames.append(frame)
    return frames


walk = make_sheet_frames("walk", 2, 0.239, [415, 338, 415, 350])
attack = make_sheet_frames("attack", 3, 0.296, [256] * 6)
animations = {"idle": [idle], "walk": walk, "attack": attack}
destinations = {
    "idle": ASSETS / "frame/sidra/idle/idle_front.png",
    "walk": ASSETS / "sheets/sidra/walk/walk.png",
    "attack": ASSETS / "sheets/sidra/attack/attack.png",
}
manifest = {"canvas": [SIZE, SIZE], "feet_anchor": [96, 159],
            "palette": [list(color) for color in colors], "animations": {}}
for name, frames in animations.items():
    sheet = Image.new("RGBA", (SIZE * len(frames), SIZE))
    information = []
    for index, frame in enumerate(frames):
        sheet.paste(frame, (SIZE * index, 0))
        information.append({"frame": index,
                            "alpha_bounds_exclusive": frame.getchannel("A").getbbox()})
    destination = destinations[name]
    destination.parent.mkdir(parents=True, exist_ok=True)
    sheet.save(destination)
    manifest["animations"][name] = {
        "file": str(destination.relative_to(ROOT)),
        "fps": {"idle": 1, "walk": 8, "attack": 10}[name],
        "loop": name != "attack", "frames": information,
    }
(HERE / "manifest.json").write_text(json.dumps(manifest, indent=2) + "\n")

overview = Image.new("RGB", (1152, 648), (35, 40, 51))
draw = ImageDraw.Draw(overview)
for row, (name, frames) in enumerate(animations.items()):
    for index, frame in enumerate(frames):
        x, y = index * SIZE, row * 216
        overview.paste(frame, (x, y), frame)
        draw.line((x, y + 160, x + 191, y + 160), fill=(80, 90, 100))
        draw.text((x + 60, y + 192), f"{name.upper()} {index + 1}", fill="white")
overview.save(HERE / "overview.png")

reference = Image.open(ASSETS / "frame/goku/idle/idle_front.png").convert("RGBA")
sequence = [("IDLE", idle, 1000)]
sequence += [("WALK", frame, 125) for _ in range(4) for frame in walk]
sequence += [("IDLE", idle, 500)]
sequence += [("ATTACK", frame, 100) for _ in range(3) for frame in attack]
preview_frames, durations = [], []
for label, frame, duration in sequence:
    preview = Image.new("RGB", (384, 216), (35, 40, 51))
    preview.paste(reference, (0, 0), reference)
    preview.paste(frame, (192, 0), frame)
    preview_draw = ImageDraw.Draw(preview)
    preview_draw.line((0, 160, 383, 160), fill=(80, 90, 100))
    preview_draw.text((70, 194), "GOKU", fill="white")
    preview_draw.text((260, 194), label, fill="white")
    preview_frames.append(preview.resize((768, 432), Image.Resampling.NEAREST))
    durations.append(duration)
preview_frames[0].save(HERE / "preview.gif", save_all=True,
                       append_images=preview_frames[1:], duration=durations,
                       loop=0, disposal=2)
print(json.dumps(manifest["animations"], indent=2))
