<script setup lang="ts">
import { ref, watch } from 'vue';
import { notificationApi } from '../services/api';
import { useMessagingNotification } from '../composables/useMessagingNotification';
const { isConnected, notifications, connectionError, connect, disconnect } = useMessagingNotification();

const props = defineProps<{
  isAuthenticated: boolean
}>();

// Déclarer les variables avant le watch
const isProcessing = ref(false);
const errorMessage = ref<string>('');
const successMessage = ref<string>('');
const recipientClientId = ref<string>('');
const userMessage = ref<string>('');
const broadcastMessage = ref<string>('');

// Surveiller l'état d'authentification pour se connecter/déconnecter
watch(() => props.isAuthenticated, (authenticated) => {
  if (authenticated) {
    console.log('Utilisateur connecté - Connexion au stream SSE de messaging');
    connect();
  } else {
    console.log('Utilisateur déconnecté - Déconnexion du stream SSE de messaging');
    disconnect();
    // Réinitialiser l'état
    errorMessage.value = '';
    successMessage.value = '';
    recipientClientId.value = '';
    userMessage.value = '';
    broadcastMessage.value = '';
    notifications.value = []; // Vider les notifications
  }
}, { immediate: true });

/**
 * Envoie un message à un utilisateur spécifique
 */
const handleSendToUser = async () => {
  if (!recipientClientId.value.trim()) {
    errorMessage.value = 'Veuillez entrer un Client ID destinataire';
    return;
  }

  if (!userMessage.value.trim()) {
    errorMessage.value = 'Veuillez entrer un message';
    return;
  }

  isProcessing.value = true;
  errorMessage.value = '';
  successMessage.value = '';

  try {
    await notificationApi.sendToUser(recipientClientId.value, userMessage.value);
    
    successMessage.value = `Message envoyé à ${recipientClientId.value}`;
    console.log('Message envoyé avec succès');
    
    // Réinitialiser le formulaire
    userMessage.value = '';
  } catch (error) {
    console.error('Erreur:', error);
    if (error instanceof Error) {
      errorMessage.value = error.message;
    } else {
      errorMessage.value = 'Erreur lors de l\'envoi du message';
    }
  } finally {
    isProcessing.value = false;
  }
};

/**
 * Diffuse un message à tous les utilisateurs
 */
const handleBroadcast = async () => {
  if (!broadcastMessage.value.trim()) {
    errorMessage.value = 'Veuillez entrer un message à diffuser';
    return;
  }

  isProcessing.value = true;
  errorMessage.value = '';
  successMessage.value = '';

  try {
    await notificationApi.broadcastToAll(broadcastMessage.value);
    
    successMessage.value = 'Message diffusé à tous les utilisateurs';
    console.log('Message diffusé avec succès');
    
    // Réinitialiser le formulaire
    broadcastMessage.value = '';
  } catch (error) {
    console.error('Erreur:', error);
    if (error instanceof Error) {
      errorMessage.value = error.message;
    } else {
      errorMessage.value = 'Erreur lors de la diffusion du message';
    }
  } finally {
    isProcessing.value = false;
  }
};
</script>

<template>
  <div class="messaging-panel">
    <div class="panel-header">
      <h2>📨 Messaging</h2>
      <p class="description">
        Envoyez des messages ciblés ou diffusez à tous les utilisateurs connectés.
      </p>
    </div>

    <div class="messaging-controls">
      <!-- Envoyer à un utilisateur -->
      <div class="message-row">
        <input 
          v-model="recipientClientId"
          type="text"
          placeholder="Client ID destinataire"
          :disabled="isProcessing"
          class="input-field input-recipient"
        />
        <input 
          v-model="userMessage"
          type="text"
          placeholder="Message à envoyer..."
          :disabled="isProcessing"
          class="input-field input-message"
          @keyup.enter="handleSendToUser"
        />
        <button 
          @click="handleSendToUser"
          :disabled="isProcessing"
          class="btn-send"
        >
          Envoyer
        </button>
      </div>

      <!-- Broadcast à tous -->
      <div class="message-row">
        <input 
          v-model="broadcastMessage"
          type="text"
          placeholder="Message de diffusion à tous..."
          :disabled="isProcessing"
          class="input-field input-message-broadcast"
          @keyup.enter="handleBroadcast"
        />
        <button 
          @click="handleBroadcast"
          :disabled="isProcessing"
          class="btn-broadcast"
        >
          📢 Broadcast
        </button>
      </div>
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
.messaging-panel {
  background: white;
  padding: 1.5rem;
  border-radius: 8px;
  border: 2px solid #e5e7eb;
  box-shadow: 0 2px 8px rgba(0,0,0,0.1);
}

.panel-header {
  margin-bottom: 1.5rem;
}

h2 {
  color: #2c3e50;
  margin: 0 0 0.5rem 0;
  font-size: 1.3rem;
}

.description {
  color: #666;
  line-height: 1.6;
  margin: 0;
  font-size: 0.95rem;
}

.messaging-controls {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}

.message-row {
  display: flex;
  gap: 0.5rem;
  align-items: center;
}

.input-field {
  padding: 0.625rem 1rem;
  border: 2px solid #e5e7eb;
  border-radius: 6px;
  font-size: 0.95rem;
  transition: all 0.2s;
}

.input-field:focus {
  outline: none;
  border-color: #3b82f6;
  box-shadow: 0 0 0 3px rgba(59, 130, 246, 0.1);
}

.input-field:disabled {
  background-color: #f3f4f6;
  cursor: not-allowed;
  opacity: 0.6;
}

.input-recipient {
  flex: 0 0 220px;
}

.input-message {
  flex: 1;
}

.input-message-broadcast {
  flex: 1;
}

.btn-send,
.btn-broadcast {
  padding: 0.625rem 1.25rem;
  border: none;
  border-radius: 6px;
  font-size: 0.95rem;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.3s ease;
  color: white;
  white-space: nowrap;
}

.btn-send {
  background-color: #3b82f6;
  box-shadow: 0 2px 4px rgba(59, 130, 246, 0.3);
}

.btn-send:hover:not(:disabled) {
  background-color: #2563eb;
  transform: translateY(-1px);
  box-shadow: 0 4px 6px rgba(59, 130, 246, 0.4);
}

.btn-broadcast {
  background-color: #8b5cf6;
  box-shadow: 0 2px 4px rgba(139, 92, 246, 0.3);
}

.btn-broadcast:hover:not(:disabled) {
  background-color: #7c3aed;
  transform: translateY(-1px);
  box-shadow: 0 4px 6px rgba(139, 92, 246, 0.4);
}

.btn-send:active:not(:disabled),
.btn-broadcast:active:not(:disabled) {
  transform: translateY(0);
}

.btn-send:disabled,
.btn-broadcast:disabled {
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
