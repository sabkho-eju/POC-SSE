<script setup lang="ts">
import { ref } from 'vue';
import { authService } from '../services/AuthenticationService';

const props = defineProps<{
  isOpen: boolean
}>();

const emit = defineEmits<{
  close: []
  loginSuccess: [username: string]
}>();

const username = ref('');
const password = ref('');
const errorMessage = ref('');
const isLoading = ref(false);

const handleLogin = async () => {
  errorMessage.value = '';
  
  if (!username.value || !password.value) {
    errorMessage.value = 'Veuillez remplir tous les champs';
    return;
  }

  isLoading.value = true;

  try {
    const response = await authService.login(username.value, password.value);
    
    if (response.success) {
      emit('loginSuccess', response.username || username.value);
      emit('close');
      // Réinitialiser le formulaire
      username.value = '';
      password.value = '';
      errorMessage.value = '';
    } else {
      errorMessage.value = response.message || 'Identifiants incorrects';
    }
  } catch (error) {
    errorMessage.value = 'Erreur de connexion au serveur';
    console.error('Login failed:', error);
  } finally {
    isLoading.value = false;
  }
};

const handleClose = () => {
  if (!isLoading.value) {
    emit('close');
    // Réinitialiser le formulaire
    username.value = '';
    password.value = '';
    errorMessage.value = '';
  }
};

// Fermer la modal avec la touche Escape
const handleKeyDown = (event: KeyboardEvent) => {
  if (event.key === 'Escape' && props.isOpen) {
    handleClose();
  }
};

// Ajouter l'écouteur d'événement
if (typeof window !== 'undefined') {
  window.addEventListener('keydown', handleKeyDown);
}
</script>

<template>
  <Teleport to="body">
    <Transition name="modal">
      <div v-if="isOpen" class="modal-overlay" @click="handleClose">
        <div class="modal-container" @click.stop>
          <div class="modal-header">
            <h2>Authentification</h2>
            <button class="btn-close" @click="handleClose" :disabled="isLoading">
              ✕
            </button>
          </div>

          <div class="modal-body">
            <p class="subtitle">Connectez-vous pour accéder aux fonctionnalités</p>

            <form @submit.prevent="handleLogin">
              <div class="form-group">
                <label for="username">Nom d'utilisateur</label>
                <input
                  id="username"
                  v-model="username"
                  type="text"
                  placeholder="Entrez votre nom d'utilisateur"
                  :disabled="isLoading"
                  autocomplete="username"
                />
              </div>

              <div class="form-group">
                <label for="password">Mot de passe</label>
                <input
                  id="password"
                  v-model="password"
                  type="password"
                  placeholder="Entrez votre mot de passe"
                  :disabled="isLoading"
                  autocomplete="current-password"
                  @keyup.enter="handleLogin"
                />
              </div>

              <div v-if="errorMessage" class="error-message">
                ⚠️ {{ errorMessage }}
              </div>

              <button type="submit" class="btn-login" :disabled="isLoading">
                {{ isLoading ? 'Connexion...' : 'Se connecter' }}
              </button>
            </form>

            <div class="help-text">
              <p><strong>Comptes de test :</strong></p>
              <p>admin / admin123 | user / user123</p>
            </div>
          </div>
        </div>
      </div>
    </Transition>
  </Teleport>
</template>

<style scoped>
/* Overlay */
.modal-overlay {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background: rgba(0, 0, 0, 0.6);
  backdrop-filter: blur(4px);
  display: flex;
  justify-content: center;
  align-items: center;
  z-index: 9999;
  padding: 1rem;
}

/* Container */
.modal-container {
  background: white;
  border-radius: 12px;
  box-shadow: 0 20px 60px rgba(0, 0, 0, 0.3);
  max-width: 450px;
  width: 100%;
  max-height: 90vh;
  overflow-y: auto;
  animation: slideDown 0.3s ease-out;
}

@keyframes slideDown {
  from {
    opacity: 0;
    transform: translateY(-50px) scale(0.95);
  }
  to {
    opacity: 1;
    transform: translateY(0) scale(1);
  }
}

/* Header */
.modal-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 1.5rem 2rem;
  border-bottom: 1px solid #e5e7eb;
}

.modal-header h2 {
  margin: 0;
  font-size: 1.5rem;
  color: #1f2937;
}

.btn-close {
  background: none;
  border: none;
  font-size: 1.5rem;
  color: #6b7280;
  cursor: pointer;
  padding: 0.25rem 0.5rem;
  border-radius: 4px;
  transition: all 0.2s;
  line-height: 1;
}

.btn-close:hover:not(:disabled) {
  background: #f3f4f6;
  color: #1f2937;
}

.btn-close:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

/* Body */
.modal-body {
  padding: 2rem;
}

.subtitle {
  color: #6b7280;
  margin: 0 0 1.5rem 0;
  font-size: 0.95rem;
}

/* Form */
.form-group {
  margin-bottom: 1.25rem;
}

.form-group label {
  display: block;
  margin-bottom: 0.5rem;
  font-weight: 500;
  color: #374151;
  font-size: 0.9rem;
}

.form-group input {
  width: 100%;
  padding: 0.75rem 1rem;
  border: 2px solid #e5e7eb;
  border-radius: 6px;
  font-size: 1rem;
  transition: all 0.2s;
  box-sizing: border-box;
}

.form-group input:focus {
  outline: none;
  border-color: #667eea;
  box-shadow: 0 0 0 3px rgba(102, 126, 234, 0.1);
}

.form-group input:disabled {
  background-color: #f9fafb;
  cursor: not-allowed;
  opacity: 0.6;
}

/* Error Message */
.error-message {
  padding: 0.75rem 1rem;
  background: #fee2e2;
  color: #dc2626;
  border-radius: 6px;
  margin-bottom: 1rem;
  font-size: 0.9rem;
  border-left: 3px solid #dc2626;
}

/* Login Button */
.btn-login {
  width: 100%;
  padding: 0.875rem 1.5rem;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  color: white;
  border: none;
  border-radius: 6px;
  font-size: 1rem;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.3s;
  box-shadow: 0 4px 6px rgba(102, 126, 234, 0.3);
}

.btn-login:hover:not(:disabled) {
  transform: translateY(-2px);
  box-shadow: 0 6px 12px rgba(102, 126, 234, 0.4);
}

.btn-login:active:not(:disabled) {
  transform: translateY(0);
}

.btn-login:disabled {
  opacity: 0.6;
  cursor: not-allowed;
  transform: none;
}

/* Help Text */
.help-text {
  margin-top: 1.5rem;
  padding: 1rem;
  background: #f9fafb;
  border-radius: 6px;
  font-size: 0.85rem;
  color: #6b7280;
  text-align: center;
}

.help-text p {
  margin: 0.25rem 0;
}

/* Transitions */
.modal-enter-active,
.modal-leave-active {
  transition: opacity 0.3s ease;
}

.modal-enter-from,
.modal-leave-to {
  opacity: 0;
}

.modal-enter-active .modal-container,
.modal-leave-active .modal-container {
  transition: transform 0.3s ease;
}

.modal-enter-from .modal-container,
.modal-leave-to .modal-container {
  transform: translateY(-50px) scale(0.95);
}
</style>
