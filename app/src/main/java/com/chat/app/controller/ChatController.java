package com.chat.app.controller;

import com.chat.app.model.Message;
import com.chat.app.model.User; // Assurez-vous que le User est correctement récupéré (ex: via SecurityContext)
import com.chat.app.repository.MessageRepository;
import com.chat.app.repository.UserRepository; // Pour récupérer les détails de l'utilisateur
import lombok.RequiredArgsConstructor;
import org.springframework.messaging.handler.annotation.MessageMapping;
import org.springframework.messaging.handler.annotation.Payload;
import org.springframework.messaging.simp.SimpMessagingTemplate;
import org.springframework.security.core.Authentication; // Pour obtenir l'utilisateur authentifié
import org.springframework.security.core.context.SecurityContextHolder; // Pour obtenir l'utilisateur authentifié
import org.springframework.stereotype.Controller;
import org.springframework.transaction.annotation.Transactional;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.ResponseBody;
import org.springframework.web.bind.annotation.RestController;
import org.springframework.messaging.handler.annotation.Header;
import org.springframework.messaging.simp.SimpMessageHeaderAccessor;

import io.swagger.v3.oas.annotations.Operation;
import io.swagger.v3.oas.annotations.Parameter;
import io.swagger.v3.oas.annotations.media.ArraySchema;
import io.swagger.v3.oas.annotations.media.Content;
import io.swagger.v3.oas.annotations.media.Schema;
import io.swagger.v3.oas.annotations.responses.ApiResponse;
import io.swagger.v3.oas.annotations.responses.ApiResponses;
import io.swagger.v3.oas.annotations.tags.Tag;
import io.swagger.v3.oas.annotations.security.SecurityRequirement;

import java.security.Principal;

import java.time.LocalDateTime;
import java.util.List;
import java.util.stream.Collectors;
import com.chat.app.model.Channel;
import com.chat.app.repository.ChannelRepository;

@RestController
@RequiredArgsConstructor
@Tag(name = "Messages", description = "API de gestion des messages")
public class ChatController {

    private final SimpMessagingTemplate messagingTemplate;
    private final MessageRepository messageRepository;
    private final UserRepository userRepository; // Injecté pour trouver les utilisateurs
    private final ChannelRepository channelRepository; // Injecter ChannelRepository

    // Gère l'envoi d'un message public (ou à un salon spécifique)
    @MessageMapping("/chat.sendMessage")
    @Transactional
    @Operation(summary = "Envoyer un message", 
               description = "Envoie un message (privé, salon ou public) via WebSocket")
    // Injecter Principal pour obtenir l'utilisateur authentifié
    // Supprimer @Payload si le message ne contient que le contenu
    public void sendMessage(Principal principal, @Payload ChatMessagePayload payload) {
        if (principal == null) { /* ... gestion erreur ... */ return; }
        String currentUsername = principal.getName();
        User sender = userRepository.findByUsername(currentUsername)
                .orElseThrow(() -> new RuntimeException(/*...*/));
        
        User recipient = null;
        Channel channel = null;
        String destinationTopic = "/topic/public"; // Destination par défaut (ancien comportement)

        if (payload.channelId() != null) {
            // Message destiné à un salon
            channel = channelRepository.findById(payload.channelId()).orElse(null);
            if (channel == null) {
                System.err.println("Salon non trouvé: " + payload.channelId());
                // Envoyer une erreur à l'utilisateur ?
                return; // Ne pas envoyer le message
            }
            destinationTopic = "/topic/channel." + channel.getId(); // Destination spécifique au salon
            System.out.println("Sending message to channel: " + destinationTopic);

        } else if (payload.recipientUsername() != null && !payload.recipientUsername().isEmpty()) {
            // Message privé
            recipient = userRepository.findByUsername(payload.recipientUsername()).orElse(null);
            if (recipient == null) {
                 System.err.println("Destinataire privé non trouvé: " + payload.recipientUsername());
                 return; // Ne pas envoyer si destinataire privé inconnu
            }
            // La diffusion se fera via convertAndSendToUser
        } else {
             System.out.println("Sending message to public topic (no channel, no recipient)");
             // Message public générique (si on garde cette logique)
        }

        // Créer et sauvegarder le message
        Message message = new Message();
        message.setSender(sender);
        message.setRecipient(recipient); // Null si message de salon ou public générique
        message.setChannel(channel);     // Null si message privé ou public générique
        message.setContent(payload.content());
        message.setTimestamp(LocalDateTime.now());
        Message savedMessage = messageRepository.save(message);

        // Construire le DTO
        MessageDto messageDto = new MessageDto(
            savedMessage.getId(),
            sender.getUsername(),
            recipient != null ? recipient.getUsername() : null,
            channel != null ? channel.getId() : null, // Inclure l'ID du salon
            savedMessage.getContent(),
            savedMessage.getTimestamp()
        );

        // Diffuser le message
        if (recipient != null) {
            // Message privé
            messagingTemplate.convertAndSendToUser(sender.getUsername(), "/queue/messages", messageDto);
            messagingTemplate.convertAndSendToUser(recipient.getUsername(), "/queue/messages", messageDto);
        } else {
            // Message de salon ou public générique
            messagingTemplate.convertAndSend(destinationTopic, messageDto);
        }
    }

    // On pourrait ajouter une méthode pour gérer l'ajout d'un utilisateur au chat (connexion)
    // @MessageMapping("/chat.addUser")
    // @SendTo("/topic/public")
    // public Message addUser(@Payload Message message, SimpMessageHeaderAccessor headerAccessor) {
    //     // Ajouter le nom d'utilisateur dans la session WebSocket
    //     headerAccessor.getSessionAttributes().put("username", message.getSender());
    //     return message;
    // }

    // --- Endpoint REST pour l'historique des messages (privé) ---
    @GetMapping("/api/messages/user/{otherUsername}") // Chemin spécifique pour user
    @ResponseBody
    @Operation(summary = "Historique de conversation privée", 
               description = "Récupère l'historique des messages échangés avec un utilisateur spécifique")
    @ApiResponses(value = {
        @ApiResponse(responseCode = "200", description = "Historique récupéré avec succès",
                     content = @Content(array = @ArraySchema(schema = @Schema(implementation = MessageDto.class)))),
        @ApiResponse(responseCode = "401", description = "Utilisateur non authentifié")
    })
    @SecurityRequirement(name = "bearerAuth")
    public List<MessageDto> getUserConversationHistory(
            @Parameter(hidden = true) Principal principal,
            @Parameter(description = "Nom d'utilisateur de l'autre participant", required = true) 
            @PathVariable String otherUsername) {
        if (principal == null) { return List.of(); }
        User currentUser = userRepository.findByUsername(principal.getName()).orElse(null);
        User otherUser = userRepository.findByUsername(otherUsername).orElse(null);
        if (currentUser == null || otherUser == null) { return List.of(); }
        List<Message> messages = messageRepository.findConversationHistory(currentUser.getId(), otherUser.getId());
        return mapMessagesToDto(messages);
    }

    @GetMapping("/api/messages/channel/{channelId}") // Chemin spécifique pour channel
    @ResponseBody
    @Operation(summary = "Historique d'un salon", 
               description = "Récupère l'historique des messages d'un salon spécifique")
    @ApiResponses(value = {
        @ApiResponse(responseCode = "200", description = "Historique récupéré avec succès",
                     content = @Content(array = @ArraySchema(schema = @Schema(implementation = MessageDto.class)))),
        @ApiResponse(responseCode = "401", description = "Utilisateur non authentifié"),
        @ApiResponse(responseCode = "404", description = "Salon non trouvé")
    })
    @SecurityRequirement(name = "bearerAuth")
    public List<MessageDto> getChannelHistory(
            @Parameter(hidden = true) Principal principal, 
            @Parameter(description = "Identifiant du salon", required = true) 
            @PathVariable Long channelId) {
         if (principal == null) { return List.of(); } // Doit être loggé pour voir l'historique
         // Vérifier si le salon existe (optionnel, mais recommandé)
         if (!channelRepository.existsById(channelId)) {
             // Ou retourner 404 Not Found
             return List.of();
         }
         List<Message> messages = messageRepository.findByChannelIdOrderByTimestampAsc(channelId);
         return mapMessagesToDto(messages);
    }

    @GetMapping("/api/messages/public") // Endpoint pour historique public générique
    @ResponseBody
    @Operation(summary = "Historique du chat public général", 
               description = "Récupère l'historique des messages du chat public général")
    @ApiResponses(value = {
        @ApiResponse(responseCode = "200", description = "Historique récupéré avec succès",
                     content = @Content(array = @ArraySchema(schema = @Schema(implementation = MessageDto.class)))),
        @ApiResponse(responseCode = "401", description = "Utilisateur non authentifié")
    })
    @SecurityRequirement(name = "bearerAuth")
    public List<MessageDto> getPublicHistory(@Parameter(hidden = true) Principal principal) {
        if (principal == null) { return List.of(); } // Doit être loggé
        List<Message> messages = messageRepository.findByRecipientIsNullAndChannelIsNullOrderByTimestampAsc();
        return mapMessagesToDto(messages);
    }

    // Méthode utilitaire pour mapper Message en MessageDto
    private List<MessageDto> mapMessagesToDto(List<Message> messages) {
        return messages.stream()
                .map(msg -> new MessageDto(
                        msg.getId(),
                        msg.getSender().getUsername(),
                        msg.getRecipient() != null ? msg.getRecipient().getUsername() : null,
                        msg.getChannel() != null ? msg.getChannel().getId() : null,
                        msg.getContent(),
                        msg.getTimestamp()))
                .collect(Collectors.toList());
    }

    // --- Endpoint REST pour récupérer la liste des utilisateurs (pour le chat privé) ---
    @GetMapping("/api/users")
    @ResponseBody
    @Operation(summary = "Liste des utilisateurs", 
               description = "Récupère la liste de tous les utilisateurs disponibles pour le chat privé")
    @ApiResponses(value = {
        @ApiResponse(responseCode = "200", description = "Liste récupérée avec succès",
                     content = @Content(array = @ArraySchema(schema = @Schema(implementation = UserDto.class)))),
        @ApiResponse(responseCode = "401", description = "Utilisateur non authentifié")
    })
    @SecurityRequirement(name = "bearerAuth")
    public List<UserDto> getAllUsers(@Parameter(hidden = true) Principal principal) {
         if (principal == null) {
            return List.of(); 
        }
        String currentUsername = principal.getName();
        List<User> users = userRepository.findAll();
        return users.stream()
                    // Exclure l'utilisateur actuel de la liste
                    .filter(user -> !user.getUsername().equals(currentUsername)) 
                    .map(user -> new UserDto(user.getUsername())) // Ne renvoyer que le username
                    .collect(Collectors.toList());
    }

    // --- Endpoint REST pour l'historique des messages publics (exemple) ---
    // @GetMapping("/api/messages/public")
    // @ResponseBody
    // public List<MessageDto> getPublicHistory() {
    //     // Implémenter la logique pour récupérer les messages publics/d'un canal spécifique
    //     // List<Message> messages = messageRepository.findByChannelIdOrderByTimestampAsc(PUBLIC_CHANNEL_ID);
    //     List<Message> messages = messageRepository.findAll(); // Simpliste: récupère tout
    //     return messages.stream()
    //             .filter(m -> m.getRecipient() == null) // Filtrer pour les messages publics
    //             .map(msg -> new MessageDto(
    //                     msg.getId(),
    //                     msg.getSender().getUsername(),
    //                     null, // Pas de destinataire spécifique
    //                     msg.getContent(),
    //                     msg.getTimestamp()))
    //             .collect(Collectors.toList());
    // }

    // DTO pour les messages envoyés aux clients (pour ne pas exposer toute l'entité User)
    @Schema(description = "DTO pour les messages")
    record MessageDto(
        @Schema(description = "Identifiant unique du message") Long id, 
        @Schema(description = "Nom d'utilisateur de l'expéditeur") String senderUsername, 
        @Schema(description = "Nom d'utilisateur du destinataire (pour les messages privés)") String recipientUsername, 
        @Schema(description = "Identifiant du salon (pour les messages de salon)") Long channelId, 
        @Schema(description = "Contenu du message") String content, 
        @Schema(description = "Timestamp du message") LocalDateTime timestamp
    ) {}
    
    // Payload pour WebSocket (ne contient plus le sender)
    @Schema(description = "Payload pour l'envoi de message via WebSocket")
    record ChatMessagePayload(
        @Schema(description = "Contenu du message", example = "Bonjour tout le monde!") String content, 
        @Schema(description = "Nom d'utilisateur du destinataire (pour message privé)") String recipientUsername, 
        @Schema(description = "Identifiant du salon (pour message de salon)") Long channelId
    ) {}

    // DTO pour la liste des utilisateurs
    @Schema(description = "DTO pour un utilisateur")
    record UserDto(@Schema(description = "Nom d'utilisateur") String username) {}
} 