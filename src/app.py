from __future__ import annotations
import logging
from flask import Flask, jsonify, request

from src.config import ARTNET_TARGET_IP, ARTNET_TARGET_PORT, DEFAULT_LEDS, DEFAULT_START_ADDRESS, DEFAULT_UNIVERSE, FRAME_FPS
from src.controller import Controller
from src.model import ArtNetModel

app = Flask(__name__)
log = logging.getLogger("artnet.app")
logging.basicConfig(level=logging.INFO, format="[%(levelname)s] %(message)s")

model = ArtNetModel(ip=ARTNET_TARGET_IP, port=ARTNET_TARGET_PORT, fps=FRAME_FPS, on_status=log.info)
controller = Controller(model)


@app.get("/play/<int:player_id>")
def play(player_id: int):
    leds = request.args.get("leds", type=int)
    freq = request.args.get("freq", type=float)
    addr = request.args.get("addr", default=DEFAULT_START_ADDRESS, type=int)
    uni = request.args.get("uni", default=DEFAULT_UNIVERSE, type=int)

    if leds is None or leds <= 0:
        return jsonify({"error": "leds is required and must be > 0"}), 400
    if freq is None:
        return jsonify({"error": "freq is required (Hz)"}), 400

    try:
        out = controller.apply(player_id=player_id, leds=leds, freq=float(freq), addr=int(addr), uni=int(uni))
        if "error" in out:
            return jsonify(out), 400
        return jsonify({"status": "ok", **out}), 200
    except Exception as e:
        log.exception("/play error: %r", e)
        return jsonify({"error": repr(e)}), 500


@app.get("/healthz")
def healthz():
    return jsonify({
        "running": model.is_running,
        "ip": ARTNET_TARGET_IP,
        "port": ARTNET_TARGET_PORT,
    })


if __name__ == "__main__":
    app.run(host="0.0.0.0", port=8001, debug=False)