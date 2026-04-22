import { ref, onMounted, onUnmounted } from 'vue';

export interface ServiceNotification {
  id: string;
  timestamp: Date;
  type: string;
  message: string;
  data?: unknown;
}

export function useServiceNotification() {
  const isConnected = ref(false);
  const notifications = ref<ServiceNotification[]>([]);
  const connectionError = ref<string>('');
   
  const connect = () => {
 
  };

  const disconnect = () => {
   
  };

  const clearNotifications = () => {
    notifications.value = [];
  };

  onMounted(() => {
    connect();
  });

  onUnmounted(() => {
    disconnect();
  });

  return {
    isConnected,
    notifications,
    connectionError,
    connect,
    disconnect,
    clearNotifications
  };
}
