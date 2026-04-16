<script setup lang="ts">
import { ref } from 'vue';

const emit = defineEmits<{
  jobStarted: []
}>();

const isProcessing = ref(false);

const handleStartJob = () => {
  isProcessing.value = true;
  emit('jobStarted');
  
  // Simuler un traitement (sera remplacé par l'appel backend)
  setTimeout(() => {
    isProcessing.value = false;
  }, 2000);
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

    <button 
      @click="handleStartJob"
      :disabled="isProcessing"
      class="btn-start-job"
    >
      {{ isProcessing ? 'Traitement en cours...' : 'Start Job' }}
    </button>
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
</style>