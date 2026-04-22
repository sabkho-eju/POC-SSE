<script setup lang="ts">
import { ref, watch } from 'vue';
import { jobApi } from '../services/api';
import { useJobNotification } from '../composables/useJobNotification';
const { isConnected, notifications, connectionError, connect, disconnect } = useJobNotification();

const props = defineProps<{
  isAuthenticated: boolean
}>();

const emit = defineEmits<{
  jobStarted: [jobId: string]
}>();

// Déclarer les variables avant le watch
const isProcessing = ref(false);
const errorMessage = ref<string>('');
const successMessage = ref<string>('');
const lastJobId = ref<string>('');
const isJobActive = ref(false);
const durationInput = ref<string>('5');

// Surveiller l'état d'authentification pour se connecter/déconnecter
watch(() => props.isAuthenticated, (authenticated) => {
  if (authenticated) {
    console.log('Utilisateur connecté - Connexion au stream SSE de jobs');
    connect();
  } else {
    console.log('Utilisateur déconnecté - Déconnexion du stream SSE de jobs');
    disconnect();
    // Réinitialiser l'état
    isJobActive.value = false;
    lastJobId.value = '';
    errorMessage.value = '';
    successMessage.value = '';
    notifications.value = []; // Vider les notifications
  }
}, { immediate: true });

const parseDuration = (): number => {
  const parsed = parseInt(durationInput.value, 10);
  
  if (isNaN(parsed)) {
    console.warn('Durée invalide, utilisation de la valeur par défaut (5s)');
    durationInput.value = '5';
    return 5;
  }
  
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
  
  const duration = parseDuration();
  
  try {
    const response = await jobApi.processJob('Sample job data from Vue.js', duration);
    
    lastJobId.value = response.jobId;
    isJobActive.value = true;
    
    const processedTime = new Date(response.timestamp).toLocaleTimeString();
    successMessage.value = `Job démarré : ${response.jobId} (statut: ${response.status}, durée: ${duration}s, à ${processedTime})`;
    console.log('Job démarré avec succès:', response);
    
    emit('jobStarted', response.jobId);
    
  } catch (error) {
    console.error('Erreur:', error);
    isJobActive.value = false;
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
    
    isJobActive.value = false;
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

// Surveiller les notifications pour mettre à jour l'état des boutons
watch(notifications, (newNotifications) => {
  if (newNotifications.length === 0) return;
  
  // Récupérer la dernière notification
  const latestNotification = newNotifications[newNotifications.length - 1];
  
  // Vérifier que la notification existe
  if (!latestNotification) return;
  
  // Mettre à jour l'état des boutons en fonction du statut
  if (latestNotification.status === 'JobStarted') {
    isJobActive.value = true;
    console.log('Job actif - Cancel activé');
  } else if (latestNotification.status === 'JobCompleted') {
    isJobActive.value = false;
    successMessage.value = `Job ${latestNotification.jobId} terminé avec succès`;
    console.log('Job terminé - Start activé, Cancel désactivé');
  }
}, { deep: true });
</script>

<template>
  <div class="job-control-panel">
    <div class="panel-header">
      <h2>Contrôle des Jobs</h2>
      <p class="description">
        Démarrez un traitement asynchrone. Le serveur vous notifiera via SSE lorsque le job sera terminé.
      </p>
    </div>

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

    <div class="button-group">
      <h2>        
        <label style="font-size: 1rem; margin: 0 0.5rem 0 0;">Notifications Service</label>
        <span v-if="isConnected" class="status-badge connected">🟢 Connecté</span>
        <span v-else class="status-badge disconnected">🔴 Déconnecté</span>
      </h2>
    </div>

    <div v-if="connectionError" class="connection-error-message">
      ⚠️ {{ connectionError }}
    </div>


    <div v-if="errorMessage" class="error-message">
      ⚠️ {{ errorMessage }}
    </div>

    <div v-if="successMessage && !errorMessage" class="success-message">
      ✅ {{ successMessage }}
    </div>
  </div>
</template>

<style scoped>
.job-control-panel {
  background: #f8f9fa;
  padding: 1.5rem;
  border-radius: 8px;
  box-shadow: 0 2px 8px rgba(0,0,0,0.1);
}

.panel-header {
  margin-bottom: 1.5rem;
}

h2 {
  color: #2c3e50;
  margin: 0 0 0.5rem 0;
  font-size: 1.5rem;
}

.description {
  color: #666;
  line-height: 1.6;
  margin: 0;
  font-size: 0.95rem;
}

.form-group {
  margin-bottom: 1.5rem;
}

.form-group label {
  display: block;
  margin-bottom: 0.5rem;
  font-weight: 500;
  color: #374151;
  font-size: 0.9rem;
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

.button-group {
  display: flex;
  gap: 1rem;
  margin-bottom: 1rem;
}

.btn-start-job,
.btn-cancel-job {
  padding: 0.875rem 1.75rem;
  border: none;
  border-radius: 6px;
  font-size: 1rem;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.3s ease;
  color: white;
}

.btn-start-job {
  background-color: #42b983;
  box-shadow: 0 2px 4px rgba(66, 185, 131, 0.3);
}

.btn-start-job:hover:not(:disabled) {
  background-color: #35a372;
  transform: translateY(-2px);
  box-shadow: 0 4px 8px rgba(66, 185, 131, 0.4);
}

.btn-cancel-job {
  background-color: #dc3545;
  box-shadow: 0 2px 4px rgba(220, 53, 69, 0.3);
}

.btn-cancel-job:hover:not(:disabled) {
  background-color: #c82333;
  transform: translateY(-2px);
  box-shadow: 0 4px 8px rgba(220, 53, 69, 0.4);
}

.btn-start-job:active:not(:disabled),
.btn-cancel-job:active:not(:disabled) {
  transform: translateY(0);
}

.btn-start-job:disabled,
.btn-cancel-job:disabled {
  opacity: 0.6;
  cursor: not-allowed;
  transform: none;
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

.connection-error-message {
  margin-top: 0.5rem;
  padding: 0.75rem;
  background: #fef3c7;
  color: #92400e;
  border-radius: 4px;
  border-left: 4px solid #f59e0b;
  font-size: 0.9rem;
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
</style>
