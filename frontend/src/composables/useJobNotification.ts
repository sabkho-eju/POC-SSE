import { ref, onUnmounted } from 'vue';
import { jobApi, type JobNotificationStreamHandlers } from '../services/api';

export interface JobNotification {
  jobId: string;
  status: string;
  timestamp: string; 
}

export function useJobNotification() {
  const isConnected = ref(false);
  const notifications = ref<JobNotification[]>([]);
  const connectionError = ref<string>('');

  let stopStream: (() => void) | null = null;

  const connect = () => {

    if (stopStream) {
      return;
    }

    connectionError.value = '';

    stopStream = jobApi.connectToStream( {
      onOpen: () => {
        console.debug('Connexion établie');
        isConnected.value = true;        
      },
      onMessage: (jobNotification) => {
        //console.debug('Notification reçue:', jobNotification);
        notifications.value.push({
          jobId: jobNotification.jobId,
          status: jobNotification.status,
          timestamp: jobNotification.timestamp
        });              
      },
      onUnauthorized: () => {
        console.warn('Session expirée ou non autorisée. Veuillez vous reconnecter.');
        //isConnected.value = false;
        connectionError.value = 'Session expiree. Veuillez vous reconnecter.';
      },
      onError: (error) => {
        console.error('Erreur de connexion SSE:', error);
        //isConnected.value = false;
        connectionError.value = error.message;
        //addNotification('error', error.message);
      },
      onClose: () => {
        console.debug('Connexion fermée');
        isConnected.value = false;
        stopStream = null;
      },
    } as JobNotificationStreamHandlers);
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
