# Application Spring Boot avec Docker

Ce projet est une application Spring Boot conteneurisée avec Docker, incluant une base de données PostgreSQL.

## Prérequis

- Docker
- Docker Compose
- Java 17
- Maven

## Structure du Projet

```
app/
├── src/
├── Dockerfile
├── docker-compose.yml
└── pom.xml
```

## Configuration Docker

### Dockerfile

Le projet utilise un build multi-stage pour optimiser l'image finale :

1. **Stage de Build** :
   - Utilise `maven:3.9.6-eclipse-temurin-17-alpine`
   - Compile l'application
   - Génère le fichier JAR

2. **Stage d'Exécution** :
   - Utilise `eclipse-temurin:17-jre-alpine`
   - Image légère pour l'environnement de production
   - Contient uniquement le JAR et le JRE

### Docker Compose

Le fichier `docker-compose.yml` configure deux services :

1. **Application (app)** :
   - Port : 8080
   - Dépend du service de base de données
   - Variables d'environnement configurées pour Spring Boot

2. **Base de données (db)** :
   - PostgreSQL 15
   - Port : 5432
   - Volume persistant pour les données

## Configuration de la Base de Données

- **Database** : chatapp
- **Username** : postgres
- **Password** : postgres
- **URL** : jdbc:postgresql://db:5432/chatapp

## Démarrage

cd app

1. Construire et démarrer les conteneurs :
```bash
docker-compose up --build
```

2. Pour exécuter en arrière-plan :
```bash
docker-compose up -d
```

3. Pour arrêter les conteneurs :
```bash
docker-compose down
```

## Accès aux Services

- **Application** : http://localhost:8080
- **Base de données** : localhost:5432

## Volumes et Persistance

Les données de PostgreSQL sont persistantes grâce au volume `postgres_data`.

## Réseau

Un réseau dédié `spring-network` est créé pour la communication entre les conteneurs.

## Variables d'Environnement

### Application Spring Boot
```yaml
SPRING_DATASOURCE_URL=jdbc:postgresql://db:5432/chatapp
SPRING_DATASOURCE_USERNAME=postgres
SPRING_DATASOURCE_PASSWORD=postgres
SPRING_JPA_HIBERNATE_DDL_AUTO=update
```

### PostgreSQL
```yaml
POSTGRES_DB=chatapp
POSTGRES_USER=postgres
POSTGRES_PASSWORD=postgres
``` 

# Se connecter au conteneur PostgreSQL
docker-compose exec db psql -U postgres -d chatapp

# Une fois connecté, vous pouvez utiliser ces commandes :
# Lister toutes les tables
\dt

# Voir la structure d'une table spécifique (par exemple users)
\d users

# Voir le contenu d'une table
SELECT * FROM users;

# Commande pour rendre la colonne 'read' nullable (si nécessaire)
docker-compose exec -T db psql -U postgres -d chatapp -c 'ALTER TABLE messages ALTER COLUMN "read" DROP NOT NULL;'



docker-compose exec -T db psql -U postgres -d chatapp -c 'ALTER TABLE messages ALTER COLUMN "recipient_id" DROP NOT NULL;'


## Connexion Frontend (.NET WPF / Autres)

Ce backend expose des API REST et une connexion WebSocket/STOMP pour l'interaction avec un frontend.

### 1. API REST (Authentification, Données)

Les endpoints REST sont exposés sur `http://localhost:8080` (ou l'adresse où l'application `app` est accessible).

*   **Inscription :** `POST /api/auth/register`
    *   Body (JSON) : `{ "username": "...", "email": "...", "password": "..." }`
    *   Réponse (Succès) : `{ "user": { "id": ..., "username": "...", "email": "..." }, "token": "jwt_token..." }`
*   **Connexion :** `POST /api/auth/login`
    *   Body (JSON) : `{ "username": "...", "password": "..." }`
    *   Réponse (Succès) : `{ "token": "jwt_token...", "username": "..." }`
*   **Lister les Utilisateurs :** `GET /api/users`
    *   **Nécessite Authentification :** Header `Authorization: Bearer <jwt_token>`
    *   Réponse (Succès) : `[ { "username": "..." }, ... ]`
*   **Lister les Salons Publics :** `GET /api/channels`
    *   **Nécessite Authentification :** Header `Authorization: Bearer <jwt_token>`
    *   Réponse (Succès) : `[ { "id": 1, "name": "Nom du salon", "description": "Description du salon" }, ... ]`
*   **Créer un Salon Public :** `POST /api/channels`
    *   **Nécessite Authentification :** Header `Authorization: Bearer <jwt_token>`
    *   Body (JSON) : `{ "name": "Nom du salon", "description": "Description du salon" }`
    *   Réponse (Succès) : `{ "id": 1, "name": "Nom du salon", "description": "Description du salon" }`
*   **Historique de Conversation Privée :** `GET /api/messages/user/{otherUsername}`
    *   **Nécessite Authentification :** Header `Authorization: Bearer <jwt_token>`
    *   Remplacer `{otherUsername}` par le nom de l'autre utilisateur.
    *   Réponse (Succès) : `[ { "id": ..., "senderUsername": "...", "recipientUsername": "...", "content": "...", "timestamp": "..." }, ... ]`
*   **Historique de Salon Public :** `GET /api/messages/channel/{channelId}`
    *   **Nécessite Authentification :** Header `Authorization: Bearer <jwt_token>`
    *   Remplacer `{channelId}` par l'ID du salon.
    *   Réponse (Succès) : `[ { "id": ..., "senderUsername": "...", "channelId": ..., "content": "...", "timestamp": "..." }, ... ]`
*   **Historique du Chat Public Général :** `GET /api/messages/public`
    *   **Nécessite Authentification :** Header `Authorization: Bearer <jwt_token>`
    *   Réponse (Succès) : `[ { "id": ..., "senderUsername": "...", "content": "...", "timestamp": "..." }, ... ]`

**Note pour .NET WPF :** Utilisez `HttpClient` pour effectuer ces requêtes. N'oubliez pas d'ajouter le header `Authorization` pour les endpoints sécurisés et de désérialiser les réponses JSON.

### 2. WebSocket / STOMP (Messages Temps Réel)

Le backend utilise STOMP par-dessus WebSocket pour la communication en temps réel.

*   **URL de Connexion WebSocket :** `ws://localhost:8080/ws/websocket`
    *   Note : Utilisez `/ws/websocket` pour une connexion WebSocket brute (recommandé pour les clients non-JS). L'endpoint `/ws` est principalement pour SockJS.
*   **Protocole :** STOMP v1.1 / v1.2

**Flux STOMP typique :**

1.  **Établir la connexion WebSocket** à l'URL ci-dessus.
2.  **Envoyer la trame `CONNECT` STOMP :**
    *   **Important :** Inclure un header `Authorization: Bearer <jwt_token>` dans les **headers STOMP** pour l'authentification.
    ```stomp
    CONNECT
    accept-version:1.1,1.2
    host:localhost
    Authorization: Bearer <jwt_token>
    heart-beat:10000,10000

     
    ```
3.  **Attendre la trame `CONNECTED`** du serveur.
4.  **Envoyer des trames `SUBSCRIBE`** pour écouter les messages :
    *   Messages publics (chat général) : `SUBSCRIBE id:sub-public destination:/topic/public  `
    *   Messages d'un salon public spécifique : `SUBSCRIBE id:sub-channel-1 destination:/topic/channel.1  `
        *   Remplacer `1` par l'ID du salon concerné.
    *   Messages privés (pour l'utilisateur connecté) : `SUBSCRIBE id:sub-private destination:/user/queue/messages  `
        *   Note : Le backend route automatiquement vers `/user/{username}/queue/messages`. Le client s'abonne simplement à `/user/queue/messages` après authentification.
5.  **Envoyer des trames `SEND`** pour envoyer des messages :
    *   Destination : `/app/chat.sendMessage`
    *   Header : `content-type:application/json`
    *   Body (JSON) pour message privé : `{ "content": "...", "recipientUsername": "..." }`
    *   Body (JSON) pour message dans un salon : `{ "content": "...", "channelId": 1 }`
    *   Body (JSON) pour message public général : `{ "content": "..." }` (sans recipientUsername ni channelId)
    ```stomp
    SEND
    destination:/app/chat.sendMessage
    content-type:application/json

    {"content":"Votre message ici","channelId":1}
    ```
6.  **Recevoir des trames `MESSAGE`** sur les destinations auxquelles vous êtes abonné.
    *   Le body contiendra le JSON du `MessageDto`.
7.  **Envoyer `DISCONNECT`** pour fermer proprement la session STOMP.

**Note pour .NET WPF :** Utilisez `ClientWebSocket` pour la connexion WebSocket. Vous aurez besoin d'une bibliothèque STOMP pour .NET (cherchez sur NuGet, ex: `StompSharp`, `Stomp.Net`) pour gérer facilement l'envoi et la réception des trames STOMP formatées.

### 3. Fonctionnalités des Salons Publics

L'application permet de créer et rejoindre des salons publics pour discuter avec plusieurs utilisateurs simultanément.

**Création d'un salon :**
1. Connectez-vous à l'application
2. Cliquez sur le bouton "Créer un salon" dans la barre latérale
3. Saisissez un nom unique pour le salon et une description (optionnelle)
4. Cliquez sur "Créer" pour créer le salon

**Rejoindre un salon :**
1. Les salons existants sont affichés dans la liste des salons publics
2. Cliquez sur un salon pour le rejoindre et commencer à discuter
3. Tous les messages envoyés dans un salon sont visibles par tous les utilisateurs qui y sont connectés

**Modèle de données :**
- Chaque salon (`Channel`) a un identifiant unique, un nom et une description
- Les messages envoyés dans un salon ont une référence au salon via `channelId` mais pas de destinataire spécifique (`recipientId` est null)
- Le format de sujet WebSocket pour les salons est `/topic/channel.{id}` où `{id}` est l'identifiant du salon

### 4. Documentation API avec Swagger

L'application intègre Swagger (SpringDoc OpenAPI) pour fournir une documentation interactive des APIs REST.

**Accès à la documentation Swagger :**
- Interface Swagger UI : `http://localhost:8080/swagger-ui.html`
- Spécification OpenAPI au format JSON : `http://localhost:8080/v3/api-docs`

**Fonctionnalités de la documentation :**
- Description détaillée de tous les endpoints REST
- Schémas des modèles de données (DTOs)
- Possibilité de tester les APIs directement depuis l'interface
- Support de l'authentification JWT pour tester les endpoints sécurisés

**Utilisation avec l'authentification :**
1. D'abord, exécutez une requête vers `/api/auth/login` ou `/api/auth/register` pour obtenir un token JWT
2. Cliquez sur le bouton "Authorize" en haut de la page Swagger UI
3. Entrez votre token JWT au format `Bearer xxxxx...` (où xxxxx... est votre token)
4. Vous pouvez maintenant tester les endpoints sécurisés