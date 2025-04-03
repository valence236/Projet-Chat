package com.chat.app.config;

import com.chat.app.security.IJwtService;
import com.chat.app.service.IUserService;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.messaging.Message;
import org.springframework.messaging.MessageChannel;
import org.springframework.messaging.simp.stomp.StompCommand;
import org.springframework.messaging.simp.stomp.StompHeaderAccessor;
import org.springframework.messaging.support.ChannelInterceptor;
import org.springframework.messaging.support.MessageHeaderAccessor;
import org.springframework.security.authentication.UsernamePasswordAuthenticationToken;
import org.springframework.security.core.context.SecurityContextHolder;
import org.springframework.security.core.userdetails.UserDetails;
import org.springframework.stereotype.Component;

import java.util.List;

@Component
@RequiredArgsConstructor
@Slf4j // Pour les logs
public class WebSocketAuthInterceptor implements ChannelInterceptor {

    private final IJwtService jwtService;
    private final IUserService userService;

    @Override
    public Message<?> preSend(Message<?> message, MessageChannel channel) {
        StompHeaderAccessor accessor = MessageHeaderAccessor.getAccessor(message, StompHeaderAccessor.class);

        // Intercepter uniquement la commande CONNECT
        if (accessor != null && StompCommand.CONNECT.equals(accessor.getCommand())) {
            log.debug("Intercepting STOMP CONNECT message");

            // Récupérer le token depuis le header natif 'Authorization'
            // Le client DOIT envoyer ce header lors de la connexion STOMP
            List<String> authorization = accessor.getNativeHeader("Authorization");
            log.debug("Authorization header: {}", authorization);

            String jwt = null;
            if (authorization != null && !authorization.isEmpty()) {
                // Prend le premier header 'Authorization' trouvé
                String authHeader = authorization.get(0);
                if (authHeader != null && authHeader.startsWith("Bearer ")) {
                    jwt = authHeader.substring(7);
                    log.debug("JWT Token extracted: {}", jwt);
                } else {
                     log.warn("Authorization header does not start with Bearer");
                }
            } else {
                 log.warn("Authorization header not found in STOMP CONNECT");
            }


            if (jwt != null) {
                try {
                    String username = jwtService.extractUsername(jwt);
                    log.debug("Username extracted from JWT: {}", username);

                    if (username != null && SecurityContextHolder.getContext().getAuthentication() == null) {
                        UserDetails userDetails = userService.loadUserByUsername(username);

                        if (jwtService.isTokenValid(jwt, userDetails)) {
                            log.info("JWT token is valid for user: {}", username);
                            // Créer l'objet d'authentification Spring Security
                            UsernamePasswordAuthenticationToken authToken = new UsernamePasswordAuthenticationToken(
                                    userDetails,
                                    null, // Pas de credentials nécessaires ici
                                    userDetails.getAuthorities()
                            );
                            // Associer l'authentification à l'accessor STOMP (important!)
                            accessor.setUser(authToken);
                            log.debug("User set in StompHeaderAccessor: {}", authToken);
                            // Optionnel: Mettre dans SecurityContextHolder si nécessaire pour d'autres logiques synchrones
                            // SecurityContextHolder.getContext().setAuthentication(authToken);
                        } else {
                             log.warn("JWT token is invalid for user: {}", username);
                        }
                    }
                } catch (Exception e) {
                    // Log l'erreur de validation/extraction
                     log.error("Error validating JWT token during STOMP CONNECT: {}", e.getMessage());
                    // On pourrait rejeter la connexion ici, mais pour l'instant on logue juste
                    // throw new MessagingException("Invalid JWT Token"); // Décommentez pour rejeter
                }
            } else {
                log.warn("No JWT token found in STOMP CONNECT headers. Connection will be anonymous unless secured otherwise.");
                // Rejeter la connexion si l'authentification est obligatoire
                // throw new MessagingException("Missing JWT Token"); // Décommentez pour rejeter
            }
        }
        // Laisser passer le message (modifié ou non)
        return message;
    }
} 