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
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(body),
      });

      if (!response.ok) {
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