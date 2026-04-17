// Types
export interface LoginRequest {
  username: string;
  password: string;
}

export interface LoginResponse {
  success: boolean;
  token?: string;
  username?: string;
  message?: string;
}

export interface User {
  username: string;
  token: string;
}

const API_BASE_URL = '/api'; // Proxy Vite

// Clés de stockage
const TOKEN_KEY = 'auth_token';
const USER_KEY = 'auth_user';

export const authService = {
  /**
   * Login avec username/password
   */
  async login(username: string, password: string): Promise<LoginResponse> {
    try {
      const response = await fetch(`${API_BASE_URL}/authentication/login`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({ username, password }),
      });

      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
      }

      const data: LoginResponse = await response.json();

      // Stocker le token et l'utilisateur
      if (data.success && data.token && data.username) {
        this.setToken(data.token);
        this.setUser({ username: data.username, token: data.token });
      }

      return data;
    } catch (error) {
      console.error('Login error:', error);
      throw error;
    }
  },

  /**
   * Logout
   */
  async logout(): Promise<void> {
    try {
      await fetch(`${API_BASE_URL}/authentication/logout`, {
        method: 'POST',
        headers: {
          Authorization: `Bearer ${this.getToken()}`,
        },
      });
    } catch (error) {
      console.error('Logout error:', error);
    } finally {
      this.clearAuth();
    }
  },

  /**
   * Stocker le token
   */
  setToken(token: string): void {
    localStorage.setItem(TOKEN_KEY, token);
  },

  /**
   * Récupérer le token
   */
  getToken(): string | null {
    return localStorage.getItem(TOKEN_KEY);
  },

  /**
   * Stocker l'utilisateur
   */
  setUser(user: User): void {
    localStorage.setItem(USER_KEY, JSON.stringify(user));
  },

  /**
   * Récupérer l'utilisateur
   */
  getUser(): User | null {
    const userJson = localStorage.getItem(USER_KEY);
    return userJson ? JSON.parse(userJson) : null;
  },

  /**
   * Vérifier si l'utilisateur est authentifié
   */
  isAuthenticated(): boolean {
    const token = this.getToken();
    if (!token) return false;

    // Vérifier si le token est expiré (optionnel pour POC)
    // Pour un vrai projet, décoder le JWT et vérifier exp
    return true;
  },

  /**
   * Nettoyer les données d'authentification
   */
  clearAuth(): void {
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(USER_KEY);
  },
};