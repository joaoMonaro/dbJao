"""Finaliza a referência frontal gerada; requer Pillow."""

from pathlib import Path

from PIL import Image, ImageDraw


HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
DESTINATION = ROOT / "assets/frame/goku/idle/idle_front_normal.png"

source = Image.open(HERE / "generated.png").convert("RGBA")
# Descarta o ruído quase transparente deixado fora da silhueta pela geração.
alpha = source.getchannel("A").point(lambda value: 255 if value >= 128 else 0)
source.putalpha(alpha)
bounds = alpha.getbbox()
if bounds is None:
    raise ValueError("A imagem gerada não contém uma silhueta visível.")

# Ajusta a composição ao alvo de produção, sem interpolação das cores.
sprite = source.crop(bounds).resize((64, 128), Image.Resampling.NEAREST)
alpha = sprite.getchannel("A")
rgb = Image.new("RGB", sprite.size, (0, 0, 0))
rgb.paste(sprite, mask=alpha)
sprite = rgb.quantize(
    colors=32, method=Image.Quantize.MEDIANCUT, dither=Image.Dither.NONE
).convert("RGBA")
sprite.putalpha(alpha)

canvas = Image.new("RGBA", (192, 192), (0, 0, 0, 0))
canvas.paste(sprite, (64, 32))
actual_bounds = canvas.getchannel("A").getbbox()
if actual_bounds != (64, 32, 128, 160):
    raise ValueError(f"Silhueta fora das medidas esperadas: {actual_bounds}")
DESTINATION.parent.mkdir(parents=True, exist_ok=True)
canvas.save(DESTINATION)

# Comparação de escala e alinhamento; não é um asset usado pelo jogo.
pilaf = Image.open(ROOT / "assets/frame/pilaf/idle/idle_front.png").convert("RGBA")
comparison = Image.new("RGB", (384, 216), (35, 40, 51))
comparison.paste(pilaf, (0, 0), pilaf)
comparison.paste(canvas, (192, 0), canvas)
draw = ImageDraw.Draw(comparison)
draw.line((0, 160, 383, 160), fill=(88, 98, 111))
draw.text((62, 196), "PEQUENO 36x68", fill=(222, 227, 234))
draw.text((246, 196), "NORMAL 64x128", fill=(222, 227, 234))
comparison.resize((1152, 648), Image.Resampling.NEAREST).save(HERE / "comparison.png")

print(f"PNG: {DESTINATION.relative_to(ROOT)}")
print(f"Canvas: {canvas.size}; limites alpha (exclusivos): {actual_bounds}")
print(f"Alpha: {sorted(value for _, value in canvas.getchannel('A').getcolors())}")
print(f"Cores visíveis: {sum(1 for _, color in canvas.getcolors() if color[3])}")
