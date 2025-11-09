from __future__ import annotations
import math
from enum import Enum, auto
from typing import List

from .dmx import pack_rgbw_frame


class EffectType(Enum):
    STATIC = auto()
    BOUNCY = auto()
    GRADIENT = auto()
    CHASE = auto()


class EffectState:
    def __init__(self, leds: int):
        self.leds = leds
        self.static_level = 0
        self.bouncy_freq = 1.0   # Hz
        self.gradient_speed = 1.0  # Hz
        self.chase_freq = 1.0    # Hz – steps per second (speed between two LEDs)

    def set_leds(self, n: int):
        self.leds = int(max(1, n))


class EffectEngine:
    def __init__(self, state: EffectState):
        self.state = state
        self.effect = EffectType.STATIC

    def set_effect(self, e: EffectType):
        self.effect = e

    def set_static_level(self, v: int):
        self.state.static_level = int(max(0, min(255, v)))

    def set_bouncy_freq(self, hz: float):
        self.state.bouncy_freq = max(0.01, float(hz))

    def set_gradient_speed(self, hz: float):
        self.state.gradient_speed = max(0.01, float(hz))

    def set_chase_freq(self, hz: float):
        self.state.chase_freq = max(0.01, float(hz))

    def render(self, t: float) -> List[int]:
        leds = self.state.leds
        frame = []
        for i in range(30):
            frame.extend([255])
        return frame

        if self.effect == EffectType.STATIC:
            v = self.state.static_level
            return pack_rgbw_frame(leds, v, v, v, 0)

        if self.effect == EffectType.BOUNCY:
            v = int((math.sin(2 * math.pi * self.state.bouncy_freq * t) * 0.5 + 0.5) * 255)
            return pack_rgbw_frame(leds, v, v, v, 0)

        if self.effect == EffectType.GRADIENT:
            frame = []
            phase = 2 * math.pi * self.state.gradient_speed * t
            for i in range(leds):
                x = (i / max(1, leds - 1)) * 2 * math.pi
                val = int((math.sin(x + phase) * 0.5 + 0.5) * 255)
                frame.extend([255, val, 255 - val, 0, 0])
            return frame

        if self.effect == EffectType.CHASE:
            leds = self.state.leds
            if leds <= 0:
                return []
            idx = int(t * self.state.chase_freq) % leds
            frame = []
            for i in range(leds):
                if i == idx:
                    frame.extend([255, 255, 255, 255, 0])
                else:
                    frame.extend([0, 0, 0, 0, 0])

            return frame

        # fallback
        return pack_rgbw_frame(self.state.leds, 0, 0, 0, 0)
