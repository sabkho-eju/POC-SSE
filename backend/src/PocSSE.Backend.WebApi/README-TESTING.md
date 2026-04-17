# Guide de test de l'API PocSSE avec authentification JWT

## 🔐 Script de test avec authentification

Le script `test-api.ps1` teste automatiquement tous les endpoints de l'API avec authentification JWT.

## 🚀 Utilisation

### Utilisation simple (par défaut)

```powershell
.\src\PocSSE.Backend.WebApi\test-api.ps1
```

Par défaut, le script utilise :
- **URL**: `http://localhost:5236`
- **Username**: `admin`
- **Password**: `password123`

### Utilisation avec paramètres personnalisés

```powershell
# Avec un utilisateur différent
.\src\PocSSE.Backend.WebApi\test-api.ps1 -Username "demo" -Password "demo"

# Avec HTTPS
.\src\PocSSE.Backend.WebApi\test-api.ps1 -BaseUrl "https://localhost:7084"

# Avec tous les paramètres
.\src\PocSSE.Backend.WebApi\test-api.ps1 -BaseUrl "https://localhost:7084" -Username "user1" -Password "pass123"
```

## 👥 Utilisateurs disponibles (POC)

| Username   | Password       |
|------------|----------------|
| admin      | password123    |
| user1      | pass123        |
| testuser   | testpassword   |
| demo       | demo           |

## 📋 Déroulement du test

Le script exécute automatiquement les tests suivants dans l'ordre :

1. **Authentication Tests**
   - `POST /api/authentication/login` - Obtient un token JWT

2. **API Endpoint Tests** (avec le token JWT)
   - `GET /api/jobprocessing/test` - Vérifie que le contrôleur fonctionne
   - `POST /api/jobprocessing/process` - Traite un job

3. **Logout Test**
   - `POST /api/authentication/logout` - Se déconnecte (côté client)

## 📊 Résultat attendu

```
============================================================
  PocSSE API Test Suite with JWT Authentication
============================================================
Base URL: http://localhost:5236
Username: admin
Time: 2024-01-15 10:30:45

============================================================
  Authentication Tests
============================================================

[POST] /api/authentication/login
Description: Login to obtain JWT token
URL: http://localhost:5236/api/authentication/login

Request Body:
{
  "username": "admin",
  "password": "password123"
}

Sending request...
✓ SUCCESS (45ms)

Response:
{
  "success": true,
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "username": "admin",
  "message": "Login successful"
}

✓ Authentication successful! Token acquired.
Token (first 20 chars): eyJhbGciOiJIUzI1NiIs...
Logged in as: admin

============================================================
  API Endpoint Tests
============================================================

[GET] /api/jobprocessing/test
Description: Test endpoint to verify controller is working
URL: http://localhost:5236/api/jobprocessing/test
Using JWT Authentication

Sending request...
✓ SUCCESS (12ms)

...

============================================================
  Test Summary
============================================================
Total Tests: 4
Passed: 4
Failed: 0

✓ All tests passed!
```

## 🔧 Détails techniques

### Comment fonctionne l'authentification JWT

1. **Login** : Le script envoie username/password à `/api/authentication/login`
2. **Token JWT** : L'API retourne un token JWT signé avec la SecretKey
3. **Authentification** : Le token est envoyé dans l'en-tête `Authorization: Bearer {token}` pour chaque requête
4. **Logout** : Le token est simplement supprimé côté client (JWT est stateless)

### Structure du token JWT

Le token contient :
- **Claims** : 
  - `name` : nom d'utilisateur
  - `sub` : subject (username)
  - `jti` : JWT ID unique
- **Issuer** : `POC-SSE-Backend`
- **Audience** : `POC-SSE-Frontend`
- **Expiration** : 8 heures après création
- **Signature** : HMAC-SHA256 avec la SecretKey

### Paramètres du script

| Paramètre | Type   | Défaut               | Description                    |
|-----------|--------|----------------------|--------------------------------|
| BaseUrl   | string | http://localhost:5236| URL de base de l'API          |
| Username  | string | admin                | Nom d'utilisateur pour login  |
| Password  | string | password123          | Mot de passe pour login       |

## 🧪 Tests manuels avec curl

Si vous préférez tester manuellement :

```bash
# 1. Login
curl -X POST http://localhost:5236/api/authentication/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"password123"}'

# Copier le token de la réponse

# 2. Appeler un endpoint protégé
curl -X GET http://localhost:5236/api/jobprocessing/test \
  -H "Authorization: Bearer {votre-token-ici}"

# 3. Logout
curl -X POST http://localhost:5236/api/authentication/logout \
  -H "Authorization: Bearer {votre-token-ici}"
```

## ⚠️ Important

- Ce système d'authentification est **uniquement pour le POC**
- Les mots de passe sont en clair (ne JAMAIS faire en production)
- En production, utiliser :
  - Base de données pour les utilisateurs
  - Hachage des mots de passe (bcrypt, Argon2)
  - HTTPS obligatoire
  - Tokens courts avec refresh tokens
  - Azure AD / Identity Server

## 🐛 Dépannage

### "Authentication failed"
- Vérifiez que l'API est démarrée
- Vérifiez le username/password
- Consultez les logs de l'API

### "Unauthorized" (401)
- Le token a peut-être expiré (8h de validité)
- Relancez le script pour obtenir un nouveau token
- Vérifiez que la SecretKey est correctement configurée dans `appsettings.json`

### "Access denied to store" (certificat HTTPS)
- Utilisez le profil HTTP : `-BaseUrl "http://localhost:5236"`
- Ou exécutez `.\fix-https-certificate.ps1` en tant qu'administrateur
