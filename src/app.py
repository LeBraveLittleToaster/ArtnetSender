import asyncio
import logging

from aioartnet import ArtNetClient




async def main() -> None:
    client = ArtNetClient()
    u1 = client.set_port_config("0:0:1", isoutput=True)
    await client.connect()
    await client.set_dmx(u1, bytes(list(range(128)) * 4))


if __name__ == "__main__":
    logging.basicConfig(level=logging.INFO)
    asyncio.run(main())
    asyncio.get_event_loop().run_forever()