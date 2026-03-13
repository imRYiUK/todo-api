# Todo API - ASP.NET Core + MongoDB + JWT

Ce projet implémente une API Todo en ASP.NET Core avec MongoDB et une sécurité par authentification JWT + rôles.

## Fonctionnalités

- API REST pour gérer les Todos
- Stockage MongoDB
- Authentification JWT
- Autorisation par rôles:
  - `admin`: créer, modifier, supprimer des todos
  - `user` et `admin`: lire les todos
- Démarrage local avec Docker (`docker-compose`)

## Prérequis

- .NET 9 SDK (si exécution hors Docker)
- Docker + Docker Compose (recommandé)

## Utilisateurs de démonstration

Créés automatiquement au démarrage (seed):

- `admin` / `Admin123!` (rôle `admin`)
- `user` / `User123!` (rôle `user`)

## Exécution avec Docker (recommandé)

1. Construire et démarrer les conteneurs:

```bash
docker compose up --build
```

2. Ouvrir Swagger:

- [http://localhost:5013/swagger](http://localhost:5013/swagger)

3. Obtenir un token JWT:

- Endpoint: `POST /api/auth/login`
- Body exemple:

```json
{
  "username": "admin",
  "password": "Admin123!"
}
```

4. Dans Swagger:

- Cliquer sur `Authorize`
- Saisir: `Bearer <votre_token>`

5. Tester les endpoints:

- `GET /api/todos` accessible avec `user` ou `admin`
- `POST /api/todos`, `PUT /api/todos/{id}`, `DELETE /api/todos/{id}` réservés à `admin`

## Exécution locale sans Docker

1. Lancer l'API:

```bash
dotnet restore
dotnet run
```

2. Ouvrir Swagger:

- [http://localhost:5013/swagger](http://localhost:5013/swagger)

3. Base URL API locale:

- [http://localhost:5013](http://localhost:5013)

4. Notes environnement local:

- En `Development`, `UseInMemoryStore=true` (pas de MongoDB obligatoire pour tester rapidement).
- Si vous voulez forcer MongoDB en local, mettez `UseInMemoryStore=false` dans `appsettings.Development.json` et démarrez MongoDB sur `mongodb://localhost:27017`.

## Configuration

Le fichier `appsettings.json` contient:

- `TodoDatabase`: connexion MongoDB et noms de collections
- `Jwt`: paramètres du token (`Issuer`, `Audience`, `Key`)
- `SeedUsers`: utilisateurs créés au démarrage

Pour la production, remplacez la clé `Jwt:Key` par une vraie clé secrète forte.

## Déploiement (alternative à Azure)

Comme demandé, ce projet est prêt pour un hébergement cloud non-Azure via conteneur Docker:

- Build de l'image avec le `Dockerfile`
- Déploiement sur une plateforme compatible Docker (ex: GitLab Container Registry + runner, Render, Railway, VPS)

Exemple rapide de build/push vers GitLab Registry:

```bash
docker build -t registry.gitlab.com/<namespace>/<projet>/todoapi:latest .
docker push registry.gitlab.com/<namespace>/<projet>/todoapi:latest
```

## Fichiers importants

- `Program.cs`: configuration DI, JWT, Swagger, middlewares
- `Controllers/AuthController.cs`: login et génération JWT
- `Controllers/TodosController.cs`: endpoints Todo sécurisés
- `Services/TodoService.cs`: accès MongoDB pour les todos
- `Services/UserService.cs`: accès utilisateurs + validation credentials
- `Services/DataSeeder.cs`: création des utilisateurs initiaux
- `docker-compose.yml`: stack locale API + MongoDB
- `TodoApi.http`: requêtes prêtes à l'emploi
