package com.chat.app.controller;

import com.chat.app.model.Channel;
import com.chat.app.repository.ChannelRepository;
import lombok.RequiredArgsConstructor;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.security.access.prepost.PreAuthorize; // Pour sécuriser la création
import org.springframework.web.bind.annotation.*;

import io.swagger.v3.oas.annotations.Operation;
import io.swagger.v3.oas.annotations.Parameter;
import io.swagger.v3.oas.annotations.media.Content;
import io.swagger.v3.oas.annotations.media.Schema;
import io.swagger.v3.oas.annotations.responses.ApiResponse;
import io.swagger.v3.oas.annotations.responses.ApiResponses;
import io.swagger.v3.oas.annotations.tags.Tag;
import io.swagger.v3.oas.annotations.security.SecurityRequirement;

import java.security.Principal;
import java.util.List;
import java.util.stream.Collectors;

@RestController
@RequestMapping("/api/channels")
@RequiredArgsConstructor
@Tag(name = "Salons", description = "API de gestion des salons publics")
public class ChannelController {

    private final ChannelRepository channelRepository;

    // Lister tous les salons
    @GetMapping
    @Operation(summary = "Liste tous les salons", 
               description = "Récupère la liste de tous les salons disponibles")
    @ApiResponses(value = {
        @ApiResponse(responseCode = "200", description = "Liste des salons récupérée avec succès"),
        @ApiResponse(responseCode = "401", description = "Utilisateur non authentifié")
    })
    @SecurityRequirement(name = "bearerAuth")
    public ResponseEntity<List<ChannelDto>> getAllChannels(
            @Parameter(hidden = true) Principal principal) {
        // S'assurer que l'utilisateur est authentifié pour voir les salons
        if (principal == null) {
            return ResponseEntity.status(HttpStatus.UNAUTHORIZED).build();
        }
        List<Channel> channels = channelRepository.findAll();
        List<ChannelDto> channelDtos = channels.stream()
                .map(channel -> new ChannelDto(channel.getId(), channel.getName(), channel.getDescription()))
                .collect(Collectors.toList());
        return ResponseEntity.ok(channelDtos);
    }

    // Créer un nouveau salon 
    @PostMapping
    @Operation(summary = "Crée un nouveau salon", 
               description = "Permet de créer un nouveau salon public")
    @ApiResponses(value = {
        @ApiResponse(responseCode = "201", description = "Salon créé avec succès"),
        @ApiResponse(responseCode = "400", description = "Données invalides"),
        @ApiResponse(responseCode = "401", description = "Utilisateur non authentifié"),
        @ApiResponse(responseCode = "409", description = "Un salon avec ce nom existe déjà")
    })
    @SecurityRequirement(name = "bearerAuth")
    // @PreAuthorize("hasRole('ADMIN')") // Optionnel: Restreindre la création aux admins
    public ResponseEntity<?> createChannel(
            @RequestBody CreateChannelRequest request, 
            @Parameter(hidden = true) Principal principal) {
        if (principal == null) {
            return ResponseEntity.status(HttpStatus.UNAUTHORIZED).build();
        }
        if (request.name() == null || request.name().trim().isEmpty()) {
            return ResponseEntity.badRequest().body("Le nom du salon est obligatoire.");
        }
        if (channelRepository.existsByName(request.name().trim())) {
            return ResponseEntity.status(HttpStatus.CONFLICT).body("Un salon avec ce nom existe déjà.");
        }

        Channel newChannel = new Channel();
        newChannel.setName(request.name().trim());
        newChannel.setDescription(request.description());
        
        Channel savedChannel = channelRepository.save(newChannel);
        ChannelDto channelDto = new ChannelDto(savedChannel.getId(), savedChannel.getName(), savedChannel.getDescription());
        
        // On pourrait aussi notifier les clients via WebSocket de la création d'un nouveau salon
        // messagingTemplate.convertAndSend("/topic/channels.new", channelDto);

        return ResponseEntity.status(HttpStatus.CREATED).body(channelDto);
    }

    // --- DTOs ---
    @Schema(description = "DTO pour un salon")
    record ChannelDto(
        @Schema(description = "Identifiant unique du salon") Long id, 
        @Schema(description = "Nom du salon") String name, 
        @Schema(description = "Description du salon") String description
    ) {}
    
    @Schema(description = "Requête de création d'un salon")
    record CreateChannelRequest(
        @Schema(description = "Nom du salon", example = "General") String name, 
        @Schema(description = "Description du salon", example = "Salon de discussion général") String description
    ) {}
} 