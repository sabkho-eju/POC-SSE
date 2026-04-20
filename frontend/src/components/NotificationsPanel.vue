<script setup lang="ts">
import { useServiceNotification } from '../composables/useServiceNotification';

const { isConnected, notifications, connectionError, clearNotifications } = useServiceNotification();
</script>

<template>
  <div class="notifications-panel">
    <div class="panel-header">
      <h2>
        Notifications Service
        <span v-if="isConnected" class="status-badge connected">🟢 Connecté</span>
        <span v-else class="status-badge disconnected">🔴 Déconnecté</span>
      </h2>
      <button 
        v-if="notifications.length > 0"
        @click="clearNotifications" 
        class="btn-clear"
      >
        Effacer
      </button>
    </div>

    <div v-if="connectionError" class="connection-error">
      ⚠️ {{ connectionError }}
    </div>

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
</template>

<style scoped>
.notifications-panel {
  background: white;
  padding: 1.5rem;
  border-radius: 8px;
  border: 2px solid #e5e7eb;
  box-shadow: 0 2px 8px rgba(0,0,0,0.1);
}

.panel-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 1rem;
}

h2 {
  margin: 0;
  font-size: 1.3rem;
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

.connection-error {
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
