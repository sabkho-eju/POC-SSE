# Système de notifications basé sur EventType

## Vue d'ensemble

Le système de notifications (`NotificationQueue`) est organisé autour des **types d'événements** (EventType) plutôt que par client. Cette architecture permet :

- **Multiples souscriptions** pour un même EventType (plusieurs clients peuvent s'abonner indépendamment)
- **Pas de réutilisation** de souscriptions existantes (chaque souscription est unique)
- **Publication basée sur EventType** : quand un événement est publié, il est envoyé à **toutes** les souscriptions correspondantes

## Architecture

```
EventType "JobStarted"
├── Subscription #1 (client: user123, channel: ...)
├── Subscription #2 (client: user456, channel: ...)
└── Subscription #3 (client: anonymous, channel: ...)

EventType "JobCompleted"
├── Subscription #4 (client: user123, channel: ...)
└── Subscription #5 (client: user789, channel: ...)
```

## Types d'événements disponibles

- `JobStarted` - Déclenché quand un job commence
- `JobCompleted` - Déclenché quand un job se termine
- `JobFailed` - Déclenché quand un job échoue
- `MessagingNotification` - Messages entre utilisateurs
- Tout autre EventType personnalisé que vous définissez

## API de NotificationQueue

### Subscribe

Crée une nouvelle souscription pour un type d'événement spécifique.

```csharp
public (Guid subscriptionId, ChannelReader<QueuedNotification> reader) Subscribe(
    string eventType, 
    string? clientId = null)
```

**Paramètres:**
- `eventType` : Le type d'événement auquel s'abonner (ex: "JobStarted")
- `clientId` : Identifiant optionnel du client (pour les logs)

**Retour:**
- `subscriptionId` : GUID unique de la souscription (nécessaire pour se désabonner)
- `reader` : ChannelReader pour lire les notifications

**Exemple:**
```csharp
var (subscriptionId, reader) = notificationQueue.Subscribe("JobStarted", username);
```

### Unsubscribe

Supprime une souscription.

```csharp
public void Unsubscribe(Guid subscriptionId)
```

**Paramètres:**
- `subscriptionId` : Le GUID de la souscription à supprimer

**Exemple:**
```csharp
notificationQueue.Unsubscribe(subscriptionId);
```

### Publish

Publie une notification à **toutes** les souscriptions correspondant à l'EventType.

```csharp
public int Publish(QueuedNotification notification)
```

**Paramètres:**
- `notification` : La notification à publier (contient EventType et Data)

**Retour:**
- Nombre de souscriptions qui ont reçu la notification avec succès

**Exemple:**
```csharp
var notification = new QueuedNotification(
    EventType: "JobStarted",
    Data: JsonSerializer.SerializeToElement(new { JobId = "job-123" })
);

int subscriberCount = notificationQueue.Publish(notification);
```

## Exemples d'utilisation

### Endpoint SSE pour les événements de jobs

```csharp
[HttpGet("job-events")]
[Authorize]
public async Task GetJobEvents(CancellationToken cancellationToken)
{
    var username = GetAuthenticatedUsername();

    // S'abonner aux événements JobStarted
    var (subscriptionId, reader) = _notificationQueue.Subscribe("JobStarted", username);

    try
    {
        await foreach (var notification in reader.ReadAllAsync(cancellationToken))
        {
            var eventData = notification.Data?.ToString() ?? "{}";
            // Envoyer via SSE...
            yield return new SseEvent
            {
                EventType = notification.EventType,
                Data = eventData
            };
        }
    }
    finally
    {
        _notificationQueue.Unsubscribe(subscriptionId);
    }
}
```

### Publier un événement de job

```csharp
// Dans JobProcessorWorker
var jobStartedData = JsonSerializer.SerializeToElement(new 
{ 
    JobId = job.JobId,
    StartedAt = DateTime.UtcNow 
});

// Publie à TOUTES les souscriptions "JobStarted"
_notificationQueue.Publish(new QueuedNotification("JobStarted", jobStartedData));
```

### Multiples souscriptions pour différents événements

```csharp
// Client 1 s'abonne aux jobs qui démarrent
var (sub1, reader1) = notificationQueue.Subscribe("JobStarted", "user123");

// Client 2 s'abonne AUSSI aux jobs qui démarrent (souscription indépendante)
var (sub2, reader2) = notificationQueue.Subscribe("JobStarted", "user456");

// Client 3 s'abonne aux jobs terminés
var (sub3, reader3) = notificationQueue.Subscribe("JobCompleted", "user123");

// Quand on publie "JobStarted", les clients 1 et 2 le reçoivent
notificationQueue.Publish(new QueuedNotification("JobStarted", data));
// → user123 (sub1) reçoit
// → user456 (sub2) reçoit
// → user123 (sub3) ne reçoit PAS (abonné à JobCompleted)

// Quand on publie "JobCompleted", seul le client 3 le reçoit
notificationQueue.Publish(new QueuedNotification("JobCompleted", data));
// → user123 (sub1) ne reçoit PAS
// → user456 (sub2) ne reçoit PAS
// → user123 (sub3) reçoit
```

### Scénario: Plusieurs onglets d'un même utilisateur

```csharp
// Onglet 1 de l'utilisateur user123
var (subTab1, readerTab1) = notificationQueue.Subscribe("JobStarted", "user123");

// Onglet 2 du même utilisateur user123
var (subTab2, readerTab2) = notificationQueue.Subscribe("JobStarted", "user123");

// Les deux onglets reçoivent les événements indépendamment
notificationQueue.Publish(new QueuedNotification("JobStarted", data));
// → Onglet 1 reçoit (subTab1)
// → Onglet 2 reçoit (subTab2)
```

## Caractéristiques importantes

### 1. Pas de réutilisation de souscriptions

Chaque appel à `Subscribe()` crée une **nouvelle souscription unique** :

```csharp
var (sub1, reader1) = notificationQueue.Subscribe("JobStarted", "user123");
var (sub2, reader2) = notificationQueue.Subscribe("JobStarted", "user123");

// sub1 != sub2 (GUIDs différents)
// reader1 != reader2 (channels différents)
```

### 2. Publication broadcast par EventType

Quand vous publiez, **toutes** les souscriptions actives pour cet EventType reçoivent la notification :

```csharp
// 5 clients abonnés à "JobStarted"
int count = notificationQueue.Publish(new QueuedNotification("JobStarted", data));
// count = 5 (tous ont reçu)
```

### 3. Gestion automatique des souscriptions inactives

Les souscriptions marquées comme inactives (après `Unsubscribe`) sont automatiquement ignorées lors de la publication.

### 4. Thread-safe

Utilise `ConcurrentDictionary` et `ConcurrentBag` pour garantir la sécurité des threads.

## Logs

```
// À la souscription
[Information] Subscription abc-123 created for EventType 'JobStarted' (client: user123)

// À la publication
[Debug] Published notification 'JobStarted' to subscription abc-123 (client: user123)
[Information] Published notification 'JobStarted' to 3 active subscriptions

// À la désinscription
[Information] Subscription abc-123 removed for EventType 'JobStarted' (client: user123)
```

## Différences avec l'ancienne architecture

| Ancienne (par ClientId) | Nouvelle (par EventType) |
|--------------------------|--------------------------|
| Clé: ClientId | Clé: EventType |
| 1 souscription par client | Multiples souscriptions possibles |
| Réutilisation si déjà abonné | Chaque Subscribe() crée une nouvelle souscription |
| `PublishToClient(clientId, ...)` | `Publish(notification)` → broadcast |
| Filtre EventType au niveau du client | Organisation naturelle par EventType |

## Cas d'usage recommandés

### Dashboard temps réel
```csharp
// S'abonner à plusieurs types d'événements
var (sub1, reader1) = notificationQueue.Subscribe("JobStarted", userId);
var (sub2, reader2) = notificationQueue.Subscribe("JobCompleted", userId);
var (sub3, reader3) = notificationQueue.Subscribe("JobFailed", userId);
```

### Système de monitoring
```csharp
// Plusieurs instances de monitoring s'abonnent aux mêmes événements
var (subMonitor1, reader1) = notificationQueue.Subscribe("JobStarted", "monitor-1");
var (subMonitor2, reader2) = notificationQueue.Subscribe("JobStarted", "monitor-2");
```

### Application multi-onglets
```csharp
// Chaque onglet maintient sa propre souscription
// Tous reçoivent les événements indépendamment
```

## Performance

- Organisation par EventType : **O(1)** lookup pour trouver les souscriptions
- Publication : **O(n)** où n = nombre de souscriptions pour cet EventType
- Utilisation de `ConcurrentBag` : ajout de souscriptions en **O(1)**
- Pas de suppression physique du Bag (marquage inactif) pour éviter les coûts de synchronisation

