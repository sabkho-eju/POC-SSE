# Exemples d'utilisation du script test-api.ps1

## Scénarios de test

### 1. Test rapide avec l'utilisateur admin (par défaut)
```powershell
.\src\PocSSE.Backend.WebApi\test-api.ps1
```

### 2. Test avec un utilisateur différent
```powershell
# Utilisateur demo
.\src\PocSSE.Backend.WebApi\test-api.ps1 -Username "demo" -Password "demo"

# Utilisateur user1
.\src\PocSSE.Backend.WebApi\test-api.ps1 -Username "user1" -Password "pass123"

# Utilisateur testuser
.\src\PocSSE.Backend.WebApi\test-api.ps1 -Username "testuser" -Password "testpassword"
```

### 3. Test avec HTTPS (nécessite certificat installé)
```powershell
.\src\PocSSE.Backend.WebApi\test-api.ps1 -BaseUrl "https://localhost:7084"
```

### 4. Test avec mauvais credentials (devrait échouer)
```powershell
.\src\PocSSE.Backend.WebApi\test-api.ps1 -Username "admin" -Password "wrongpassword"
```

### 5. Test avec tous les paramètres
```powershell
.\src\PocSSE.Backend.WebApi\test-api.ps1 `
    -BaseUrl "https://localhost:7084" `
    -Username "admin" `
    -Password "password123"
```

## Workflow de développement recommandé

### Démarrage de la session de développement
```powershell
# 1. Démarrer l'API dans Visual Studio (Ctrl+F5)

# 2. Dans un terminal PowerShell, tester l'API
cd C:\dev\git\poc\POC-SSE\backend
.\src\PocSSE.Backend.WebApi\test-api.ps1

# 3. Vérifier que tous les tests passent
# ✓ All tests passed!
```

### Test après modifications du code
```powershell
# 1. Modifier le code dans Visual Studio

# 2. Redémarrer l'API (Ctrl+Shift+F5)

# 3. Relancer les tests
.\src\PocSSE.Backend.WebApi\test-api.ps1

# 4. Vérifier qu'aucun test n'est cassé
```

### Test d'intégration avec différents utilisateurs
```powershell
# Test avec tous les utilisateurs disponibles
@("admin", "user1", "testuser", "demo") | ForEach-Object {
    $password = switch ($_) {
        "admin" { "password123" }
        "user1" { "pass123" }
        "testuser" { "testpassword" }
        "demo" { "demo" }
    }

    Write-Host "`n========== Testing with user: $_ ==========" -ForegroundColor Magenta
    .\src\PocSSE.Backend.WebApi\test-api.ps1 -Username $_ -Password $password
}
```

## Tests manuels avec Invoke-RestMethod

Si vous voulez tester manuellement des endpoints spécifiques :

```powershell
$baseUrl = "http://localhost:5236"

# 1. Login
$loginResponse = Invoke-RestMethod `
    -Uri "$baseUrl/api/authentication/login" `
    -Method POST `
    -Body (@{ username = "admin"; password = "password123" } | ConvertTo-Json) `
    -ContentType "application/json"

$token = $loginResponse.token
Write-Host "Token: $token"

# 2. Utiliser le token pour appeler un endpoint
$headers = @{
    "Authorization" = "Bearer $token"
}

$response = Invoke-RestMethod `
    -Uri "$baseUrl/api/jobprocessing/test" `
    -Method GET `
    -Headers $headers

$response | ConvertTo-Json

# 3. Process a job
$jobResponse = Invoke-RestMethod `
    -Uri "$baseUrl/api/jobprocessing/process" `
    -Method POST `
    -Headers $headers `
    -Body (@{ jobId = "manual-test-123"; jobData = "Test data" } | ConvertTo-Json) `
    -ContentType "application/json"

$jobResponse | ConvertTo-Json

# 4. Logout
$logoutResponse = Invoke-RestMethod `
    -Uri "$baseUrl/api/authentication/logout" `
    -Method POST `
    -Headers $headers

$logoutResponse | ConvertTo-Json
```

## Débogage

### Voir les détails d'une requête qui échoue
```powershell
# Activer le mode verbose
$VerbosePreference = "Continue"
.\src\PocSSE.Backend.WebApi\test-api.ps1

# Désactiver après
$VerbosePreference = "SilentlyContinue"
```

### Tester la connectivité de base
```powershell
# Vérifier que l'API répond
Test-NetConnection -ComputerName localhost -Port 5236

# Vérifier que l'endpoint /api/authentication/login est accessible
Invoke-WebRequest -Uri "http://localhost:5236/api/authentication/login" -Method OPTIONS
```

### Capturer le token JWT pour analyse
```powershell
# Login et sauvegarder le token
$response = Invoke-RestMethod `
    -Uri "http://localhost:5236/api/authentication/login" `
    -Method POST `
    -Body (@{ username = "admin"; password = "password123" } | ConvertTo-Json) `
    -ContentType "application/json"

$token = $response.token

# Sauvegarder le token dans un fichier
$token | Out-File -FilePath "jwt-token.txt"

# Décoder le token JWT (header et payload seulement, pas la signature)
$parts = $token.Split('.')
$header = [System.Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($parts[0]))
$payload = [System.Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($parts[1] + "=="))

Write-Host "JWT Header:" -ForegroundColor Cyan
$header | ConvertFrom-Json | ConvertTo-Json

Write-Host "`nJWT Payload:" -ForegroundColor Cyan
$payload | ConvertFrom-Json | ConvertTo-Json
```

## Automatisation CI/CD

### Script pour pipeline Azure DevOps / GitHub Actions
```powershell
# test-api-ci.ps1
param(
    [string]$BaseUrl = "http://localhost:5236",
    [int]$MaxRetries = 3,
    [int]$RetryDelaySeconds = 5
)

$retryCount = 0
$success = $false

while (-not $success -and $retryCount -lt $MaxRetries) {
    try {
        Write-Host "Attempt $($retryCount + 1) of $MaxRetries..."

        # Attendre que l'API soit prête
        Start-Sleep -Seconds $RetryDelaySeconds

        # Exécuter les tests
        .\src\PocSSE.Backend.WebApi\test-api.ps1 -BaseUrl $BaseUrl

        $success = $LASTEXITCODE -eq 0

        if (-not $success) {
            throw "Tests failed"
        }
    }
    catch {
        $retryCount++
        if ($retryCount -ge $MaxRetries) {
            Write-Error "All test attempts failed"
            exit 1
        }
    }
}

Write-Host "✓ Tests passed successfully" -ForegroundColor Green
exit 0
```
