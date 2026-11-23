#!/usr/bin/env python3
"""Generate synthetic HUD icon templates and a 1920x1080 fixture for recognition tests.

Uses only the Python standard library (zlib + struct) so it runs without Pillow.
"""

from __future__ import annotations

import json
import struct
import zlib
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
TEMPLATES = ROOT / "assets" / "templates"
FIXTURES = ROOT / "assets" / "fixtures"

# Must match HudLayoutPresets.SyntheticFullHd
PLAYER_ROI = (820, 12, 64, 64)
ALLY0_ROI = (740, 12, 64, 64)
ALLY1_ROI = (660, 12, 64, 64)
ENEMY0_ROI = (1000, 12, 64, 64)
ENEMY1_ROI = (1080, 12, 64, 64)
INV0_ROI = (860, 960, 64, 64)
INV1_ROI = (932, 960, 64, 64)


def png_chunk(tag: bytes, data: bytes) -> bytes:
    return struct.pack(">I", len(data)) + tag + data + struct.pack(">I", zlib.crc32(tag + data) & 0xFFFFFFFF)


def write_png(path: Path, width: int, height: int, rgba: bytes) -> None:
    assert len(rgba) == width * height * 4
    raw = b"".join(b"\x00" + rgba[y * width * 4 : (y + 1) * width * 4] for y in range(height))
    ihdr = struct.pack(">IIBBBBB", width, height, 8, 6, 0, 0, 0)
    png = b"\x89PNG\r\n\x1a\n" + png_chunk(b"IHDR", ihdr) + png_chunk(b"IDAT", zlib.compress(raw, 9)) + png_chunk(b"IEND", b"")
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_bytes(png)


def solid(w: int, h: int, r: int, g: int, b: int) -> bytearray:
    buf = bytearray(w * h * 4)
    for i in range(0, len(buf), 4):
        buf[i : i + 4] = bytes((r, g, b, 255))
    return buf


def paint_rect(buf: bytearray, tw: int, x0: int, y0: int, x1: int, y1: int, r: int, g: int, b: int) -> None:
    for y in range(y0, y1):
        for x in range(x0, x1):
            i = (y * tw + x) * 4
            buf[i : i + 4] = bytes((r, g, b, 255))


def make_pudge(size: int = 64) -> bytes:
    buf = solid(size, size, 120, 40, 30)
    paint_rect(buf, size, 8, 8, 56, 56, 180, 60, 40)
    paint_rect(buf, size, 20, 18, 44, 42, 220, 200, 80)
    paint_rect(buf, size, 28, 26, 36, 50, 40, 20, 20)
    return bytes(buf)


def make_invoker(size: int = 64) -> bytes:
    buf = solid(size, size, 20, 40, 120)
    paint_rect(buf, size, 6, 6, 58, 58, 40, 90, 200)
    paint_rect(buf, size, 16, 16, 32, 48, 220, 80, 40)
    paint_rect(buf, size, 34, 16, 50, 48, 80, 200, 220)
    return bytes(buf)


def make_shadow_fiend(size: int = 64) -> bytes:
    buf = solid(size, size, 30, 10, 10)
    paint_rect(buf, size, 8, 8, 56, 56, 90, 20, 20)
    paint_rect(buf, size, 18, 22, 30, 46, 200, 40, 40)
    paint_rect(buf, size, 34, 22, 46, 46, 200, 40, 40)
    paint_rect(buf, size, 24, 8, 40, 20, 240, 220, 60)
    return bytes(buf)


def make_queen_of_pain(size: int = 64) -> bytes:
    buf = solid(size, size, 60, 10, 70)
    paint_rect(buf, size, 10, 10, 54, 54, 140, 30, 160)
    paint_rect(buf, size, 22, 18, 42, 48, 240, 120, 200)
    paint_rect(buf, size, 28, 6, 36, 18, 255, 220, 240)
    return bytes(buf)


def make_blink(size: int = 64) -> bytes:
    buf = solid(size, size, 20, 80, 40)
    paint_rect(buf, size, 10, 10, 54, 54, 40, 180, 90)
    paint_rect(buf, size, 22, 18, 42, 46, 200, 255, 180)
    paint_rect(buf, size, 30, 8, 34, 56, 255, 255, 255)
    return bytes(buf)


def make_bottle(size: int = 64) -> bytes:
    buf = solid(size, size, 20, 60, 90)
    paint_rect(buf, size, 18, 8, 46, 56, 60, 160, 200)
    paint_rect(buf, size, 24, 16, 40, 48, 180, 230, 255)
    paint_rect(buf, size, 28, 4, 36, 14, 140, 140, 150)
    return bytes(buf)


def blit(dst: bytearray, dw: int, sx: int, sy: int, src: bytes, sw: int, sh: int) -> None:
    for y in range(sh):
        for x in range(sw):
            si = (y * sw + x) * 4
            di = ((sy + y) * dw + (sx + x)) * 4
            dst[di : di + 4] = src[si : si + 4]


def main() -> None:
    icons = {
        "heroes/pudge.png": make_pudge(),
        "heroes/invoker.png": make_invoker(),
        "heroes/shadow_fiend.png": make_shadow_fiend(),
        "heroes/queen_of_pain.png": make_queen_of_pain(),
        "items/blink_dagger.png": make_blink(),
        "items/bottle.png": make_bottle(),
    }
    for rel, data in icons.items():
        write_png(TEMPLATES / rel, 64, 64, data)

    catalog = {
        "heroes": [
            {"id": "pudge", "name_ru": "Pudge", "file": "heroes/pudge.png"},
            {"id": "invoker", "name_ru": "Invoker", "file": "heroes/invoker.png"},
            {"id": "shadow_fiend", "name_ru": "Shadow Fiend", "file": "heroes/shadow_fiend.png"},
            {"id": "queen_of_pain", "name_ru": "Queen of Pain", "file": "heroes/queen_of_pain.png"},
        ],
        "items": [
            {"id": "blink_dagger", "name_ru": "Blink Dagger", "file": "items/blink_dagger.png"},
            {"id": "bottle", "name_ru": "Bottle", "file": "items/bottle.png"},
        ],
    }
    (TEMPLATES / "catalog.json").write_text(
        json.dumps(catalog, ensure_ascii=False, indent=2) + "\n", encoding="utf-8"
    )

    # Dark HUD canvas with icons placed at known ROIs
    w, h = 1920, 1080
    canvas = solid(w, h, 18, 20, 24)
    paint_rect(canvas, w, 0, 0, w, 88, 28, 30, 36)
    paint_rect(canvas, w, 800, 900, 1120, 1080, 24, 26, 30)

    blit(canvas, w, PLAYER_ROI[0], PLAYER_ROI[1], icons["heroes/pudge.png"], 64, 64)
    blit(canvas, w, ALLY0_ROI[0], ALLY0_ROI[1], icons["heroes/invoker.png"], 64, 64)
    blit(canvas, w, ALLY1_ROI[0], ALLY1_ROI[1], icons["heroes/shadow_fiend.png"], 64, 64)
    blit(canvas, w, ENEMY0_ROI[0], ENEMY0_ROI[1], icons["heroes/queen_of_pain.png"], 64, 64)
    blit(canvas, w, ENEMY1_ROI[0], ENEMY1_ROI[1], icons["heroes/invoker.png"], 64, 64)
    blit(canvas, w, INV0_ROI[0], INV0_ROI[1], icons["items/blink_dagger.png"], 64, 64)
    blit(canvas, w, INV1_ROI[0], INV1_ROI[1], icons["items/bottle.png"], 64, 64)

    fixture = FIXTURES / "match_hud_1920x1080.png"
    write_png(fixture, w, h, bytes(canvas))

    expected = {
        "hero_id": "pudge",
        "ally_ids": ["invoker", "shadow_fiend"],
        "enemy_ids": ["queen_of_pain", "invoker"],
        "inventory_ids": ["blink_dagger", "bottle"],
        "demo_opponent_id": "invoker",
        "demo_level": 5,
        "layout": "synthetic-fullhd-1080",
        "resolution": [1920, 1080],
        "note_ru": "Синтетический HUD для тестов статуса распознавания (не скриншот реальной Dota).",
    }
    (FIXTURES / "match_hud_expected.json").write_text(
        json.dumps(expected, ensure_ascii=False, indent=2) + "\n", encoding="utf-8"
    )
    print(f"Wrote templates under {TEMPLATES}")
    print(f"Wrote fixture {fixture}")


if __name__ == "__main__":
    main()
