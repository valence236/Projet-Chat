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
        
        try {
            if (principal == null) {
                return ResponseEntity.status(HttpStatus.UNAUTHORIZED)
                        .body(new ErrorResponse("Non authentifié"));
            }
            
            Channel channel = channelRepository.findById(channelId)
                    .orElseThrow(() -> new RuntimeException("Salon non trouvé"));
    
            Message message = messageRepository.findById(messageId)
                    .orElseThrow(() -> new RuntimeException("Message non trouvé"));
    
            if (message.getChannel() == null || !message.getChannel().getId().equals(channelId)) {
                return ResponseEntity.badRequest()
                        .body(new ErrorResponse("Le message n'appartient pas à ce salon"));
            }
    
            String username = principal.getName();
            if (!channel.canModerateMessages(username)) {
                return ResponseEntity.status(HttpStatus.FORBIDDEN)
                        .body(new ErrorResponse("Vous devez être modérateur ou administrateur pour supprimer des messages"));
            }
    
            messageRepository.deleteById(messageId);
            return ResponseEntity.ok(new SuccessResponse("Message supprimé avec succès"));
            
        } catch (Exception e) {
            return ResponseEntity.status(HttpStatus.INTERNAL_SERVER_ERROR)
                    .body(new ErrorResponse("Erreur lors de la suppression du message: " + e.getMessage()));
        }
    }
    
    // Classes pour les réponses
    record ErrorResponse(String message) {}
    record SuccessResponse(String message) {}
} 