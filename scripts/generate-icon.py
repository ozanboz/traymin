from __future__ import annotations

from io import BytesIO
from pathlib import Path
import struct

from PIL import Image, ImageDraw

SIZES = (16, 20, 24, 32, 48, 256)
SUPERSAMPLE = 4
BLUE = "#246BFD"
WHITE = "#FFFFFF"
MINT = "#78F0D0"
EDGE = "#D9E7FF"


def rounded_line(draw: ImageDraw.ImageDraw, points, fill: str, width: int) -> None:
    draw.line(points, fill=fill, width=width, joint="curve")
    radius = width // 2
    for x, y in (points[0], points[-1]):
        draw.ellipse((x - radius, y - radius, x + radius, y + radius), fill=fill)


def render(size: int) -> Image.Image:
    n = size * SUPERSAMPLE
    image = Image.new("RGBA", (n, n), (0, 0, 0, 0))
    draw = ImageDraw.Draw(image)

    margin = round(n * 0.07)
    radius = round(n * 0.22)
    border = max(SUPERSAMPLE, round(n * 0.055))
    draw.rounded_rectangle(
        (margin, margin, n - margin - 1, n - margin - 1),
        radius=radius,
        fill=BLUE,
        outline=EDGE,
        width=border,
    )

    t_width = max(2 * SUPERSAMPLE, round(n * 0.135))
    rounded_line(
        draw,
        [(round(n * 0.29), round(n * 0.28)), (round(n * 0.71), round(n * 0.28))],
        WHITE,
        t_width,
    )
    rounded_line(
        draw,
        [(round(n * 0.50), round(n * 0.28)), (round(n * 0.50), round(n * 0.64))],
        WHITE,
        t_width,
    )

    tray_width = max(SUPERSAMPLE, round(n * 0.09))
    rounded_line(
        draw,
        [
            (round(n * 0.26), round(n * 0.69)),
            (round(n * 0.37), round(n * 0.82)),
            (round(n * 0.63), round(n * 0.82)),
            (round(n * 0.74), round(n * 0.69)),
        ],
        MINT,
        tray_width,
    )

    return image.resize((size, size), Image.Resampling.LANCZOS)


def png_bytes(image: Image.Image) -> bytes:
    buffer = BytesIO()
    image.save(buffer, format="PNG", optimize=True)
    return buffer.getvalue()


def write_ico(path: Path) -> None:
    frames = [(size, png_bytes(render(size))) for size in SIZES]
    directory_size = 6 + 16 * len(frames)
    offset = directory_size
    entries = []
    payloads = []

    for size, payload in frames:
        dimension = 0 if size == 256 else size
        entries.append(
            struct.pack(
                "<BBBBHHII",
                dimension,
                dimension,
                0,
                0,
                1,
                32,
                len(payload),
                offset,
            )
        )
        payloads.append(payload)
        offset += len(payload)

    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_bytes(
        struct.pack("<HHH", 0, 1, len(frames))
        + b"".join(entries)
        + b"".join(payloads)
    )


def main() -> None:
    root = Path(__file__).resolve().parents[1]
    output = root / "src" / "TrayMin" / "Assets" / "traymin.ico"
    preview = root / "docs" / "assets" / "traymin-icon.png"
    write_ico(output)
    preview.parent.mkdir(parents=True, exist_ok=True)
    render(256).save(preview, format="PNG", optimize=True)
    print(f"wrote {output} with sizes {list(SIZES)}")
    print(f"wrote {preview}")


if __name__ == "__main__":
    main()
