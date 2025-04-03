package com.chat.app.controller;

import com.chat.app.model.Message;
import com.chat.app.model.Channel;
import com.chat.app.repository.MessageRepository;
import com.chat.app.repository.ChannelRepository;
import lombok.RequiredArgsConstructor;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.*;
import org.springframework.http.HttpStatus;

import io.swagger.v3.oas.annotations.Operation;
import io.swagger.v3.oas.annotations.responses.ApiResponse;
import io.swagger.v3.oas.annotations.security.SecurityRequirement;

import java.security.Principal;

@RestController
@RequestMapping("/api/messages")
@RequiredArgsConstructor
public class MessageController {

    private final MessageRepository messageRepository;
    private final ChannelRepository channelRepository;

    @DeleteMapping("/channel/{channelId}/messages/{messageId}")
    @Operation(summary = "Supprimer un message")
    @SecurityRequirement(name = "bearerAuth")
    public ResponseEntity<?> deleteMessage(
            @PathVariable Long channelId,
            @PathVariable Long messageId,
            Principal principal) {
        
        Channel channel = channelRepository.findById(channelId)
                .orElseThrow(() -> new RuntimeException("Salon non trouvé"));

        Message message = messageRepository.findById(messageId)
                .orElseThrow(() -> new RuntimeException("Message non trouvé"));

        if (!message.getChannel().getId().equals(channelId)) {
            return ResponseEntity.badRequest()
                    .body("Le message n'appartient pas à ce salon");
        }

        String username = principal.getName();
        if (!channel.canModerateMessages(username)) {
            return ResponseEntity.status(HttpStatus.FORBIDDEN)
                    .body("Vous devez être modérateur ou administrateur pour supprimer des messages");
        }

        messageRepository.deleteById(messageId);
        return ResponseEntity.ok().build();
    }
} 