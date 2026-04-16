<script setup lang="ts">
import JobControl from './components/JobControl.vue';

const handleJobStarted = () => {
  console.log('Job démarré !');
  // Plus tard : logique SSE et appel API
};
</script>

<template>
  <div class="app">
    <header>
      <h1>POC SSE - Vue.js + ASP.NET</h1>
      <p class="subtitle">Démonstration Server-Sent Events</p>
    </header>

    <main>
      <JobControl @job-started="handleJobStarted" />
    </main>
  </div>
</template>


<style scoped>
.app {
  max-width: 900px;
  margin: 0 auto;
  padding: 2rem;
  font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Oxygen, Ubuntu, sans-serif;
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