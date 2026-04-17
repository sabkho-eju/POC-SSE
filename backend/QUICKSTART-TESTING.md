# 🚀 Quick Start - Testing PocSSE API with JWT Authentication

## ✅ Ce qui a été configuré

1. **SecretKey JWT** générée et configurée dans `appsettings.json`
2. **Script de test complet** avec authentification JWT (`test-api.ps1`)
3. **Script de test d'authentification** rapide (`test-auth.ps1`)
4. **Documentation** complète pour les tests

## 📋 Démarrage rapide (3 étapes)

### 1. Démarrer l'API

Dans Visual Studio :
- Sélectionnez le profil **"http"** (ou "https" si certificat installé)
- Appuyez sur **F5** ou **Ctrl+F5**

### 2. Tester l'authentification

```powershell
cd C:\dev\git\poc\POC-SSE\backend
.\src\PocSSE.Backend.WebApi\test-auth.ps1
```

Vous devriez voir :
```
✓ All authentication tests passed!
```

### 3. Tester l'API complète

```powershell
.\src\PocSSE.Backend.WebApi\test-api.ps1
```

Vous devriez voir :
```
✓ All tests passed!
```

## 🔑 Utilisateurs disponibles

| Username   | Password       | Description           |
|------------|----------------|-----------------------|
| admin      | password123    | Administrateur        |
| user1      | pass123        | Utilisateur standard  |
| testuser   | testpassword   | Utilisateur de test   |
| demo       | demo           | Compte de démo        |

## 📁 Fichiers créés/modifiés

### Scripts de test
- ✅ `src/PocSSE.Backend.WebApi/test-api.ps1` - Script principal avec JWT
- ✅ `src/PocSSE.Backend.WebApi/test-auth.ps1` - Test rapide authentification
- ✅ `fix-https-certificate.ps1` - Fix certificat HTTPS

### Documentation
- ✅ `HTTPS-CERTIFICATE-FIX.md` - Guide certificat HTTPS
- ✅ `src/PocSSE.Backend.WebApi/README-TESTING.md` - Guide complet des tests
- ✅ `src/PocSSE.Backend.WebApi/TESTING-EXAMPLES.md` - Exemples d'utilisation

### Configuration
- ✅ `src/PocSSE.Backend.WebApi/appsettings.json` - SecretKey JWT configurée

## 🎯 Commandes essentielles

### Test complet avec utilisateur par défaut (admin)
```powershell
.\src\PocSSE.Backend.WebApi\test-api.ps1
```

### Test avec un autre utilisateur
```powershell
.\src\PocSSE.Backend.WebApi\test-api.ps1 -Username "demo" -Password "demo"
```

### Test avec HTTPS
```powershell
.\src\PocSSE.Backend.WebApi\test-api.ps1 -BaseUrl "https://localhost:7084"
```

### Test authentification uniquement
```powershell
.\src\PocSSE.Backend.WebApi\test-auth.ps1
```

## 🔍 Comment ça fonctionne

### Flux d'authentification JWT

```
1. Login Request
   └─> POST /api/authentication/login
       Body: { username, password }

2. API Response
   └─> { success: true, token: "eyJ...", username: "admin" }

3. Authenticated Request
   └─> GET /api/jobprocessing/test
       Header: Authorization: Bearer eyJ...

4. API validates token
   └─> Returns data if valid
   └─> Returns 401 if invalid/expired
```

### Structure du script test-api.ps1

```powershell
# 1. Authentification
POST /api/authentication/login
  ├─> Récupère le token JWT
  └─> Stocke dans $Script:AuthToken

# 2. Tests des endpoints (avec token)
GET /api/jobprocessing/test
  └─> Header: Authorization: Bearer {token}

POST /api/jobprocessing/process
  └─> Header: Authorization: Bearer {token}

# 3. Déconnexion
POST /api/authentication/logout
  └─> Supprime le token localement
```

## 🐛 Résolution de problèmes

### L'API ne démarre pas
```powershell
# Vérifier que le port n'est pas déjà utilisé
Get-NetTCPConnection -LocalPort 5236

# Si occupé, trouver le processus
Get-Process -Id (Get-NetTCPConnection -LocalPort 5236).OwningProcess

# Stopper le processus
Stop-Process -Id <PID>
```

### Erreur "Access denied to store" (HTTPS)
```powershell
# Utiliser HTTP temporairement
.\src\PocSSE.Backend.WebApi\test-api.ps1 -BaseUrl "http://localhost:5236"

# Ou fixer le certificat (en tant qu'admin)
.\fix-https-certificate.ps1
```

### Les tests échouent
```powershell
# 1. Vérifier que l'API est démarrée
Test-NetConnection -ComputerName localhost -Port 5236

# 2. Vérifier les logs dans Visual Studio
# Output -> Show output from: ASP.NET Core Web Server

# 3. Tester l'authentification seule
.\src\PocSSE.Backend.WebApi\test-auth.ps1
```

### Token expiré (401)
Les tokens JWT expirent après **8 heures**. Relancez simplement le script pour obtenir un nouveau token.

## 📚 Pour aller plus loin

### Tester manuellement avec curl (Windows)
```bash
# Login
curl -X POST http://localhost:5236/api/authentication/login ^
  -H "Content-Type: application/json" ^
  -d "{\"username\":\"admin\",\"password\":\"password123\"}"

# Appeler un endpoint (remplacer {TOKEN} par le vrai token)
curl -X GET http://localhost:5236/api/jobprocessing/test ^
  -H "Authorization: Bearer {TOKEN}"
```

### Décoder un token JWT
```powershell
$token = "eyJ..." # Votre token

$parts = $token.Split('.')
$payload = [System.Text.Encoding]::UTF8.GetString(
    [Convert]::FromBase64String($parts[1] + "==")
)

$payload | ConvertFrom-Json | ConvertTo-Json -Depth 10
```

### Tester tous les utilisateurs en boucle
```powershell
@("admin", "user1", "testuser", "demo") | ForEach-Object {
    $pass = switch($_) {
        "admin" {"password123"}
        "user1" {"pass123"}
        "testuser" {"testpassword"}
        "demo" {"demo"}
    }
    Write-Host "`n=== Testing $_/$pass ===" -ForegroundColor Cyan
    .\src\PocSSE.Backend.WebApi\test-api.ps1 -Username $_ -Password $pass
}
```

## ⚠️ Important pour la production

Ce système est **uniquement pour le POC**. En production :

- ❌ Ne JAMAIS stocker les mots de passe en clair
- ❌ Ne JAMAIS commit la SecretKey dans Git
- ✅ Utiliser une base de données pour les utilisateurs
- ✅ Hacher les mots de passe (bcrypt, Argon2, PBKDF2)
- ✅ Utiliser HTTPS obligatoire
- ✅ Stocker la SecretKey dans Azure Key Vault
- ✅ Tokens courts (15-60 min) avec refresh tokens
- ✅ Implémenter rate limiting
- ✅ Considérer Azure AD / Identity Server

## 🎓 Ressources

- Documentation : `src/PocSSE.Backend.WebApi/README-TESTING.md`
- Exemples : `src/PocSSE.Backend.WebApi/TESTING-EXAMPLES.md`
- Fix HTTPS : `HTTPS-CERTIFICATE-FIX.md`
- JWT RFC : https://tools.ietf.org/html/rfc7519
- ASP.NET Core Auth : https://learn.microsoft.com/en-us/aspnet/core/security/authentication/

---

**Happy Testing! 🚀**
