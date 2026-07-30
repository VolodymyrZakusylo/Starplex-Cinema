import api from './axios';

export interface AuthResponse {
  token: string;
  refreshToken: string;
  tokenExpiry: string;
  userId: string;
  email: string;
  firstName: string;
  lastName: string;
  roles: string[];
}

export interface LoginRequest {
  email: string;
  password:  string;
}

export interface RegisterRequest {
  email: string;
  password:  string;
  firstName: string;
  lastName:  string;
  dateOfBirth: string;
}

export const authApi = {
  login: async (data: LoginRequest): Promise<AuthResponse> => {
    const response = await api.post<AuthResponse>('/auth/login', data);
    return response.data;
  },

  register: async (data: RegisterRequest): Promise<void> => {
    await api.post('/auth/register', data);
  },

  logout: async (userId: string, refreshToken: string): Promise<void> => {
    await api.post('/auth/logout', { userId, refreshToken });
  }
};