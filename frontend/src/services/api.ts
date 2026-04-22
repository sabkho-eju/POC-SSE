import { authService } from './AuthenticationService';

// Configuration de base
// En dev, Vite proxifiera /api vers http://localhost:5236
const API_BASE_URL = '/api'; // ← Vide pour utiliser le proxy

// Types pour le job
export interface JobRequest {
  jobId: string;
  jobData: string;
  durationSeconds?: number; // Optionnel, défaut: 5 côté backend
}

export interface JobResponse {
  jobId: string;
  status: string;
  timestamp: string; // DateTime sérialisé en ISO string
}

export interface ServiceMessagingNotification {
  message: string;
}

export interface JobNotificationStreamHandlers {
  onOpen?: () => void;
  onMessage: (notification: JobResponse) => void;
  onError?: (error: Error) => void;
  onUnauthorized?: () => void;
  onClose?: () => void;
}

export interface MessagingNotificationStreamHandlers {
  onOpen?: () => void;
  onMessage: (notification: ServiceMessagingNotification) => void; 
  onError?: (error: Error) => void;
  onUnauthorized?: () => void;
  onClose?: () => void;
}

/**
 * Ajoute le header Authorization si un token existe
 */
const getAuthHeaders = (): HeadersInit => {
  const headers: HeadersInit = {
    'Content-Type': 'application/json',
  };

  const token = authService.getToken();
  if (token) {
    headers['Authorization'] = `Bearer ${token}`;
  }

  return headers;
};

// Service API
export const jobApi = {
  /**
   * Démarre un nouveau job de traitement
   */
  async processJob(jobData: string, durationSeconds?: number): Promise<JobResponse> {
    const jobId = `job-${new Date().toISOString().replace(/[-:]/g, '').substring(0, 14)}`;
    
    const body: JobRequest = {
      jobId,
      jobData,
      durationSeconds, // Optionnel, utilise la valeur par défaut du backend si non fourni
    };

    try {
      const response = await fetch(`${API_BASE_URL}/jobprocessing/process`, {
        method: 'POST',    
        headers: getAuthHeaders(),             
        body: JSON.stringify(body),
      });

      if (!response.ok) {
        // Gestion erreur 401 Unauthorized
        if (response.status === 401) {
          authService.clearAuth();
          throw new Error('Non authentifié. Veuillez vous connecter pour démarrer un job.');
        }
        throw new Error(`HTTP error! status: ${response.status}`);
      }

      const data: JobResponse = await response.json();
      return data;
    } catch (error) {
      console.error('Erreur lors de l\'appel API:', error);
      throw error;
    }
  },

  /**
   * Annule un job en cours
   */
  async cancelJob(jobId: string): Promise<void> {
    try {
      const response = await fetch(`${API_BASE_URL}/jobprocessing/cancel?jobId=${encodeURIComponent(jobId)}`, {
        method: 'POST',
        headers: getAuthHeaders(),
      });

      if (!response.ok) {
        if (response.status === 401) {
          authService.clearAuth();
          throw new Error('Non authentifié. Veuillez vous connecter.');
        }
        throw new Error(`HTTP error! status: ${response.status}`);
      }
    } catch (error) {
      console.error('Erreur lors de l\'annulation du job:', error);
      throw error;
    }
  },

  connectToStream(handlers: JobNotificationStreamHandlers): () => void {
    const token = authService.getToken();
    if (!token) {
      const error = new Error('Token manquant. Veuillez vous connecter.');
      handlers.onError?.(error);
      return () => {};
    }

    // EventSource ne supporte pas les headers personnalisés, on passe le token dans l'URL
    const url = `${API_BASE_URL}/jobprocessing/job-notification-stream?access_token=${encodeURIComponent(token)}`;
    const eventSource = new EventSource(url);

    // Connexion ouverte
    eventSource.onopen = () => {
      console.log('SSE Connection opened');
      handlers.onOpen?.();
    };

    // Écouter l'événement spécifique 'JobNotification' défini côté backend
    eventSource.addEventListener('JobNotification', (event) => {
      try {
        const notification: JobResponse = JSON.parse(event.data);
        console.log(`Job Notification ${event.lastEventId}:`, notification);
        handlers.onMessage(notification);
      } catch (error) {
        const parseError = error instanceof Error 
          ? error 
          : new Error('Erreur lors du parsing de la notification');
        handlers.onError?.(parseError);
      }
    });

    // Gérer les messages génériques (si le backend n'utilise pas d'événement nommé)
    eventSource.onmessage = (event) => {
      try {
        const notification: JobResponse = JSON.parse(event.data);
        console.log('Received job notification:', notification);
        handlers.onMessage(notification);
      } catch (error) {
        const parseError = error instanceof Error 
          ? error 
          : new Error('Erreur lors du parsing de la notification');
        handlers.onError?.(parseError);
      }
    };

    // Gérer les erreurs et reconnexions
    eventSource.onerror = () => {
      if (eventSource.readyState === EventSource.CONNECTING) {
        console.log('SSE Reconnecting...');
      } else if (eventSource.readyState === EventSource.CLOSED) {
        console.log('SSE Connection closed');
        handlers.onClose?.();
      } else {
        const error = new Error('Erreur de connexion SSE');
        handlers.onError?.(error);
      }
    };

    // Retourner une fonction de cleanup pour fermer la connexion
    return () => {
      console.log('Closing SSE connection');
      eventSource.close();
      handlers.onClose?.();
    };
  },  
};

// Service API pour les notifications
export const notificationApi = {
  /**
   * Envoie un message à un utilisateur spécifique
   */
  async sendToUser(recipientClientId: string, message: string): Promise<void> {
    try {
      const response = await fetch(`${API_BASE_URL}/messaging/send-message-to-a-user?recipientClientId=${encodeURIComponent(recipientClientId)}&message=${encodeURIComponent(message)}`, {
        method: 'POST',
        headers: getAuthHeaders(),
      });

      if (!response.ok) {
        if (response.status === 401) {
          authService.clearAuth();
          throw new Error('Non authentifié. Veuillez vous connecter.');
        }
        throw new Error(`HTTP error! status: ${response.status}`);
      }
    } catch (error) {
      console.error('Erreur lors de l\'envoi du message:', error);
      throw error;
    }
  },

  /**
   * Diffuse un message à tous les utilisateurs connectés
   */
  async broadcastToAll(message: string): Promise<void> {
    try {
      const response = await fetch(`${API_BASE_URL}/messaging/broadcast-to-all-users?message=${encodeURIComponent(message)}`, {
        method: 'POST',
        headers: getAuthHeaders(),
      });

      if (!response.ok) {
        if (response.status === 401) {
          authService.clearAuth();
          throw new Error('Non authentifié. Veuillez vous connecter.');
        }
        throw new Error(`HTTP error! status: ${response.status}`);
      }
    } catch (error) {
      console.error('Erreur lors du broadcast du message:', error);
      throw error;
    }
  },

  connectToStream(handlers: MessagingNotificationStreamHandlers): () => void {
    
    // Retourner une fonction de cleanup pour fermer la connexion
    return () => {
      console.log('Closing connection');            
    };
  },  
};

