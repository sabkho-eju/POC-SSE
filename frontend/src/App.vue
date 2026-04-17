<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { authService } from './services/AuthenticationService';
import LoginModal from './components/LoginModal.vue';
import AppHeader from './components/AppHeader.vue';
import JobControl from './components/JobControl.vue';

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

const handleJobStarted = () => {
  console.log('Job démarré !');
  // Plus tard : logique SSE et appel API
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
      <JobControl @job-started="handleJobStarted" />
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
  max-width: 900px;
  margin: 0 auto;
  padding: 2rem;
  font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Oxygen, Ubuntu, sans-serif;
}

.app-content {
  flex: 1;
  max-width: 900px;
  margin: 0 auto;
  padding: 2rem;
  width: 100%;
  box-sizing: border-box;
}

header {
  text-align: center;
  margin-bottom: 3rem;
  padding-bottom: 1.5rem;
  border-bottom: 3px solid #42b983;
}

h1 {
  color: #2c3e50;
  margin: 0 0 0.5rem 0;
  font-size: 2.5rem;
}

.subtitle {
  color: #7f8c8d;
  margin: 0;
  font-size: 1.1rem;
}

main {
  display: flex;
  flex-direction: column;
  gap: 2rem;
}
</style>