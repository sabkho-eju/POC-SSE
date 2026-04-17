# Presentation PoC - SSE SPA (.NET 10 + Vue.js)

## 1) Contexte

Nous devons valider qu'une application SPA peut:

- lancer un traitement asynchrone cote serveur,
- etre notifiee en temps reel quand le traitement se termine,
- sans polling actif cote client.

Le PoC cible une communication **server-to-client** simple et unidirectionnelle via **Server-Sent Events (SSE)**.

## 2) Objectif du PoC

Prouver le scenario bout en bout suivant:

1. La SPA Vue.js envoie une commande au backend.
2. Le backend demarre un traitement asynchrone en arriere-plan.
3. A la fin du traitement, le backend pousse un evenement SSE.
4. Le client initiateur recoit l'evenement et affiche une confirmation utilisateur.

## 3) Perimetre

Inclus:

- Front SPA Vue.js (Vite)
- API ASP.NET Core 10
- Worker asynchrone (`BackgroundService`)
- Notification SSE ciblee par client
- Broadcast de demonstration

Hors perimetre:

- persistance metier complete
- securisation avancee des flux
- architecture distribuee multi-noeuds production

## 4) Architecture retenue

### Frontend

- Ouvre un flux SSE: `GET /api/sse/stream?clientId=...`
- Envoie la commande metier: `POST /api/jobs/start`
- Gere timeout fonctionnel et retour utilisateur

### Backend

- `BackgroundJobQueue`: file en memoire
- `JobProcessorWorker`: execute les jobs
- `JobNotificationHub`: route les notifications SSE par `clientId`

### Flux de bout en bout

1. `POST /api/jobs/start`
2. Job queue
3. Worker execute
4. `job-completed` envoye sur flux SSE
5. UI notifiee

## 5) Resultats observes

- Le client initiateur recoit bien l'evenement de fin.
- Aucune ouverture de port supplementaire n'est necessaire.
- La reconnexion SSE apres refresh navigateur est fonctionnelle.
- Le broadcast vers plusieurs clients connectes est possible.

## 6) Reponses aux questions projet

### Ports supplementaires

Non. SSE utilise HTTP/HTTPS standard (meme port que l'API).

### Timeout / evenement jamais recu

- Keep-alive SSE serveur (ping periodique)
- Timeout metier cote front (30s) avec message utilisateur

### Comportement sur F5

- La connexion SSE est recreee automatiquement.
- Le `clientId` est conserve en `sessionStorage` pour garder l'identite d'onglet.

### Broadcast

Oui. Le backend expose `POST /api/jobs/broadcast` pour publier a tous les clients connectes.

## 7) Decision technique proposee

Pour les besoins de notification unidirectionnelle serveur vers client, **SSE est une option viable**:

- plus simple que WebSocket si la bidirection n'est pas necessaire,
- natif cote navigateur (`EventSource`),
- integration rapide dans une architecture ASP.NET + SPA.

## 8) Limites identifiees

- Etat en memoire (pas durable)
- Pas de replay d'evenements apres deconnexion
- Non adapte tel quel a un deploiement multi-instance
- Securite minimale dans le PoC

## 9) Risques en production

- Perte d'evenement sur coupure reseau/reload
- Probleme de buffering/timeouts proxy si non configures
- Difficulte de routage en scale-out sans bus de messages

## 10) Plan de passage a l'echelle

1. Persister statut et historique des jobs (DB).
2. Externaliser file/notification (broker ou bus d'evenements).
3. Ajouter authN/authZ et isolation multi-tenant.
4. Configurer reverse proxy/load balancer pour SSE.
5. Ajouter endpoint de consultation d'etat (`GET /jobs/{id}`) pour rattrapage.

## 11) Critere de validation final

Le PoC est valide si le client qui initie la commande recoit une notification de fin de traitement via SSE, de facon reproductible, sans polling.
