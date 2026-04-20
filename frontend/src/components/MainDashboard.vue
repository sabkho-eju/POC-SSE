<script setup lang="ts">
import JobControlPanel from './JobControlPanel.vue';
import MessagingPanel from './MessagingPanel.vue';
import NotificationsPanel from './NotificationsPanel.vue';

const handleJobStarted = (jobId: string) => {
  console.log('Job démarré dans le dashboard:', jobId);
};
</script>

<template>
  <div class="dashboard">
    <div class="panels-container">
      <JobControlPanel @job-started="handleJobStarted" />
      <MessagingPanel />
      <NotificationsPanel />
    </div>
  </div>
</template>

<style scoped>
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
