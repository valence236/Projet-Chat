package com.chat.app.model;

import jakarta.persistence.*;
import lombok.Data;
import lombok.NoArgsConstructor;
import lombok.AllArgsConstructor;

import java.time.LocalDateTime;
import java.util.HashSet;
import java.util.Set;

@Data
@NoArgsConstructor
@AllArgsConstructor
@Entity
@Table(name = "channels")
public class Channel {
    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;

    @Column(unique = true, nullable = false)
    private String name;

    private String description;

    @Column(nullable = false)
    private String creatorUsername;  // Username de l'administrateur

    @ElementCollection
    private Set<String> moderatorUsernames = new HashSet<>();  // Liste des modérateurs
    
    @ElementCollection
    private Set<String> blockedUsernames = new HashSet<>();  // Liste des utilisateurs bloqués

    @Column(nullable = false, updatable = false)
    private LocalDateTime createdAt = LocalDateTime.now();

    // Méthodes utilitaires pour vérifier les permissions
    public boolean isAdmin(String username) {
        return creatorUsername.equals(username);
    }

    public boolean isModerator(String username) {
        return moderatorUsernames.contains(username);
    }

    public boolean canModerateMessages(String username) {
        return isAdmin(username) || isModerator(username);
    }
    
    public boolean isBlocked(String username) {
        return blockedUsernames.contains(username);
    }
} 