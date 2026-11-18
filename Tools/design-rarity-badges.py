"""Create the four original rank badges for the Unity gallery."""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[1]
OUTPUT = ROOT / "Assets/DragonIdle/Resources/Interface/Rarity"
FONT = Path("C:/Windows/Fonts/arialbd.ttf")
SIZE = (256, 192)
CENTER = (128, 94)


def facet(canvas, vertices, top, bottom):
    mask = Image.new("L", SIZE)
    ImageDraw.Draw(mask).polygon(vertices, fill=255)
    layer = Image.new("RGBA", SIZE)
    brush = ImageDraw.Draw(layer)
    for y in range(SIZE[1]):
        t = y / (SIZE[1] - 1)
        color = tuple(round(x + (z - x) * t) for x, z in zip(top, bottom))
        brush.line((0, y, SIZE[0], y), fill=color)
    canvas.paste(layer, (0, 0), mask)


def spark(draw, x, y, radius, color):
    draw.polygon([(x, y-radius), (x+radius//4, y-2), (x+radius, y),
                  (x+radius//4, y+2), (x, y+radius), (x-radius//4, y+2),
                  (x-radius, y), (x-radius//4, y-2)], fill=color)


def badge(rank):
    level = "CBAS".index(rank)
    metal, shade, face = [
        ((207, 225, 228), (92, 122, 133), "#203A45"),
        ((154, 249, 250), (30, 141, 181), "#104159"),
        ((241, 211, 255), (123, 53, 210), "#392063"),
        ((255, 244, 185), (199, 104, 17), "#563319"),
    ][level]
    img = Image.new("RGBA", SIZE)
    draw = ImageDraw.Draw(img)
    if level >= 2:
        for direction in [-1, 1]:
            width = 104 if level == 3 else 91
            for i in range(level):
                x = 128 + direction * width
                y = 55 + i * 20
                draw.polygon([(128 + direction*48, y+17), (x, y),
                              (x-direction*16, y+25), (128+direction*54, y+30)],
                             fill=shade, outline=metal, width=2)
    if level == 3:
        draw.arc((39, 9, 217, 179), 200, 338, fill="#FFF1A4", width=3)
        draw.arc((39, 9, 217, 179), 21, 159, fill="#B97124", width=3)
        for x, y, radius in [(34, 35, 10), (222, 36, 9), (215, 146, 7), (41, 146, 7), (128, 14, 12)]:
            spark(draw, x, y, radius, "#FFF3A9")
    if level == 0:
        rim = [(128, 28), (185, 50), (185, 119), (128, 158), (71, 119), (71, 50)]
        inner = [(128, 40), (174, 58), (174, 113), (128, 146), (82, 113), (82, 58)]
    elif level == 1:
        rim = [(128, 19), (194, 60), (194, 124), (128, 169), (62, 124), (62, 60)]
        inner = [(128, 33), (181, 66), (181, 117), (128, 154), (75, 117), (75, 66)]
    else:
        rim = [(128, 22), (188, 46), (201, 72), (187, 132), (128, 172),
               (69, 132), (55, 72), (68, 46)]
        inner = [(128, 37), (178, 57), (186, 75), (174, 125), (128, 154),
                 (82, 125), (70, 75), (78, 57)]
    draw.polygon([(x, y+5) for x, y in rim], fill="#131728")
    facet(img, rim, metal, shade)
    draw.polygon(inner, fill=face, outline=metal, width=2)
    if level:
        draw.line((85, 69, 128, 53, 171, 69), fill=shade, width=3)
        spark(draw, 128, 142, 6 + level, metal)
    if level >= 2:
        for x in [60, 196]:
            spark(draw, x, 118, 10 if level == 2 else 13, metal)
    font = ImageFont.truetype(str(FONT), 85 if level >= 2 else 82)
    draw.text((CENTER[0]+2, CENTER[1]+4), rank, font=font, anchor="mm",
              fill="#0B1021", stroke_width=4, stroke_fill="#0B1021")
    draw.text(CENTER, rank, font=font, anchor="mm", fill=metal,
              stroke_width=2, stroke_fill="#FFF4D5" if level == 3 else metal)
    return img


if __name__ == "__main__":
    OUTPUT.mkdir(parents=True, exist_ok=True)
    for rank in "CBAS":
        badge(rank).save(OUTPUT / f"{rank}.png")
