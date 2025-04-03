package com.chat.app.repository;

import com.chat.app.model.Channel;
import org.springframework.data.jpa.repository.JpaRepository;

import java.util.Optional;

public interface ChannelRepository extends JpaRepository<Channel, Long> {
    Optional<Channel> findByName(String name);
    boolean existsByName(String name);
} 