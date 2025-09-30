import asyncio
import random
import time

from pyartnet import ArtNetNode
from pyartnet.base import channel

# IMPORTANT: WLED TURN ON: >>MAIN SEGMENTS ONLY<<

colors= [
    [255, 0, 0],
    [0, 255, 0],
    [0, 0, 255],
]

NUM_LEDS = 6

async def main():
    print("Initializing node...")
    # Run this code in your async function
    node = ArtNetNode('192.168.178.99', 6454)

    print("Adding universe...")
    # Create universe 0
    universe = node.add_universe(0)

    print("Creating channel...")
    # Add a channel to the universe which consists of 3 values
    # Default size of a value is 8Bit (0..255) so this would fill
    # the DMX values 1..3 of the universe
    channel = universe.add_channel(start=1, width= (3*NUM_LEDS) )

    max_cycles = 1000
    cycles = max_cycles
    while(cycles > 0):
        print("Starting cycle..." + str(max_cycles - cycles) + "/" + str(max_cycles) + "...")
        aggr = []
        for i in range(0, NUM_LEDS):
            d = (cycles + i) % 3
            aggr.extend(colors[d])

        print(aggr)
        channel.add_fade(aggr, 1000)

        await channel
        time.sleep(0.5)
        cycles = cycles - 1

asyncio.run(main())