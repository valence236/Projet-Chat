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