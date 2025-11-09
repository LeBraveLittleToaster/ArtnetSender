from __future__ import annotations
from typing import Dict, Any

from .model import ArtNetModel, DmxConfig
from .effects import EffectType

# Player mapping including the new CHASE effect
PLAYER_MAP = {
    1: "SNAP_ON",
    2: "SNAP_OFF",
    3: "BOUNCY",
    4: "GRADIENT",
    5: "CHASE",
}


class Controller:
    def __init__(self, model: ArtNetModel):
        self.model = model

    def apply(self, player_id: int, leds: int, freq: float, addr: int, uni: int) -> Dict[str, Any]:
        # Update addressing and LED count
        self.model.set_dmx_config(DmxConfig(universe=uni, address=addr))
        self.model.restart_for_leds(leds)

        if player_id not in PLAYER_MAP:
            return {"error": f"unknown player_id {player_id}", "allowed": sorted(PLAYER_MAP.keys())}

        label = PLAYER_MAP[player_id]
        if label == "SNAP_ON":
            self.model.set_effect(EffectType.STATIC)
            self.model.set_static_level(255)
        elif label == "SNAP_OFF":
            self.model.set_effect(EffectType.STATIC)
            self.model.set_static_level(0)
        elif label == "BOUNCY":
            self.model.set_effect(EffectType.BOUNCY)
            self.model.set_bouncy_freq(freq)
        elif label == "GRADIENT":
            self.model.set_effect(EffectType.GRADIENT)
            self.model.set_gradient_speed(freq)
        elif label == "CHASE":
            self.model.set_effect(EffectType.CHASE)
            self.model.set_chase_freq(freq)

        if not self.model.is_running:
            self.model.start()

        return {
            "player": label,
            "leds": int(leds),
            "dmx": {"address": int(addr), "universe": int(uni), "channels": 4 * int(leds)},
            "frequency_hz": float(f"{freq:.4f}"),
        }
