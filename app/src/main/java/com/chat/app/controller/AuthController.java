package com.chat.app.controller;

import com.chat.app.model.User;
import com.chat.app.security.IJwtService;
import com.chat.app.service.AuthService;
import com.chat.app.service.IUserService;
import lombok.RequiredArgsConstructor;
import org.springframework.http.ResponseEntity;
import org.springframework.security.core.Authentication;
import org.springframework.security.core.userdetails.UserDetails;
import org.springframework.web.bind.annotation.*;

import io.swagger.v3.oas.annotations.Operation;
import io.swagger.v3.oas.annotations.media.Content;
import io.swagger.v3.oas.annotations.media.Schema;
import io.swagger.v3.oas.annotations.responses.ApiResponse;
import io.swagger.v3.oas.annotations.responses.ApiResponses;
import io.swagger.v3.oas.annotations.tags.Tag;
import io.swagger.v3.oas.annotations.security.SecurityRequirements;

import java.util.HashMap;
import java.util.Map;

@RestController
@RequestMapping("/api/auth")
@RequiredArgsConstructor
@Tag(name = "Authentification", description = "API d'authentification pour l'inscription et la connexion")
public class AuthController {
    private final AuthService authService;
    private final IUserService userService;
    private final IJwtService jwtService;

    @PostMapping("/register")
    @Operation(summary = "Inscription d'un nouvel utilisateur", 
               description = "Permet de créer un nouveau compte utilisateur")
    @ApiResponses(value = {
        @ApiResponse(responseCode = "200", description = "Utilisateur créé avec succès",
                     content = @Content(mediaType = "application/json")),
        @ApiResponse(responseCode = "400", description = "Données d'inscription invalides",
                     content = @Content(mediaType = "text/plain"))
    })
    @SecurityRequirements()  // Pas de sécurité requise pour cette opération
    public ResponseEntity<?> register(@RequestBody RegisterRequest request) {
        try {
            User user = authService.register(request.username(), request.password(), request.email());
            String token = jwtService.generateToken(user);
            
            Map<String, Object> response = new HashMap<>();
            response.put("user", user);
            response.put("token", token);
            
            return ResponseEntity.ok(response);
        } catch (RuntimeException e) {
            return ResponseEntity.badRequest().body(e.getMessage());
        }
    }

    @PostMapping("/login")
    @Operation(summary = "Connexion d'un utilisateur existant", 
               description = "Authentifie un utilisateur et retourne un token JWT")
    @ApiResponses(value = {
        @ApiResponse(responseCode = "200", description = "Authentification réussie",
                     content = @Content(mediaType = "application/json")),
        @ApiResponse(responseCode = "400", description = "Identifiants invalides",
                     content = @Content(mediaType = "text/plain"))
    })
    @SecurityRequirements()  // Pas de sécurité requise pour cette opération
    public ResponseEntity<?> login(@RequestBody LoginRequest request) {
        try {
            Authentication authentication = authService.login(request.username(), request.password());
            UserDetails userDetails = (UserDetails) authentication.getPrincipal();
            String token = jwtService.generateToken(userDetails);
            
            Map<String, Object> response = new HashMap<>();
            response.put("token", token);
            response.put("username", userDetails.getUsername());
            
            return ResponseEntity.ok(response);
        } catch (Exception e) {
            return ResponseEntity.badRequest().body("Identifiants invalides");
        }
    }
}

@Schema(description = "Demande d'inscription")
record RegisterRequest(
    @Schema(description = "Nom d'utilisateur", example = "johndoe") String username, 
    @Schema(description = "Mot de passe", example = "password123") String password, 
    @Schema(description = "Adresse email", example = "john.doe@example.com") String email
) {}

@Schema(description = "Demande de connexion")
record LoginRequest(
    @Schema(description = "Nom d'utilisateur", example = "johndoe") String username, 
    @Schema(description = "Mot de passe", example = "password123") String password
) {} 