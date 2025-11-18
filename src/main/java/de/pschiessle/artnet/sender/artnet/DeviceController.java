package de.pschiessle.artnet.sender.artnet;

import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.*;
import org.springframework.web.server.ResponseStatusException;

import java.util.List;

@RestController
@RequestMapping("/devices")
public class DeviceController {

    private final DeviceService deviceService;

    public DeviceController(DeviceService deviceService) {
        this.deviceService = deviceService;
    }

    @GetMapping
    public List<DeviceDTO> getAllDevices(
            @RequestParam(value = "isActive", required = false) Boolean isActive
    ) {
        if (isActive != null) {
            return deviceService.getDevicesByIsActive(isActive);
        }
        return deviceService.getAllDevices();
    }

    @GetMapping("/{id}")
    public DeviceDTO getDeviceById(@PathVariable Long id) {
        return deviceService.getDeviceById(id)
                .orElseThrow(() -> new ResponseStatusException(HttpStatus.NOT_FOUND, "Device not found"));
    }

    @GetMapping("/uuid/{uuid}")
    public DeviceDTO getDeviceByUuid(@PathVariable String uuid) {
        return deviceService.getDeviceByUuid(uuid)
                .orElseThrow(() -> new ResponseStatusException(HttpStatus.NOT_FOUND, "Device not found"));
    }

    @PostMapping
    public ResponseEntity<DeviceDTO> createDevice(@RequestBody DeviceDTO deviceDTO) {
        DeviceDTO created = deviceService.createDevice(deviceDTO);
        return ResponseEntity.status(HttpStatus.CREATED).body(created);
    }

    @PutMapping("/{id}")
    public DeviceDTO updateDevice(@PathVariable Long id, @RequestBody DeviceDTO deviceDTO) {
        return deviceService.updateDevice(id, deviceDTO)
                .orElseThrow(() -> new ResponseStatusException(HttpStatus.NOT_FOUND, "Device not found"));
    }

    @DeleteMapping("/{id}")
    @ResponseStatus(HttpStatus.NO_CONTENT)
    public void deleteDevice(@PathVariable Long id) {
        deviceService.deleteDevice(id);
    }
}
