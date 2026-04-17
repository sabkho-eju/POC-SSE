<script setup lang="ts">
import { ref } from 'vue';
import { jobApi } from '../services/api';
import { useSseConnection } from '../composables/useSseConnection';

const emit = defineEmits<{
  jobStarted: [jobId: string]
}>();

const isProcessing = ref(false);
const errorMessage = ref<string>('');
const successMessage = ref<string>('');
const lastJobId = ref<string>('');
const isJobActive = ref(false); // Track si un job est en cours
const durationInput = ref<string>('5'); // Valeur par défaut: 5 secondes

// Connexion SSE pour recevoir les notifications
const { isConnected, notifications, connectionError, clearNotifications } = useSseConnection();

/**
 * Parse la durée avec gestion d'erreur
 * Retourne la durée validée ou la valeur par défaut (5)
 */
const parseDuration = (): number => {
  const parsed = parseInt(durationInput.value, 10);
  
  // Vérifier si c'est un nombre valide
  if (isNaN(parsed)) {
    console.warn('Durée invalide, utilisation de la valeur par défaut (5s)');
    durationInput.value = '5'; // Réinitialiser à la valeur par défaut
    return 5;
  }
  
  // Vérifier les limites (min: 1, max: 300 = 5 minutes)
  if (parsed < 1) {
    console.warn('Durée trop courte, minimum 1 seconde');
    durationInput.value = '1';
    return 1;
  }
  
  if (parsed > 300) {
    console.warn('Durée trop longue, maximum 300 secondes');
    durationInput.value = '300';
    return 300;
  }
  
  return parsed;
};

const handleStartJob = async () => {
  isProcessing.value = true;
  errorMessage.value = '';
  successMessage.value = '';
  
  // Parser et valider la durée
  const duration = parseDuration();
  
  try {
    const response = await jobApi.processJob('Sample job data from Vue.js', duration);
    
    lastJobId.value = response.jobId;
    isJobActive.value = true; // Job est maintenant actif
    
    const processedTime = new Date(response.processedAt).toLocaleTimeString();
    successMessage.value = `Job démarré : ${response.jobId} (statut: ${response.status}, durée: ${duration}s, à ${processedTime})`;
    console.log('Job démarré avec succès:', response);
    
    emit('jobStarted', response.jobId);
    
  } catch (error) {
    console.error('Erreur:', error);
    isJobActive.value = false;
    // Utiliser le message d'erreur spécifique si disponible
    if (error instanceof Error) {
      errorMessage.value = error.message;
    } else {
      errorMessage.value = 'Erreur lors du démarrage du job. Vérifiez que le backend est lancé.';
    }
  } finally {
    isProcessing.value = false;
  }
};

const handleCancelJob = async () => {
  if (!lastJobId.value) return;
  
  isProcessing.value = true;
  errorMessage.value = '';
  successMessage.value = '';
  
  try {
    await jobApi.cancelJob(lastJobId.value);
    
    isJobActive.value = false; // Job n'est plus actif
    successMessage.value = `Job ${lastJobId.value} annulé avec succès`;
    console.log('Job annulé avec succès');
    
  } catch (error) {
    console.error('Erreur:', error);
    if (error instanceof Error) {
      errorMessage.value = error.message;
    } else {
      errorMessage.value = 'Erreur lors de l\'annulation du job.';
    }
  } finally {
    isProcessing.value = false;
  }
};
</script>

<template>
  <div class="job-control">
    <div class="info-section">
      <h2>Lancer un traitement</h2>
      <p class="description">
        Cliquez sur le bouton ci-dessous pour démarrer un traitement asynchrone.
        Le serveur vous notifiera via SSE lorsque le job sera terminé.
      </p>
    </div>

    <!-- Champ de durée -->
    <div class="form-group">
      <label for="duration">Durée du traitement (secondes) :</label>
      <input 
        id="duration"
        v-model="durationInput"
        type="number"
        min="1"
        max="300"
        step="1"
        :disabled="isProcessing || isJobActive"
        class="input-duration"
        placeholder="5"
      />
      <span class="input-hint">Entre 1 et 300 secondes (défaut: 5)</span>
    </div>

    <div class="button-group">
      <button 
        @click="handleStartJob"
        :disabled="isProcessing || isJobActive"
        class="btn-start-job"
      >
        {{ isProcessing ? 'Traitement en cours...' : 'Start Job' }}
      </button>

      <button 
        @click="handleCancelJob"
        :disabled="isProcessing || !isJobActive"
        class="btn-cancel-job"
      >
        Cancel Job
      </button>
    </div>

    <!-- Message d'erreur -->
    <div v-if="errorMessage" class="error-message">
      ⚠️ {{ errorMessage }}
    </div>

    <!-- Message de succès -->
    <div v-if="successMessage && !errorMessage" class="success-message">
      ✅ {{ successMessage }}
    </div>

    <!-- Section SSE Notifications -->
    <div class="sse-section">
      <div class="sse-header">
        <h3>
          Notifications SSE
          <span v-if="isConnected" class="status-badge connected">🟢 Connecté</span>
          <span v-else class="status-badge disconnected">🔴 Déconnecté</span>
        </h3>
        <button 
          v-if="notifications.length > 0"
          @click="clearNotifications" 
          class="btn-clear"
        >
          Effacer
        </button>
      </div>

      <!-- Erreur de connexion SSE -->
      <div v-if="connectionError" class="sse-error">
        ⚠️ {{ connectionError }}
      </div>

      <!-- Liste des notifications -->
      <div class="notifications-list">
        <div 
          v-if="notifications.length === 0" 
          class="notification-empty"
        >
          Aucune notification reçue
        </div>
        
        <div 
          v-for="notif in notifications" 
          :key="notif.id"
          :class="['notification-item', `notif-${notif.type}`]"
        >
          <div class="notif-header">
            <span class="notif-type">{{ notif.type }}</span>
            <span class="notif-time">{{ notif.timestamp.toLocaleTimeString() }}</span>
          </div>
          <div class="notif-message">{{ notif.message }}</div>
        </div>
      </div>
    </div>

  </div>
</template>

<style scoped>
.job-control {
  background: #f8f9fa;
  padding: 2rem;
  border-radius: 8px;
  box-shadow: 0 2px 8px rgba(0,0,0,0.1);
}

.info-section {
  margin-bottom: 1.5rem;
}

h2 {
  color: #2c3e50;
  margin: 0 0 1rem 0;
  font-size: 1.5rem;
}

.description {
  color: #666;
  line-height: 1.6;
  margin: 0;
}

.form-group {
  margin-bottom: 1.5rem;
}

.form-group label {
  display: block;
  margin-bottom: 0.5rem;
  font-weight: 500;
  color: #374151;
  font-size: 0.95rem;
}

.input-duration {
  width: 100%;
  max-width: 200px;
  padding: 0.75rem 1rem;
  border: 2px solid #e5e7eb;
  border-radius: 6px;
  font-size: 1rem;
  transition: all 0.2s;
  box-sizing: border-box;
}

.input-duration:focus {
  outline: none;
  border-color: #42b983;
  box-shadow: 0 0 0 3px rgba(66, 185, 131, 0.1);
}

.input-duration:disabled {
  background-color: #f3f4f6;
  cursor: not-allowed;
  opacity: 0.6;
}

.input-hint {
  display: block;
  margin-top: 0.5rem;
  font-size: 0.85rem;
  color: #6b7280;
  font-style: italic;
}

.btn-start-job {
  background-color: #42b983;
  color: white;
  border: none;
  padding: 14px 28px;
  font-size: 16px;
  font-weight: 600;
  border-radius: 6px;
  cursor: pointer;
  transition: all 0.3s ease;
  box-shadow: 0 2px 4px rgba(66, 185, 131, 0.3);
}

.btn-start-job:hover:not(:disabled) {
  background-color: #35a372;
  transform: translateY(-2px);
  box-shadow: 0 4px 8px rgba(66, 185, 131, 0.4);
}

.btn-start-job:active:not(:disabled) {
  transform: translateY(0);
}

.btn-start-job:disabled {
  background-color: #95c9b4;
  cursor: not-allowed;
  opacity: 0.7;
}

.button-group {
  display: flex;
  gap: 1rem;
  margin-bottom: 1rem;
}

.btn-cancel-job {
  background-color: #dc3545;
  color: white;
  border: none;
  padding: 14px 28px;
  font-size: 16px;
  font-weight: 600;
  border-radius: 6px;
  cursor: pointer;
  transition: all 0.3s ease;
  box-shadow: 0 2px 4px rgba(220, 53, 69, 0.3);
}

.btn-cancel-job:hover:not(:disabled) {
  background-color: #c82333;
  transform: translateY(-2px);
  box-shadow: 0 4px 8px rgba(220, 53, 69, 0.4);
}

.btn-cancel-job:active:not(:disabled) {
  transform: translateY(0);
}

.btn-cancel-job:disabled {
  background-color: #f8a5ad;
  cursor: not-allowed;
  opacity: 0.6;
}

.success-message {
  margin-top: 1rem;
  padding: 1rem;
  background: #e8f5e9;
  color: #2e7d32;
  border-radius: 4px;
  border-left: 4px solid #42b983;
}

.error-message {
  margin-top: 1rem;
  padding: 1rem;
  background: #fee2e2;
  color: #dc2626;
  border-radius: 4px;
  border-left: 4px solid #dc2626;
}

/* Section SSE */
.sse-section {
  margin-top: 2rem;
  padding: 1.5rem;
  background: white;
  border-radius: 8px;
  border: 2px solid #e5e7eb;
}

.sse-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 1rem;
}

.sse-header h3 {
  margin: 0;
  font-size: 1.2rem;
  color: #2c3e50;
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.status-badge {
  font-size: 0.85rem;
  padding: 0.25rem 0.75rem;
  border-radius: 12px;
  font-weight: 500;
}

.status-badge.connected {
  background: #d1fae5;
  color: #065f46;
}

.status-badge.disconnected {
  background: #fee2e2;
  color: #991b1b;
}

.btn-clear {
  padding: 0.5rem 1rem;
  background: #f3f4f6;
  border: 1px solid #d1d5db;
  border-radius: 4px;
  cursor: pointer;
  font-size: 0.9rem;
  color: #374151;
  transition: all 0.2s;
}

.btn-clear:hover {
  background: #e5e7eb;
  border-color: #9ca3af;
}

.sse-error {
  padding: 0.75rem;
  background: #fef3c7;
  color: #92400e;
  border-radius: 4px;
  margin-bottom: 1rem;
  font-size: 0.9rem;
}

.notifications-list {
  max-height: 400px;
  overflow-y: auto;
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}

.notification-empty {
  text-align: center;
  color: #9ca3af;
  padding: 2rem;
  font-style: italic;
}

.notification-item {
  padding: 0.75rem 1rem;
  border-radius: 6px;
  border-left: 4px solid #d1d5db;
  background: #f9fafb;
  transition: all 0.2s;
}

.notification-item:hover {
  background: #f3f4f6;
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.05);
}

.notif-connected {
  border-left-color: #10b981;
  background: #ecfdf5;
}

.notif-disconnected {
  border-left-color: #ef4444;
  background: #fef2f2;
}

.notif-message {
  border-left-color: #3b82f6;
  background: #eff6ff;
}

.notif-error {
  border-left-color: #f59e0b;
  background: #fffbeb;
}

.notif-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 0.5rem;
}

.notif-type {
  font-weight: 600;
  font-size: 0.85rem;
  text-transform: uppercase;
  color: #374151;
}

.notif-time {
  font-size: 0.8rem;
  color: #6b7280;
}

.notif-message {
  color: #1f2937;
  font-size: 0.95rem;
  line-height: 1.4;
  word-break: break-word;
}
</style>