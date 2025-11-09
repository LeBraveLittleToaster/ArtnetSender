from __future__ import annotations
from typing import List

CHANNELS_PER_LED = 5  # RGBW


def clamp_byte(v: int) -> int:
    return 0 if v < 0 else 255 if v > 255 else int(v)


def pack_rgbw_frame(leds: int, r: int, g: int, b: int, w: int, dimmer:int = 255) -> List[int]:
    r, g, b, w = map(clamp_byte, (r, g, b, w))
    frame = []
    for _ in range(leds):
        frame.extend([dimmer, r, g, b, w])
    return frame
