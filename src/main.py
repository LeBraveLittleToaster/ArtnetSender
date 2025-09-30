import asyncio
import random
import time

from pyartnet import ArtNetNode
from pyartnet.base import channel

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

    print("Starting cycles")
    cycles = 0
    while(cycles < 1000):
        aggr = []
        for i in range(0, NUM_LEDS):
            print(i)
            d = (cycles + i) % 3
            print(d)
            match d:
                case 0:
                    aggr.append(255)
                    aggr.append(0)
                    aggr.append(0)
                    continue
                case 1:
                    aggr.append(0)
                    aggr.append(255)
                    aggr.append(0)
                    continue
                case 2:
                    aggr.append(0)
                    aggr.append(0)
                    aggr.append(255)
                    continue

        print(aggr)
        channel.add_fade(aggr, 1000)

        # this can be used to wait till the fade is complete
        await channel
        time.sleep(0.5)
        cycles = cycles + 1

asyncio.run(main())