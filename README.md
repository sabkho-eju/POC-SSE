# SsePoc-SPA - PoC SSE avec Vue.js + ASP.NET 10

## Objectif

Demonstrer un scenario SPA dans lequel:

1. Le client Vue.js lance un traitement via API.
2. Le backend execute ce traitement dans un `BackgroundService`.
3. Le serveur notifie le client initiateur via **Server-Sent Events (SSE)**.
4. Le front affiche une message box a la reception de l'evenement de fin.

## Structure

- `frontend/` : application Vue.js (Vite)
- `backend/` : API ASP.NET Core 10 + SSE + worker

Fichiers backend principaux:

- `backend/Program.cs`
- `backend/Services/BackgroundJobQueue.cs`
- `backend/Services/JobNotificationHub.cs`
- `backend/Services/JobProcessorWorker.cs`
- `backend/Backend.sln`

Fichiers frontend principaux:

- `frontend/src/App.vue`
- `frontend/src/style.css`

## Prerequis

- .NET SDK 10
- Node.js 20+ et npm

## Lancement en local

### 1) Demarrer le backend

Depuis `SsePoc-SPA/backend`:

```bash
dotnet run
```

Backend en local sur:

- `http://localhost:5010`

### 2) Demarrer le frontend

Depuis `SsePoc-SPA/frontend`:

```bash
npm install
npm run dev
```

Frontend en local (Vite):

- `http://localhost:5173`

## Endpoints backend

- `GET /api/health` : etat du service
- `GET /api/sse/stream?clientId=...` : flux SSE client
- `POST /api/jobs/start` : demarrage d'un job async
- `POST /api/jobs/broadcast` : envoi d'un evenement a tous les clients connectes

## Test de demonstration

1. Ouvrir `http://localhost:5173`.
2. Verifier que l'etat SSE passe a `connecte`.
3. Cliquer sur `Lancer un traitement`.
4. Attendre la fin du traitement (simule a 8s).
5. Verifier:
   - reception de l'evenement `job-completed`,
   - affichage de l'`alert` de fin,
   - logs mis a jour dans l'interface.

## Reponses de cadrage (SPA)

- **Ports supplementaires**: non, SSE passe sur HTTP/HTTPS standard.
- **Timeout**: garde-fou frontend (30s) + keep-alive SSE serveur (ping toutes les 15s).
- **F5**: reconnexion SSE automatique; le `clientId` est conserve en `sessionStorage`.
- **Broadcast**: supporte via `POST /api/jobs/broadcast`.

## Limites du PoC

- Donnees en memoire uniquement (pas de persistance durable).
- Pas de replay d'evenements manques.
- Pas d'authentification/autorisation des flux SSE.
- Pas de support multi-instance distribue.

## Pistes pour la production

1. Ajouter persistance de statut job (DB).
2. Utiliser un broker pour la file et la diffusion d'evenements.
3. Ajouter authN/authZ (JWT/cookies + isolation utilisateur/tenant).
4. Configurer correctement reverse proxy/load balancer pour SSE (timeouts, buffering).
