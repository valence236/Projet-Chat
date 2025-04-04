package com.chat.app.controller;

import com.chat.app.model.Channel;
import com.chat.app.repository.ChannelRepository;
import com.chat.app.repository.MessageRepository;
import lombok.RequiredArgsConstructor;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.*;

import io.swagger.v3.oas.annotations.Operation;
import io.swagger.v3.oas.annotations.Parameter;
import io.swagger.v3.oas.annotations.responses.ApiResponse;
import io.swagger.v3.oas.annotations.security.SecurityRequirement;

import java.security.Principal;
import java.util.List;
import java.util.stream.Collectors;
import java.util.HashSet;
import java.util.Set;

@RestController
@RequestMapping("/api/channels")
@RequiredArgsConstructor
public class ChannelController {

    private final ChannelRepository channelRepository;
    private final MessageRepository messageRepository;

    @GetMapping
    @Operation(summary = "Liste tous les salons")
    @SecurityRequirement(name = "bearerAuth")
    public ResponseEntity<List<ChannelDto>> getAllChannels(Principal principal) {
        if (principal == null) {
            return ResponseEntity.status(HttpStatus.UNAUTHORIZED).build();
        }
        List<Channel> channels = channelRepository.findAll();
        List<ChannelDto> channelDtos = channels.stream()
                .map(channel -> new ChannelDto(
                    channel.getId(), 
                    channel.getName(), 
                    channel.getDescription(),
                    channel.getCreatorUsername(),
                    channel.getModeratorUsernames()))
                .collect(Collectors.toList());
        return ResponseEntity.ok(channelDtos);
    }

    @PostMapping
    @Operation(summary = "Crée un nouveau salon")
    @SecurityRequirement(name = "bearerAuth")
    public ResponseEntity<?> createChannel(
            @RequestBody CreateChannelRequest request, 
            Principal principal) {
        if (principal == null) {
            return ResponseEntity.status(HttpStatus.UNAUTHORIZED).build();
        }

        Channel newChannel = new Channel();
        newChannel.setName(request.name().trim());
        newChannel.setDescription(request.description());
        newChannel.setCreatorUsername(principal.getName());
        
        Channel savedChannel = channelRepository.save(newChannel);
        ChannelDto channelDto = new ChannelDto(
            savedChannel.getId(), 
            savedChannel.getName(), 
            savedChannel.getDescription(),
            savedChannel.getCreatorUsername(),
            savedChannel.getModeratorUsernames()
        );
        
        return ResponseEntity.status(HttpStatus.CREATED).body(channelDto);
    }

    @PostMapping("/{channelId}/moderators")
    @Operation(summary = "Met à jour la liste des modérateurs")
    @SecurityRequirement(name = "bearerAuth")
    public ResponseEntity<?> updateModerators(
            @PathVariable Long channelId,
            @RequestBody List<String> moderatorUsernames,
            Principal principal) {
        
        try {
            Channel channel = channelRepository.findById(channelId)
                .orElseThrow(() -> new RuntimeException("Salon non trouvé"));
    
            if (!channel.getCreatorUsername().equals(principal.getName())) {
                return ResponseEntity.status(HttpStatus.FORBIDDEN)
                    .body(new ErrorResponse("Seul l'administrateur peut gérer les modérateurs"));
            }
    
            System.out.println("Mise à jour des modérateurs pour le salon " + channelId + ": " + moderatorUsernames);
            channel.setModeratorUsernames(new HashSet<>(moderatorUsernames));
            channelRepository.save(channel);
            
            return ResponseEntity.ok(new SuccessResponse("Modérateurs mis à jour avec succès"));
        } catch (Exception e) {
            return ResponseEntity.status(HttpStatus.INTERNAL_SERVER_ERROR)
                .body(new ErrorResponse("Erreur lors de la mise à jour des modérateurs: " + e.getMessage()));
        }
    }

    @DeleteMapping("/{channelId}")
    @Operation(summary = "Supprime un salon")
    @SecurityRequirement(name = "bearerAuth")
    public ResponseEntity<?> deleteChannel(
            @PathVariable Long channelId,
            Principal principal) {
        try {
            System.out.println("Tentative de suppression du salon " + channelId + " par " + principal.getName());
            
            // Vérifier si le salon existe
            Channel channel = channelRepository.findById(channelId)
                .orElseThrow(() -> new RuntimeException("Salon non trouvé: " + channelId));

            System.out.println("Salon trouvé: " + channel.getName());
            System.out.println("Créateur du salon: " + channel.getCreatorUsername());
            System.out.println("Utilisateur actuel: " + principal.getName());

            // Vérifier si l'utilisateur est l'administrateur
            if (!channel.getCreatorUsername().equals(principal.getName())) {
                System.out.println("Tentative non autorisée de suppression");
                return ResponseEntity.status(HttpStatus.FORBIDDEN)
                    .body(new ErrorResponse("Seul l'administrateur peut supprimer le salon"));
            }

            try {
                // Supprimer d'abord les messages associés au salon
                System.out.println("Suppression des messages du salon");
                messageRepository.deleteByChannelId(channelId);

                // Supprimer le salon
                System.out.println("Suppression du salon");
                channelRepository.delete(channel);
                
                System.out.println("Salon supprimé avec succès");
                return ResponseEntity.ok().body(new SuccessResponse("Salon supprimé avec succès"));
            } catch (Exception e) {
                System.err.println("Erreur lors de la suppression: " + e.getMessage());
                e.printStackTrace();
                throw e;
            }
        } catch (Exception e) {
            System.err.println("Erreur globale: " + e.getMessage());
            e.printStackTrace();
            return ResponseEntity.status(HttpStatus.INTERNAL_SERVER_ERROR)
                .body(new ErrorResponse("Erreur lors de la suppression du salon: " + e.getMessage()));
        }
    }

    @GetMapping("/{channelId}/permissions")
    @Operation(summary = "Récupère les permissions de l'utilisateur pour un salon")
    @SecurityRequirement(name = "bearerAuth")
    public ResponseEntity<?> getPermissions(
            @PathVariable Long channelId,
            Principal principal) {
        
        Channel channel = channelRepository.findById(channelId)
            .orElseThrow(() -> new RuntimeException("Salon non trouvé"));

        boolean isAdmin = channel.isAdmin(principal.getName());
        boolean isModerator = channel.isModerator(principal.getName());

        return ResponseEntity.ok(new PermissionsDto(isAdmin, isModerator));
    }

    @PostMapping("/{channelId}/block/{username}")
    @Operation(summary = "Bloquer un utilisateur dans un salon")
    @SecurityRequirement(name = "bearerAuth")
    public ResponseEntity<?> blockUser(
            @PathVariable Long channelId,
            @PathVariable String username,
            Principal principal) {
        try {
            Channel channel = channelRepository.findById(channelId)
                .orElseThrow(() -> new RuntimeException("Salon non trouvé"));
            
            // Vérifier si l'utilisateur actuel est administrateur ou modérateur
            if (!channel.canModerateMessages(principal.getName())) {
                return ResponseEntity.status(HttpStatus.FORBIDDEN)
                    .body(new ErrorResponse("Vous devez être administrateur ou modérateur pour bloquer un utilisateur"));
            }
            
            // Vérifier que l'utilisateur à bloquer n'est pas l'administrateur
            if (channel.isAdmin(username)) {
                return ResponseEntity.status(HttpStatus.FORBIDDEN)
                    .body(new ErrorResponse("Impossible de bloquer l'administrateur du salon"));
            }
            
            // Ajouter l'utilisateur à la liste des utilisateurs bloqués
            Set<String> blockedUsers = channel.getBlockedUsernames();
            blockedUsers.add(username);
            channel.setBlockedUsernames(blockedUsers);
            
            // Si l'utilisateur bloqué était modérateur, le retirer des modérateurs
            if (channel.isModerator(username)) {
                Set<String> moderators = channel.getModeratorUsernames();
                moderators.remove(username);
                channel.setModeratorUsernames(moderators);
            }
            
            channelRepository.save(channel);
            
            return ResponseEntity.ok(new SuccessResponse("Utilisateur " + username + " bloqué avec succès"));
        } catch (Exception e) {
            return ResponseEntity.status(HttpStatus.INTERNAL_SERVER_ERROR)
                .body(new ErrorResponse("Erreur lors du blocage de l'utilisateur: " + e.getMessage()));
        }
    }
    
    @DeleteMapping("/{channelId}/block/{username}")
    @Operation(summary = "Débloquer un utilisateur dans un salon")
    @SecurityRequirement(name = "bearerAuth")
    public ResponseEntity<?> unblockUser(
            @PathVariable Long channelId,
            @PathVariable String username,
            Principal principal) {
        try {
            Channel channel = channelRepository.findById(channelId)
                .orElseThrow(() -> new RuntimeException("Salon non trouvé"));
            
            // Vérifier si l'utilisateur actuel est administrateur ou modérateur
            if (!channel.canModerateMessages(principal.getName())) {
                return ResponseEntity.status(HttpStatus.FORBIDDEN)
                    .body(new ErrorResponse("Vous devez être administrateur ou modérateur pour débloquer un utilisateur"));
            }
            
            // Retirer l'utilisateur de la liste des utilisateurs bloqués
            Set<String> blockedUsers = channel.getBlockedUsernames();
            blockedUsers.remove(username);
            channel.setBlockedUsernames(blockedUsers);
            channelRepository.save(channel);
            
            return ResponseEntity.ok(new SuccessResponse("Utilisateur " + username + " débloqué avec succès"));
        } catch (Exception e) {
            return ResponseEntity.status(HttpStatus.INTERNAL_SERVER_ERROR)
                .body(new ErrorResponse("Erreur lors du déblocage de l'utilisateur: " + e.getMessage()));
        }
    }
    
    @GetMapping("/{channelId}/blocked")
    @Operation(summary = "Liste les utilisateurs bloqués d'un salon")
    @SecurityRequirement(name = "bearerAuth")
    public ResponseEntity<?> getBlockedUsers(
            @PathVariable Long channelId,
            Principal principal) {
        try {
            Channel channel = channelRepository.findById(channelId)
                .orElseThrow(() -> new RuntimeException("Salon non trouvé"));
            
            // Vérifier si l'utilisateur actuel est administrateur ou modérateur
            if (!channel.canModerateMessages(principal.getName())) {
                return ResponseEntity.status(HttpStatus.FORBIDDEN)
                    .body(new ErrorResponse("Vous devez être administrateur ou modérateur pour voir les utilisateurs bloqués"));
            }
            
            return ResponseEntity.ok(channel.getBlockedUsernames());
        } catch (Exception e) {
            return ResponseEntity.status(HttpStatus.INTERNAL_SERVER_ERROR)
                .body(new ErrorResponse("Erreur lors de la récupération des utilisateurs bloqués: " + e.getMessage()));
        }
    }

    @GetMapping("/{channelId}")
    @Operation(summary = "Récupère les détails d'un salon")
    @SecurityRequirement(name = "bearerAuth")
    public ResponseEntity<?> getChannel(
            @PathVariable Long channelId,
            Principal principal) {
        if (principal == null) {
            return ResponseEntity.status(HttpStatus.UNAUTHORIZED).build();
        }
        
        try {
            Channel channel = channelRepository.findById(channelId)
                .orElseThrow(() -> new RuntimeException("Salon non trouvé"));
            
            ChannelDto channelDto = new ChannelDto(
                channel.getId(),
                channel.getName(),
                channel.getDescription(),
                channel.getCreatorUsername(),
                channel.getModeratorUsernames()
            );
            
            return ResponseEntity.ok(channelDto);
        } catch (Exception e) {
            return ResponseEntity.status(HttpStatus.INTERNAL_SERVER_ERROR)
                .body(new ErrorResponse("Erreur lors de la récupération du salon: " + e.getMessage()));
        }
    }

    @GetMapping("/{channelId}/admin-status")
    @Operation(summary = "Vérifie si l'utilisateur est administrateur d'un salon")
    @SecurityRequirement(name = "bearerAuth")
    public ResponseEntity<?> checkAdminStatus(
            @PathVariable Long channelId,
            Principal principal) {
        if (principal == null) {
            return ResponseEntity.status(HttpStatus.UNAUTHORIZED).build();
        }
        
        try {
            Channel channel = channelRepository.findById(channelId)
                .orElseThrow(() -> new RuntimeException("Salon non trouvé"));
            
            boolean isAdmin = channel.isAdmin(principal.getName());
            return ResponseEntity.ok(new AdminStatusDto(isAdmin));
        } catch (Exception e) {
            return ResponseEntity.status(HttpStatus.INTERNAL_SERVER_ERROR)
                .body(new ErrorResponse("Erreur lors de la vérification du statut admin: " + e.getMessage()));
        }
    }

    record ChannelDto(
        Long id, 
        String name, 
        String description,
        String creatorUsername,
        Set<String> moderatorUsernames
    ) {}

    record CreateChannelRequest(
        String name, 
        String description
    ) {}

    record PermissionsDto(
        boolean isAdmin, 
        boolean isModerator
    ) {}

    record AdminStatusDto(boolean isAdmin) {}

    // Classes pour les réponses
    record ErrorResponse(String message) {}
    record SuccessResponse(String message) {}
} 