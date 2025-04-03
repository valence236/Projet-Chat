package com.chat.app.config;

import io.swagger.v3.oas.models.OpenAPI;
import io.swagger.v3.oas.models.info.Info;
import io.swagger.v3.oas.models.info.Contact;
import io.swagger.v3.oas.models.info.License;
import io.swagger.v3.oas.models.Components;
import io.swagger.v3.oas.models.security.SecurityScheme;
import io.swagger.v3.oas.models.security.SecurityRequirement;

import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;

@Configuration
public class SwaggerConfig {

    @Bean
    public OpenAPI chatAppOpenAPI() {
        final String securitySchemeName = "bearerAuth";
        
        return new OpenAPI()
            .info(new Info()
                .title("API de Chat Application")
                .description("API REST et WebSocket pour l'application de messagerie en temps réel")
                .version("1.0.0")
                .contact(new Contact()
                    .name("Équipe de développement")
                    .email("contact@example.com"))
                .license(new License()
                    .name("Apache 2.0")
                    .url("http://www.apache.org/licenses/LICENSE-2.0.html")))
            .components(new Components()
                .addSecuritySchemes(securitySchemeName, 
                    new SecurityScheme()
                        .type(SecurityScheme.Type.HTTP)
                        .scheme("bearer")
                        .bearerFormat("JWT")))
            .addSecurityItem(new SecurityRequirement().addList(securitySchemeName));
    }
} 