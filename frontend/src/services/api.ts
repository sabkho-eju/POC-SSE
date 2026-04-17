import { authService } from './AuthenticationService';

// Configuration de base
// En dev, Vite proxifiera /api vers http://localhost:5236
const API_BASE_URL = ''; // ← Vide pour utiliser le proxy

// Types pour le job
export interface JobRequest {
  jobId: string;
  jobData: string;
}

export interface JobResponse {
  success: boolean;
  jobId: string;
  message?: string;
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
  async processJob(jobData: string): Promise<JobResponse> {
    const jobId = `job-${new Date().toISOString().replace(/[-:]/g, '').substring(0, 14)}`;
    
    const body: JobRequest = {
      jobId,
      jobData,
    };

    try {
      const response = await fetch(`${API_BASE_URL}/api/jobprocessing/process`, {
        method: 'POST',    
        headers: getAuthHeaders(),             
        body: JSON.stringify(body),
      });

      if (!response.ok) {
        // Gestion erreur 401 Unauthorized
        if (response.status === 401) {
          authService.clearAuth();
          window.location.href = '/login';
        }
        throw new Error(`HTTP error! status: ${response.status}`);
      }

      const data = await response.json();
      return {
        success: true,
        jobId,
        ...data,
      };
    } catch (error) {
      console.error('Erreur lors de l\'appel API:', error);
      throw error;
    }
  },
};