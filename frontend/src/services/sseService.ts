import { authService } from './AuthenticationService';

export interface SseEvent {
  id: string;
  timestamp: Date;
  type: string;
  data: unknown;
}

export type SseEventCallback = (event: SseEvent) => void;

export class SseService {
  private eventSource: EventSource | null = null;
  private listeners: Map<string, SseEventCallback[]> = new Map();
  private reconnectAttempts = 0;
  private maxReconnectAttempts = 5;
  private reconnectDelay = 2000; // 2 secondes
  private baseUrl: string;

  constructor(baseUrl: string = '/api') {
    this.baseUrl = baseUrl;
  }

  /**
   * Connexion au flux SSE
   * EventSource ne supporte pas les headers, donc on passe le token en query string
   */
  connect(): void {
    const token = authService.getToken();
    
    if (!token) {
      console.warn('Pas de token d\'authentification, connexion SSE impossible');
      this.emit('error', { 
        id: Date.now().toString(), 
        timestamp: new Date(), 
        type: 'error', 
        data: 'Non authentifié' 
      });
      return;
    }

    // Construire l'URL avec le token en query string
    const url = `${this.baseUrl}/serviceeventnotification/ssestream?access_token=${encodeURIComponent(token)}`;
    
    console.log('🔗 Tentative de connexion SSE:', url.substring(0, 60) + '...');
    console.log('🔑 Token présent:', token.substring(0, 20) + '...');
    
    try {
      this.eventSource = new EventSource(url);

      this.eventSource.onopen = () => {
        console.log('✅ Connexion SSE établie');
        this.reconnectAttempts = 0;
        this.emit('connected', {
          id: Date.now().toString(),
          timestamp: new Date(),
          type: 'connected',
          data: 'Connexion établie'
        });
      };

      this.eventSource.onmessage = (event) => {
        console.log('📨 Message SSE re\u00e7u (raw):', event);
        console.log('📨 Message SSE data:', event.data);
        console.log('📨 Message SSE type:', event.type);
        console.log('📨 Message SSE lastEventId:', event.lastEventId);
        
        try {
          let parsedData: unknown;
          
          // Tenter de parser comme JSON
          try {
            parsedData = JSON.parse(event.data);
            console.log('✅ Data pars\u00e9 comme JSON:', parsedData);
          } catch {
            // Si ce n'est pas du JSON, utiliser la cha\u00eene brute
            parsedData = event.data;
            console.log('ℹ️ Data utilis\u00e9 comme string:', parsedData);
          }
          
          const sseEvent: SseEvent = {
            id: event.lastEventId || Date.now().toString(),
            timestamp: new Date(),
            type: event.type || 'message',
            data: parsedData
          };
          
          console.log('📤 \u00c9mission de l\'\u00e9v\u00e9nement:', sseEvent);
          this.emit('message', sseEvent);
        } catch (error) {
          console.error('❌ Erreur parsing SSE message:', error);
          console.error('❌ Event data brut:', event.data);
        }
      };

      // Écouter les événements SSE custom (ex: "job-completed", "job-started", etc.)
      // EventSource ne capture que les événements "message" par défaut
      // Pour les événements custom avec "event: nom", il faut les écouter explicitement
      const customEventTypes = ['job-completed', 'job-started', 'job-cancelled', 'notification'];
      customEventTypes.forEach(eventType => {
        this.eventSource!.addEventListener(eventType, (event: MessageEvent) => {
          console.log(`📨 Év\u00e9nement SSE custom "${eventType}" re\u00e7u:`, event.data);
          
          try {
            let parsedData: unknown;
            try {
              parsedData = JSON.parse(event.data);
            } catch {
              parsedData = event.data;
            }
            
            const sseEvent: SseEvent = {
              id: event.lastEventId || Date.now().toString(),
              timestamp: new Date(),
              type: eventType,
              data: parsedData
            };
            
            console.log(`📤 Émission de l'événement custom "${eventType}":`, sseEvent);
            this.emit('message', sseEvent);
          } catch (error) {
            console.error(`❌ Erreur parsing événement "${eventType}":`, error);
          }
        });
      });

      this.eventSource.onerror = (error) => {
        console.error('❌ Erreur SSE:', error);
        
        this.emit('error', {
          id: Date.now().toString(),
          timestamp: new Date(),
          type: 'error',
          data: 'Erreur de connexion'
        });

        // Tentative de reconnexion
        if (this.reconnectAttempts < this.maxReconnectAttempts) {
          this.reconnectAttempts++;
          console.log(`🔄 Tentative de reconnexion ${this.reconnectAttempts}/${this.maxReconnectAttempts}...`);
          
          setTimeout(() => {
            this.disconnect();
            this.connect();
          }, this.reconnectDelay);
        } else {
          console.error('❌ Nombre maximum de tentatives de reconnexion atteint');
          this.disconnect();
        }
      };

    } catch (error) {
      console.error('Erreur lors de la création de EventSource:', error);
    }
  }

  /**
   * Déconnexion du flux SSE
   */
  disconnect(): void {
    if (this.eventSource) {
      this.eventSource.close();
      this.eventSource = null;
      console.log('🔌 Connexion SSE fermée');
      
      this.emit('disconnected', {
        id: Date.now().toString(),
        timestamp: new Date(),
        type: 'disconnected',
        data: 'Déconnecté'
      });
    }
  }

  /**
   * S'abonner à un type d'événement
   */
  on(eventType: string, callback: SseEventCallback): void {
    if (!this.listeners.has(eventType)) {
      this.listeners.set(eventType, []);
    }
    this.listeners.get(eventType)!.push(callback);
  }

  /**
   * Se désabonner d'un type d'événement
   */
  off(eventType: string, callback: SseEventCallback): void {
    const callbacks = this.listeners.get(eventType);
    if (callbacks) {
      const index = callbacks.indexOf(callback);
      if (index > -1) {
        callbacks.splice(index, 1);
      }
    }
  }

  /**
   * Émettre un événement vers les listeners
   */
  private emit(eventType: string, event: SseEvent): void {
    const callbacks = this.listeners.get(eventType);
    if (callbacks) {
      callbacks.forEach(callback => callback(event));
    }
  }

  /**
   * Vérifier si la connexion est active
   */
  isConnected(): boolean {
    return this.eventSource !== null && this.eventSource.readyState === EventSource.OPEN;
  }
}
