from __future__ import annotations
import asyncio
import concurrent.futures
import threading
import time
from dataclasses import dataclass
from typing import Callable, Optional

try:
    import pyartnet  # type: ignore
except Exception as e:  # pragma: no cover
    pyartnet = None  # allows import without the library for static analysis

from .effects import EffectEngine, EffectState, EffectType
from .dmx import CHANNELS_PER_LED


class _AsyncLoopThread:
    """Runs an asyncio event loop in a dedicated thread and lets us execute
    synchronous callables in that loop’s context—needed because pyartnet
    expects to create asyncio tasks inside a running loop.
    """

    def __init__(self, name: str = "artnet-loop"):
        self._name = name
        self._thread: Optional[threading.Thread] = None
        self.loop: Optional[asyncio.AbstractEventLoop] = None

    def start(self):
        if self._thread and self._thread.is_alive():
            return

        def _runner():
            loop = asyncio.new_event_loop()
            asyncio.set_event_loop(loop)
            self.loop = loop
            loop.run_forever()

        self._thread = threading.Thread(target=_runner, name=self._name, daemon=True)
        self._thread.start()
        # Wait until loop is ready
        while self.loop is None:
            time.sleep(0.01)

    def stop(self):
        if self.loop is None:
            return
        self.loop.call_soon_threadsafe(self.loop.stop)
        self.loop = None
        # thread is daemon; no join needed

    def run_in_loop(self, fn, *args, **kwargs):
        """Execute a synchronous function on the loop thread and block for result."""
        if self.loop is None:
            raise RuntimeError("loop not started")
        fut: concurrent.futures.Future = concurrent.futures.Future()

        def _call():
            try:
                res = fn(*args, **kwargs)
                fut.set_result(res)
            except BaseException as e:
                fut.set_exception(e)

        self.loop.call_soon_threadsafe(_call)
        return fut.result()


@dataclass
class DmxConfig:
    universe: int
    address: int


class ArtNetModel:
    def __init__(self, ip: str, port: int, fps: int,
                 on_status: Optional[Callable[[str], None]] = None):
        self.ip = ip
        self.port = port
        self.fps = max(1, int(fps))
        self._status = on_status or (lambda *_: None)

        self.state = EffectState(leds=1)
        self.engine = EffectEngine(self.state)

        self._dmx_cfg = DmxConfig(universe=0, address=1)
        self._driver = None
        self._universe = None
        self._channel = None

        self._loopthread = _AsyncLoopThread()
        self._thread = None
        self._running = False
        self._t0 = time.perf_counter()

    # ---- lifecycle ------------------------------------------------------

    @property
    def is_running(self) -> bool:
        return self._running

    def start(self):
        if self._running:
            return
        if pyartnet is None:
            raise RuntimeError("pyartnet is not installed")

        # Ensure asyncio loop exists before creating ArtNetNode
        self._loopthread.start()

        def _init_driver():
            self._driver = pyartnet.ArtNetNode(self.ip, self.port)
            self._universe = self._driver.add_universe(self._dmx_cfg.universe)
            width = self.state.leds * CHANNELS_PER_LED
            print("Creating channel with width=" + str(width))
            self._channel = self._universe.add_channel(self._dmx_cfg.address, width)

        self._loopthread.run_in_loop(_init_driver)

        self._running = True
        self._thread = threading.Thread(target=self._run,
                                        name="artnet-model", daemon=True)
        self._thread.start()
        self._status("ArtNetModel started")

    def stop(self):
        if not self._running:
            return
        self._running = False
        if self._thread:
            self._thread.join(timeout=1.5)
        self._thread = None

        def _teardown():
            self._channel = None
            self._universe = None
            self._driver = None

        try:
            self._loopthread.run_in_loop(_teardown)
        except RuntimeError:
            pass
        self._status("ArtNetModel stopped")

    def restart_for_leds(self, leds: int):
        leds = int(max(1, leds))
        if leds == self.state.leds and self._running:
            return
        was_running = self._running
        if was_running:
            self.stop()
        self.state.set_leds(leds)
        if was_running:
            self.start()

    # ---- configuration --------------------------------------------------

    def set_dmx_config(self, cfg: DmxConfig):
        self._dmx_cfg = cfg
        if not self._running:
            return
        # Rebuild channel with new addressing
        self.stop()
        self.start()

    # ---- effect API -----------------------------------------------------

    def set_effect(self, e: EffectType):
        self.engine.set_effect(e)

    def set_static_level(self, v: int):
        self.engine.set_static_level(v)

    def set_bouncy_freq(self, hz: float):
        self.engine.set_bouncy_freq(hz)

    def set_gradient_speed(self, hz: float):
        self.engine.set_gradient_speed(hz)

    def set_chase_freq(self, hz: float):
        self.engine.set_chase_freq(hz)

    # ---- main loop ------------------------------------------------------

    def _run(self):
        frame_time = 1.0 / self.fps
        while self._running:
            t = time.perf_counter() - self._t0
            frame = self.engine.render(t)

            def _write():
                if self._channel is not None:
                    self._channel.add_fade(frame, 0)  # immediate write

            try:
                self._loopthread.run_in_loop(_write)
            except RuntimeError:
                pass
            time.sleep(frame_time)
