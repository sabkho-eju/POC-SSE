<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { authService } from './services/AuthenticationService';
import LoginModal from './components/LoginModal.vue';
import AppHeader from './components/AppHeader.vue';
import JobControlPanel from './components/JobControlPanel.vue';
import MessagingPanel from './components/MessagingPanel.vue';
import NotificationsPanel from './components/NotificationsPanel.vue';

const isAuthenticated = ref(false);
const username = ref('');
const showLoginModal = ref(false);

// Vérifier l'authentification au chargement
onMounted(() => {
  const user = authService.getUser();
  if (user && authService.isAuthenticated()) {
    isAuthenticated.value = true;
    username.value = user.username;
  }
});

const handleLoginSuccess = (user: string) => {
  isAuthenticated.value = true;
  username.value = user;
  showLoginModal.value = false;
};

const handleLogout = async () => {
  await authService.logout();
  isAuthenticated.value = false;
  username.value = '';
};

const handleLoginClick = () => {
  showLoginModal.value = true;
};
</script>

<template>
  <div class="app">
    <AppHeader 
      :username="username" 
      :is-authenticated="isAuthenticated"
      @logout="handleLogout" 
      @login="handleLoginClick"
    />

    <main class="app-content">
      <div class="dashboard">
        <div class="panels-container">
          <JobControlPanel :is-authenticated="isAuthenticated" />
          <MessagingPanel :is-authenticated="isAuthenticated" />
          <NotificationsPanel />
        </div>
      </div>
    </main>

    <!-- Modal de login -->
    <LoginModal 
      :is-open="showLoginModal"
      @close="showLoginModal = false"
      @login-success="handleLoginSuccess"
    />
  </div>
</template>


<style scoped>
.app {
  margin: 0;
  padding: 0;
  font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Oxygen, Ubuntu, sans-serif;
}

.app-content {
  max-width: 1200px;
  margin: 0 auto;
  padding: 2rem;
  width: 100%;
  box-sizing: border-box;
}

.dashboard {
  width: 100%;
  max-width: 1200px;
  margin: 0 auto;
}

.panels-container {
  display: flex;
  flex-direction: column;
  gap: 1.5rem;
}
</style>