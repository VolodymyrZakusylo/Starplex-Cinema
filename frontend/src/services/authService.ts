import api from './api';

export interface LoginDto {
  email: string;
  password: string;
}

export interface RegisterDto {
  firstName: string;
  lastName: string;
  email: string;
  password: string;
}

export interface AuthResponse {
  token: string;
  refreshToken: string;
  tokenExpiry: string;
  userId: string;
  email: string;
  firstName: string;
  lastName: string;
  roles: string[];
  cinemaId?: string | null;
}

export const login = async (dto: LoginDto): Promise<AuthResponse> => {
  const response = await api.post<AuthResponse>('/Auth/login', dto);
  return response.data;
};

export const register = async (dto: RegisterDto): Promise<AuthResponse> => {
  const response = await api.post<AuthResponse>('/Auth/register', dto);
  return response.data;
};

export const logout = () => {
  localStorage.clear();
};