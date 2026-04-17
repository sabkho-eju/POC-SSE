<script setup lang="ts">
import { authService } from '../services/AuthenticationService';

defineProps<{
  username: string
}>();

const emit = defineEmits<{
  logout: []
}>();

const handleLogout = async () => {
  await authService.logout();
  emit('logout');
};
</script>

<template>
  <header class="app-header">
    <div class="header-content">
      <div class="header-left">
        <h1>POC SSE - Vue.js + ASP.NET</h1>
        <p class="subtitle">Démonstration Server-Sent Events</p>
      </div>
      <div class="header-right">
        <span class="user-info">👤 {{ username }}</span>
        <button @click="handleLogout" class="btn-logout">
          Déconnexion
        </button>
      </div>
    </div>
  </header>
</template>

<style scoped>
.app-header {
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  color: white;
  padding: 1.5rem 2rem;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
}

.header-content {
  max-width: 1200px;
  margin: 0 auto;
  display: flex;
  justify-content: space-between;
  align-items: center;
  flex-wrap: wrap;
  gap: 1rem;
}

.header-left h1 {
  margin: 0 0 0.25rem 0;
  font-size: 1.8rem;
}

.subtitle {
  margin: 0;
  opacity: 0.9;
  font-size: 0.95rem;
}

.header-right {
  display: flex;
  align-items: center;
  gap: 1rem;
}

.user-info {
  font-weight: 500;
  padding: 0.5rem 1rem;
  background: rgba(255, 255, 255, 0.2);
  border-radius: 20px;
}

.btn-logout {
  padding: 0.5rem 1rem;
  background: rgba(255, 255, 255, 0.2);
  color: white;
  border: 1px solid rgba(255, 255, 255, 0.3);
  border-radius: 6px;
  cursor: pointer;
  font-weight: 500;
  transition: all 0.3s;
}

.btn-logout:hover {
  background: rgba(255, 255, 255, 0.3);
  border-color: white;
}
</style>