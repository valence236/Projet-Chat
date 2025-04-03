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
import java.security.Principal;

import java.time.LocalDateTime;
import java.util.List;
import java.util.stream.Collectors;

@RestController
@RequiredArgsConstructor
public class ChatController {

    private final SimpMessagingTemplate messagingTemplate;
    private final MessageRepository messageRepository;
    private final UserRepository userRepository; // Injecté pour trouver les utilisateurs

    // Gère l'envoi d'un message public (ou à un salon spécifique)
    @MessageMapping("/chat.sendMessage")
    @Transactional
    // Injecter Principal pour obtenir l'utilisateur authentifié
    // Supprimer @Payload si le message ne contient que le contenu
    public void sendMessage(Principal principal, @Payload ChatMessagePayload chatMessage) {
        // 1. Récupérer l'utilisateur authentifié (expéditeur) depuis Principal
        if (principal == null) {
             // Normalement impossible si l'intercepteur rejette les connexions non authentifiées
            System.err.println("Principal is null in sendMessage, cannot process message.");
            return; 
        }
        String currentUsername = principal.getName();
        User sender = userRepository.findByUsername(currentUsername)
                .orElseThrow(() -> new RuntimeException("Expéditeur authentifié non trouvé en base: " + currentUsername));
        
        // 2. Trouver le destinataire (si fourni)
        User recipient = null;
        if (chatMessage.recipientUsername() != null && !chatMessage.recipientUsername().isEmpty()) {
             recipient = userRepository.findByUsername(chatMessage.recipientUsername())
                    .orElse(null); // Ne pas planter si destinataire inconnu
             if (recipient == null) {
                 System.err.println("Destinataire non trouvé: " + chatMessage.recipientUsername());
                 // Envoyer une notification d'erreur à l'expéditeur ?
                 // Pour l'instant, on ignore simplement (le message sera public s'il n'y a pas de destinataire)
             }
        }

        // 3. Créer et sauvegarder le message
        Message message = new Message();
        message.setSender(sender);
        message.setRecipient(recipient); // Sera null si destinataire non trouvé ou non fourni
        message.setContent(chatMessage.content());
        message.setTimestamp(LocalDateTime.now());

        Message savedMessage = messageRepository.save(message);

        // 4. Construire le DTO
        MessageDto messageDto = new MessageDto(
            savedMessage.getId(),
            sender.getUsername(),
            recipient != null ? recipient.getUsername() : null,
            savedMessage.getContent(),
            savedMessage.getTimestamp()
        );

        // 5. Diffuser le message
        if (recipient != null) {
            // Message privé: envoyer à la file personnelle des deux utilisateurs
            // Le préfixe /user/ est géré par Spring
            messagingTemplate.convertAndSendToUser(sender.getUsername(), "/queue/messages", messageDto);
            messagingTemplate.convertAndSendToUser(recipient.getUsername(), "/queue/messages", messageDto);
        } else {
            // Message public/canal: envoyer à un topic général
            messagingTemplate.convertAndSend("/topic/public", messageDto);
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
    @GetMapping("/api/messages/{otherUsername}")
    @ResponseBody
    public List<MessageDto> getConversationHistory(Principal principal, @PathVariable String otherUsername) {
        if (principal == null) {
            // Gérer l'erreur: l'utilisateur doit être authentifié pour voir l'historique
            return List.of(); 
        }
        String currentUsername = principal.getName();
        User currentUser = userRepository.findByUsername(currentUsername).orElse(null);
        User otherUser = userRepository.findByUsername(otherUsername).orElse(null);

        if (currentUser == null || otherUser == null) {
            return List.of(); 
        }

        List<Message> messages = messageRepository.findConversationHistory(currentUser.getId(), otherUser.getId());
        return messages.stream()
                .map(msg -> new MessageDto(
                        msg.getId(),
                        msg.getSender().getUsername(),
                        msg.getRecipient() != null ? msg.getRecipient().getUsername() : null,
                        msg.getContent(),
                        msg.getTimestamp()))
                .collect(Collectors.toList());
    }
    
    // --- Endpoint REST pour récupérer la liste des utilisateurs (pour le chat privé) ---
    @GetMapping("/api/users")
    @ResponseBody
    public List<UserDto> getAllUsers(Principal principal) {
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
    record MessageDto(Long id, String senderUsername, String recipientUsername, String content, LocalDateTime timestamp) {}
    
    // Payload pour WebSocket (ne contient plus le sender)
    record ChatMessagePayload(String content, String recipientUsername /* Peut être null */) {}

    // DTO pour la liste des utilisateurs
    record UserDto(String username) {}
} 