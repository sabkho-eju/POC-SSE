import { ref, onMounted, onUnmounted } from 'vue';
import { SseService, type SseEvent } from '../services/sseService';

export interface SseNotification {
  id: string;
  timestamp: Date;
  type: string;
  message: string;
  data?: unknown;
}

export function useSseConnection() {
  const isConnected = ref(false);
  const notifications = ref<SseNotification[]>([]);
  const connectionError = ref<string>('');
  
  let sseService: SseService;

  const addNotification = (event: SseEvent) => {
    console.log('🔔 Ajout notification:', event);
    
    const notification: SseNotification = {
      id: event.id,
      timestamp: event.timestamp,
      type: event.type,
      message: typeof event.data === 'string' ? event.data : JSON.stringify(event.data),
      data: event.data
    };
    
    console.log('🔔 Notification format\u00e9e:', notification);
    
    // Ajouter en d\u00e9but de liste (les plus r\u00e9cents en premier)
    notifications.value.unshift(notification);
    
    console.log('🔔 Nombre total de notifications:', notifications.value.length);
    
    // Limiter à 50 notifications
    if (notifications.value.length > 50) {
      notifications.value = notifications.value.slice(0, 50);
    }
  };

  const connect = () => {
    sseService = new SseService();
    
    sseService.on('connected', (event) => {
      isConnected.value = true;
      connectionError.value = '';
      addNotification(event);
    });
    
    sseService.on('disconnected', (event) => {
      isConnected.value = false;
      addNotification(event);
    });
    
    sseService.on('message', (event) => {
      console.log('📥 Message re\u00e7u dans composable:', event);
      addNotification(event);
    });
    
    sseService.on('error', (event) => {
      isConnected.value = false;
      connectionError.value = event.data || 'Erreur de connexion';
      addNotification(event);
    });
    
    sseService.connect();
  };

  const disconnect = () => {
    if (sseService) {
      sseService.disconnect();
    }
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
