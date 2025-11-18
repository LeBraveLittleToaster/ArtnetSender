package de.pschiessle.artnet.sender.artnet;

import org.springframework.data.repository.CrudRepository;
import org.springframework.stereotype.Repository;

import java.util.List;
import java.util.Optional;

@Repository
public interface DeviceRepository extends CrudRepository<DeviceDTO, Long> {
    Optional<DeviceDTO> findDeviceDTOByUuid(String uuid);
    Optional<List<DeviceDTO>> findDeviceDTOSByIsActive(Boolean isActive);
}
