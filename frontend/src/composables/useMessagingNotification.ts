import { ref, onUnmounted } from 'vue';
import { notificationApi, type MessagingNotificationStreamHandlers } from '../services/api';

export interface MessagingNotification {
  message: string;
}

export function useMessagingNotification() {
  const isConnected = ref(false);
  const notifications = ref<MessagingNotification[]>([]);
  const connectionError = ref<string>('');

  let stopStream: (() => void) | null = null;

  const connect = () => {

    if (stopStream) {
      return;
    }

    connectionError.value = '';

    stopStream = notificationApi.connectToStream( {
      onOpen: () => {
        console.debug('Connexion établie');
        isConnected.value = true;        
        },
      onMessage: (messagingNotification) => {
        //console.debug('Notification reçue:', messagingNotification);
        notifications.value.push({
          message: messagingNotification.message
        });
        },                    
      onUnauthorized: () => {
        console.warn('Session expirée ou non autorisée. Veuillez vous reconnecter.');
        //isConnected.value = false;
        connectionError.value = 'Session expiree. Veuillez vous reconnecter.';
      },
      onError: (error) => {
        console.error('Erreur de connexion:', error);
        //isConnected.value = false;
        connectionError.value = error.message;
        //addNotification('error', error.message);
      },
      onClose: () => {
        console.debug('Connexion fermée');
        isConnected.value = false;
        stopStream = null;
      },
    } as MessagingNotificationStreamHandlers);
  };

  const disconnect = () => {
    if (!stopStream) {
      return;
    }

    stopStream();
    stopStream = null;
    isConnected.value = false;
    console.debug('Connexion SSE fermée manuellement');
  };
  
  onUnmounted(() => {
    disconnect();
  });

  return {
    isConnected,
    notifications,
    connectionError,
    connect,
    disconnect
  };
}
