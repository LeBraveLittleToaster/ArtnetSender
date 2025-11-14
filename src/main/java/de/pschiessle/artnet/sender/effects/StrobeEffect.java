package de.pschiessle.artnet.sender.effects;

import java.util.Arrays;

import static de.pschiessle.artnet.sender.effects.EffectManager.DMX_UNIVERSE_SIZE;
import static de.pschiessle.artnet.sender.effects.EffectManager.DMX_UNIVERSE_SIZE;
public class StrobeEffect implements Effect{


    private int curTicks = 0;
    private boolean isOn = false;
    private final int ticksOn;
    private final int ticksOff;
    private final int lightCount;
    private double intensity = 255;

    public StrobeEffect(int ticksOn, int ticksOff, int lightCount) {
        this.ticksOn = ticksOn;
        this.ticksOff = ticksOff;

        this.lightCount = Math.max(Math.min(lightCount, DMX_UNIVERSE_SIZE), 0);
    }

    private byte[] getDmx(boolean isOn){

        byte[] dmxOut = new byte[DMX_UNIVERSE_SIZE];
        for(int segmentId = 0; segmentId < lightCount; segmentId++){
            dmxOut[segmentId * 5] = (byte) intensity;
            for(int channelOffset = 1; channelOffset < 5; channelOffset++){
                dmxOut[segmentId * 5 + channelOffset] = (byte) (isOn ? 255 : 0);
            }
        }
        return dmxOut;
    }

    public byte[] render(long time) {
        curTicks++;

        var dmx = getDmx(isOn);
        if(isOn){
            if(curTicks >= ticksOn){
                isOn = false;
                curTicks = 0;
            }
        }else{
            if(curTicks >= ticksOff){
                isOn = true;
                curTicks = 0;
            }
        }
        return dmx;
    }
}
