# Solution pour le problème de certificat HTTPS

## Problème
Visual Studio affiche "Accès refusé au store" lors de l'installation du certificat de développement HTTPS.

## Solutions

### ✅ Solution 1 : Installer le certificat avec PowerShell (Recommandé)

1. **Ouvrez PowerShell en tant qu'administrateur** :
   - Cliquez sur le bouton Windows
   - Tapez "PowerShell"
   - **Clic droit** sur "Windows PowerShell"
   - Sélectionnez **"Exécuter en tant qu'administrateur"**

2. **Naviguez vers le répertoire du projet** :
   ```powershell
   cd C:\dev\git\poc\POC-SSE\backend
   ```

3. **Exécutez le script de correction** :
   ```powershell
   .\fix-https-certificate.ps1
   ```

4. **Redémarrez Visual Studio**

5. **Lancez l'application avec le profil "https"**

### ✅ Solution 2 : Commandes manuelles (Alternative)

Si le script ne fonctionne pas, exécutez ces commandes **en tant qu'administrateur** :

```powershell
# Nettoyer les certificats existants
dotnet dev-certs https --clean

# Créer et faire confiance au nouveau certificat
dotnet dev-certs https --trust

# Vérifier l'installation
dotnet dev-certs https --check --trust
```

### ✅ Solution 3 : Utiliser HTTP (Contournement temporaire)

Si vous ne pouvez pas résoudre le problème immédiatement :

1. **Dans Visual Studio** :
   - Sélectionnez le profil **"http"** au lieu de "https" dans la barre d'outils de débogage
   - Ou modifiez `launchSettings.json` pour utiliser le profil "http" par défaut

2. **Pour tester l'API** :
   ```powershell
   # Le script test-api.ps1 utilise maintenant HTTP par défaut
   .\src\PocSSE.Backend.WebApi\test-api.ps1

   # Pour forcer HTTPS (si le certificat est installé) :
   .\src\PocSSE.Backend.WebApi\test-api.ps1 -BaseUrl "https://localhost:7084"
   ```

## Profils de lancement disponibles

- **http** : `http://localhost:5236` (pas de certificat nécessaire)
- **https** : `https://localhost:7084` (nécessite un certificat de développement)

## Troubleshooting

### Si vous obtenez toujours "Accès refusé"

1. **Fermez toutes les instances de Visual Studio**
2. **Redémarrez votre ordinateur** (parfois nécessaire pour libérer le store de certificats)
3. **Réexécutez le script** `fix-https-certificate.ps1` en tant qu'administrateur

### Si le certificat est installé mais non fiable

```powershell
# En tant qu'administrateur
dotnet dev-certs https --trust
```

### Si vous voulez tout réinitialiser

```powershell
# En tant qu'administrateur
dotnet dev-certs https --clean
dotnet dev-certs https --trust
```

## Vérification

Pour vérifier que tout fonctionne :

1. **Démarrez l'application** dans Visual Studio
2. **Exécutez le script de test** :
   ```powershell
   .\src\PocSSE.Backend.WebApi\test-api.ps1
   ```

Vous devriez voir :
```
✓ All tests passed!
```
