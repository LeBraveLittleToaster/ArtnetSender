package de.pschiessle.artnet.sender.effects;

import com.fasterxml.jackson.databind.JsonNode;
import de.pschiessle.artnet.sender.artnet.ArtnetClient;
import jakarta.annotation.PostConstruct;
import jakarta.annotation.PreDestroy;
import org.springframework.stereotype.Service;

import java.util.Arrays;
import java.util.Optional;
import java.util.concurrent.Executors;
import java.util.concurrent.ScheduledExecutorService;
import java.util.concurrent.TimeUnit;

@Service
public class EffectManager {
    private final ArtnetClient artnetClient;


    public static final int DMX_UNIVERSE_SIZE = 512;
    private static final double TARGET_FPS = 44.0;
    private static final long PERIOD_NANOS = (long) (1_000_000_000L / TARGET_FPS);

    private ScheduledExecutorService scheduler;

    private Effect curEffect = null;
    private long lastMillis = 0;

    public EffectManager(ArtnetClient artnetClient) {
        this.artnetClient = artnetClient;
    }

    @PostConstruct
    public void start() {
        System.out.println("Starting EffectManager");
        lastMillis = System.currentTimeMillis();
        scheduler = Executors.newSingleThreadScheduledExecutor(r -> {
            Thread t = new Thread(r, "artnet-dmx-sender");
            t.setDaemon(true);
            return t;
        });

        scheduler.scheduleAtFixedRate(
                this::sendLoop,
                1,
                PERIOD_NANOS,
                TimeUnit.NANOSECONDS
        );
    }

    @PreDestroy
    public void shutdown() {
        if (scheduler != null) {
            scheduler.shutdownNow();
        }
    }

    /**
     * Runs at 44 Hz in a background thread.
     * Polls the queue once each tick and sends one frame if available.
     */
    private void sendLoop() {
        if(curEffect != null){
            var dmx = curEffect.render(System.currentTimeMillis());
            System.out.println(Arrays.toString(dmx));
            artnetClient.sendDMX(dmx);
        }
    }


    public void setEffect(EffectType effectType, JsonNode optionsJson) {
        curEffect = EffectBuilder.createEffectFromTypeAndJson(effectType, optionsJson).orElse(null);
        System.out.println("Setting effect: " + effectType);
    }
}
