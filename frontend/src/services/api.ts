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
  processedAt: string; // DateTime sérialisé en ISO string
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
};