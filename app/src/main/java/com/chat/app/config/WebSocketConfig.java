package com.chat.app.config;

import org.springframework.context.annotation.Configuration;
import org.springframework.messaging.simp.config.MessageBrokerRegistry;
import org.springframework.web.socket.config.annotation.EnableWebSocketMessageBroker;
import org.springframework.web.socket.config.annotation.StompEndpointRegistry;
import org.springframework.web.socket.config.annotation.WebSocketMessageBrokerConfigurer;
import org.springframework.messaging.simp.config.ChannelRegistration;
import org.springframework.beans.factory.annotation.Autowired;

@Configuration
@EnableWebSocketMessageBroker // Active le broker de messages WebSocket
public class WebSocketConfig implements WebSocketMessageBrokerConfigurer {

    @Autowired // Injecte l'intercepteur créé
    private WebSocketAuthInterceptor webSocketAuthInterceptor;

    @Override
    public void configureMessageBroker(MessageBrokerRegistry config) {
        // Préfixe pour les destinations gérées par le broker (ex: /topic/public, /queue/private)
        config.enableSimpleBroker("/topic", "/queue"); 
        // Préfixe pour les destinations mappées aux méthodes @MessageMapping dans les contrôleurs
        config.setApplicationDestinationPrefixes("/app");
        // Optionnel : Configurer le préfixe pour les destinations spécifiques à l'utilisateur
        // Par défaut, c'est /user/. Utilisé pour simpMessagingTemplate.convertAndSendToUser
        // config.setUserDestinationPrefix("/user"); 
    }

    @Override
    public void registerStompEndpoints(StompEndpointRegistry registry) {
        // Point de terminaison que le client utilisera pour se connecter au serveur WebSocket
        // withSockJS() est utilisé pour fournir une solution de repli si WebSocket n'est pas disponible
        registry.addEndpoint("/ws")
                .withSockJS(); 
        // Vous pourriez ajouter d'autres points de terminaison si nécessaire
    }

    // Enregistrer l'intercepteur pour le canal entrant
    @Override
    public void configureClientInboundChannel(ChannelRegistration registration) {
        registration.interceptors(webSocketAuthInterceptor);
    }

    // On pourrait aussi configurer le canal sortant si nécessaire
    // @Override
    // public void configureClientOutboundChannel(ChannelRegistration registration) {
    //     // ...
    // }

    // Vous pourriez ajouter ici la configuration de sécurité pour WebSocket si nécessaire
    // Par exemple, intégrer avec Spring Security pour authentifier les connexions WebSocket

} 