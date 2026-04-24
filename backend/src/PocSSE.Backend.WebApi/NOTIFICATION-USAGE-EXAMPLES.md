# Exemples d'utilisation du nouveau système de notifications

## Exemple 1: Endpoint SSE pour surveiller les jobs

```csharp
[ApiController]
[Route("api/jobs")]
public class JobMonitoringController : ControllerBase
{
    private readonly NotificationQueue _notificationQueue;
    private readonly ILogger<JobMonitoringController> _logger;

    public JobMonitoringController(
        NotificationQueue notificationQueue,
        ILogger<JobMonitoringController> logger)
    {
        _notificationQueue = notificationQueue;
        _logger = logger;
    }

    [HttpGet("stream/started")]
    [Authorize]
    public async Task StreamJobStartedEvents(CancellationToken cancellationToken)
    {
        var username = User.Identity?.Name ?? "anonymous";

        // S'abonner uniquement aux événements JobStarted
        var (subscriptionId, reader) = _notificationQueue.Subscribe("JobStarted", username);

        _logger.LogInformation("User {Username} subscribed to JobStarted events", username);

        try
        {
            await foreach (var notification in reader.ReadAllAsync(cancellationToken))
            {
                var data = notification.Data?.ToString() ?? "{}";

                // Envoyer via SSE
                Response.ContentType = "text/event-stream";
                await Response.WriteAsync($"event: {notification.EventType}\n", cancellationToken);
                await Response.WriteAsync($"data: {data}\n\n", cancellationToken);
                await Response.Body.FlushAsync(cancellationToken);
            }
        }
        finally
        {
            _notificationQueue.Unsubscribe(subscriptionId);
            _logger.LogInformation("User {Username} unsubscribed from JobStarted events", username);
        }
    }

    [HttpGet("stream/all")]
    [Authorize]
    public async Task StreamAllJobEvents(CancellationToken cancellationToken)
    {
        var username = User.Identity?.Name ?? "anonymous";

        // Créer 3 souscriptions différentes
        var (subStarted, readerStarted) = _notificationQueue.Subscribe("JobStarted", username);
        var (subCompleted, readerCompleted) = _notificationQueue.Subscribe("JobCompleted", username);
        var (subFailed, readerFailed) = _notificationQueue.Subscribe("JobFailed", username);

        try
        {
            Response.ContentType = "text/event-stream";

            // Utiliser Task.WhenAny pour écouter les 3 channels simultanément
            var tasks = new[]
            {
                ReadChannelAsync(readerStarted, cancellationToken),
                ReadChannelAsync(readerCompleted, cancellationToken),
                ReadChannelAsync(readerFailed, cancellationToken)
            };

            await Task.WhenAll(tasks);
        }
        finally
        {
            _notificationQueue.Unsubscribe(subStarted);
            _notificationQueue.Unsubscribe(subCompleted);
            _notificationQueue.Unsubscribe(subFailed);
        }
    }

    private async Task ReadChannelAsync(
        ChannelReader<QueuedNotification> reader, 
        CancellationToken cancellationToken)
    {
        await foreach (var notification in reader.ReadAllAsync(cancellationToken))
        {
            var data = notification.Data?.ToString() ?? "{}";
            await Response.WriteAsync($"event: {notification.EventType}\n", cancellationToken);
            await Response.WriteAsync($"data: {data}\n\n", cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
        }
    }
}
```

## Exemple 2: Service de monitoring avec multiples instances

```csharp
public class JobMonitoringService : BackgroundService
{
    private readonly NotificationQueue _notificationQueue;
    private readonly ILogger<JobMonitoringService> _logger;
    private readonly List<Guid> _subscriptionIds = new();

    public JobMonitoringService(
        NotificationQueue notificationQueue,
        ILogger<JobMonitoringService> logger)
    {
        _notificationQueue = notificationQueue;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // S'abonner à tous les types d'événements de jobs
        var eventTypes = new[] { "JobStarted", "JobCompleted", "JobFailed" };
        var readers = new List<ChannelReader<QueuedNotification>>();

        foreach (var eventType in eventTypes)
        {
            var (subscriptionId, reader) = _notificationQueue.Subscribe(eventType, "monitoring-service");
            _subscriptionIds.Add(subscriptionId);
            readers.Add(reader);
            _logger.LogInformation("Monitoring service subscribed to {EventType}", eventType);
        }

        try
        {
            // Écouter tous les channels en parallèle
            var tasks = readers.Select(reader => MonitorChannelAsync(reader, stoppingToken)).ToList();
            await Task.WhenAll(tasks);
        }
        finally
        {
            foreach (var subscriptionId in _subscriptionIds)
            {
                _notificationQueue.Unsubscribe(subscriptionId);
            }
        }
    }

    private async Task MonitorChannelAsync(
        ChannelReader<QueuedNotification> reader, 
        CancellationToken stoppingToken)
    {
        await foreach (var notification in reader.ReadAllAsync(stoppingToken))
        {
            // Logger les événements
            _logger.LogInformation(
                "Monitoring: Event {EventType} received with data: {Data}",
                notification.EventType,
                notification.Data?.ToString() ?? "{}");

            // Envoyer à un système de métriques, base de données, etc.
            await StoreMetricsAsync(notification, stoppingToken);
        }
    }

    private async Task StoreMetricsAsync(QueuedNotification notification, CancellationToken cancellationToken)
    {
        // Implémenter la logique de stockage des métriques
        await Task.CompletedTask;
    }
}
```

## Exemple 3: Publier des événements depuis un worker

```csharp
public class JobProcessorWorker : BackgroundService
{
    private readonly BackgroundJobQueue _jobQueue;
    private readonly NotificationQueue _notificationQueue;
    private readonly ILogger<JobProcessorWorker> _logger;

    public JobProcessorWorker(
        BackgroundJobQueue jobQueue,
        NotificationQueue notificationQueue,
        ILogger<JobProcessorWorker> logger)
    {
        _jobQueue = jobQueue;
        _notificationQueue = notificationQueue;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            QueuedJob? job = null;
            try
            {
                job = await _jobQueue.DequeueAsync(stoppingToken);

                // Publier l'événement JobStarted
                var startedData = JsonSerializer.SerializeToElement(new 
                { 
                    job.JobId,
                    job.ClientId,
                    StartedAt = DateTime.UtcNow
                });

                int subscriberCount = _notificationQueue.Publish(
                    new QueuedNotification("JobStarted", startedData));

                _logger.LogInformation(
                    "Published JobStarted for {JobId} to {Count} subscribers",
                    job.JobId, subscriberCount);

                // Traiter le job
                await Task.Delay(TimeSpan.FromSeconds(job.DurationSeconds), stoppingToken);

                // Publier l'événement JobCompleted
                var completedData = JsonSerializer.SerializeToElement(new 
                { 
                    job.JobId,
                    job.ClientId,
                    CompletedAt = DateTime.UtcNow,
                    Duration = job.DurationSeconds
                });

                subscriberCount = _notificationQueue.Publish(
                    new QueuedNotification("JobCompleted", completedData));

                _logger.LogInformation(
                    "Published JobCompleted for {JobId} to {Count} subscribers",
                    job.JobId, subscriberCount);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                if (job != null)
                {
                    var failedData = JsonSerializer.SerializeToElement(new 
                    { 
                        job.JobId,
                        job.ClientId,
                        FailedAt = DateTime.UtcNow,
                        Error = ex.Message
                    });

                    _notificationQueue.Publish(
                        new QueuedNotification("JobFailed", failedData));
                }

                _logger.LogError(ex, "Error processing job {JobId}", job?.JobId ?? "unknown");
            }
        }
    }
}
```

## Exemple 4: Application multi-onglets (frontend)

```javascript
// Onglet 1
const eventSource1 = new EventSource('/api/jobs/stream/started?token=' + authToken);
eventSource1.addEventListener('JobStarted', (event) => {
    console.log('Onglet 1 - Job started:', JSON.parse(event.data));
});

// Onglet 2 (même utilisateur)
const eventSource2 = new EventSource('/api/jobs/stream/started?token=' + authToken);
eventSource2.addEventListener('JobStarted', (event) => {
    console.log('Onglet 2 - Job started:', JSON.parse(event.data));
});

// Les deux onglets reçoivent les événements indépendamment
// car ils ont des souscriptions différentes
```

## Exemple 5: Dashboard avec filtres multiples

```csharp
[HttpGet("stream/filtered")]
[Authorize]
public async Task StreamFilteredEvents(
    [FromQuery] string[] eventTypes,
    CancellationToken cancellationToken)
{
    var username = User.Identity?.Name ?? "anonymous";

    // Créer une souscription pour chaque type d'événement demandé
    var subscriptions = eventTypes
        .Select(eventType => _notificationQueue.Subscribe(eventType, username))
        .ToList();

    _logger.LogInformation(
        "User {Username} subscribed to {Count} event types: {EventTypes}",
        username, subscriptions.Count, string.Join(", ", eventTypes));

    try
    {
        Response.ContentType = "text/event-stream";

        // Créer une tâche pour chaque channel
        var tasks = subscriptions.Select(sub => 
            ProcessChannelAsync(sub.reader, cancellationToken)
        ).ToList();

        await Task.WhenAll(tasks);
    }
    finally
    {
        foreach (var (subscriptionId, _) in subscriptions)
        {
            _notificationQueue.Unsubscribe(subscriptionId);
        }
    }
}

private async Task ProcessChannelAsync(
    ChannelReader<QueuedNotification> reader,
    CancellationToken cancellationToken)
{
    await foreach (var notification in reader.ReadAllAsync(cancellationToken))
    {
        var data = notification.Data?.ToString() ?? "{}";
        await Response.WriteAsync($"event: {notification.EventType}\n", cancellationToken);
        await Response.WriteAsync($"data: {data}\n\n", cancellationToken);
        await Response.Body.FlushAsync(cancellationToken);
    }
}
```

## Test avec PowerShell

```powershell
# test-multi-subscriptions.ps1

$baseUrl = "https://localhost:7001"
$username = "testuser"
$password = "Test123!"

# Login
$loginResponse = Invoke-RestMethod -Uri "$baseUrl/api/authentication/login" `
    -Method POST `
    -ContentType "application/json" `
    -Body (@{ Username = $username; Password = $password } | ConvertTo-Json)

$token = $loginResponse.token

Write-Host "Logged in with token: $token"

# Lancer 3 connexions SSE simultanées (simulant 3 onglets)
$jobs = @()

1..3 | ForEach-Object {
    $tabNumber = $_
    $jobs += Start-Job -ScriptBlock {
        param($BaseUrl, $Token, $TabNumber)

        $request = [System.Net.HttpWebRequest]::Create("$BaseUrl/api/jobs/stream/started")
        $request.Headers.Add("Authorization", "Bearer $Token")
        $request.Method = "GET"

        $response = $request.GetResponse()
        $stream = $response.GetResponseStream()
        $reader = [System.IO.StreamReader]::new($stream)

        Write-Host "Tab $TabNumber connected"

        while (-not $reader.EndOfStream) {
            $line = $reader.ReadLine()
            if ($line -like "data:*") {
                Write-Host "Tab $TabNumber received: $line"
            }
        }
    } -ArgumentList $baseUrl, $token, $tabNumber
}

Write-Host "`n3 tabs connected. Waiting for events..."
Write-Host "Press Ctrl+C to stop"

# Attendre
$jobs | Wait-Job

# Nettoyer
$jobs | Remove-Job
```

## Scénarios de test

### Test 1: Un événement → plusieurs destinataires
```
1. Ouvrir 3 onglets qui s'abonnent à "JobStarted"
2. Démarrer un job
3. Vérifier que les 3 onglets reçoivent l'événement
```

### Test 2: Multiples types d'événements
```
1. Onglet A s'abonne à "JobStarted"
2. Onglet B s'abonne à "JobCompleted"
3. Démarrer un job
4. Vérifier que A reçoit JobStarted mais pas JobCompleted
5. Vérifier que B reçoit JobCompleted mais pas JobStarted
```

### Test 3: Désabonnement
```
1. S'abonner à "JobStarted"
2. Fermer la connexion SSE
3. Démarrer un job
4. Vérifier que l'événement n'est PAS envoyé (souscription supprimée)
```
